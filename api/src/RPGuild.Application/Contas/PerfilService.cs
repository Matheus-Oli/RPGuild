using Microsoft.Extensions.Logging;
using RPGuild.Application.Comum;
using RPGuild.Application.Notificacoes;
using RPGuild.Application.Uploads;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public sealed class PerfilService(
    IIdentityService identity,
    IStorageService storage,
    RefreshTokenService refreshTokens,
    EnvioDeEmail emails,
    FrontOptions front,
    ILogger<PerfilService> logger) : IPerfilService
{
    public async Task<Resultado<PerfilResponse>> ObterAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Token válido de um usuário que não existe mais: trata como sessão inválida.
        var perfil = await identity.BuscarPerfilAsync(userId, cancellationToken);
        return perfil is null ? ErrosDeConta.SessaoInvalida : Resposta(perfil);
    }

    public async Task<Resultado<PerfilResponse>> AtualizarAsync(
        Guid userId, AtualizarPerfilRequest request, CancellationToken cancellationToken)
    {
        if (request.AvatarPath is not null && !CaminhoDeAvatar.PertenceAo(userId, request.AvatarPath))
        {
            return ErrosDeConta.AvatarForaDaPasta;
        }

        var atualizado = await identity.AtualizarPerfilAsync(
            userId, request.Nome.Trim(), Nick.Criar(request.Nick), request.AvatarPath, cancellationToken);
        if (!atualizado.Sucesso)
        {
            return atualizado.Erro!;
        }

        var (perfil, anterior) = atualizado.Valor!;
        if (anterior is not null && anterior != perfil.AvatarPath)
        {
            await ApagarAvatarAntigoAsync(anterior, cancellationToken);
        }

        return Resposta(perfil);
    }

    public async Task<Resultado<Vazio>> PedirTrocaDeEmailAsync(
        Guid userId, TrocarEmailRequest request, CancellationToken cancellationToken)
    {
        var pedido = await identity.PedirTrocaDeEmailAsync(userId, request.Senha, request.NovoEmail.Trim(), cancellationToken);
        if (!pedido.Sucesso)
        {
            return pedido.Erro!;
        }

        // A confirmação vai para o endereço novo: prova que ele é da pessoa. Só o token opaco vai na URL.
        var dados = pedido.Valor!;
        var link = $"{front.BaseUrl.TrimEnd('/')}/confirmar-email?token={dados.Token.Codificar()}";
        await emails.EnviarAsync(EmailTemplates.ConfirmacaoDeNovoEmail(dados.Nome, dados.NovoEmail, link), cancellationToken);

        return Vazio.Valor;
    }

    public async Task<Resultado<Vazio>> ConfirmarTrocaDeEmailAsync(
        ConfirmarTrocaDeEmailRequest request, Guid? sessaoAtual, CancellationToken cancellationToken)
    {
        var token = TokenOpaco.Decodificar(request.Token);
        if (token is null)
        {
            return ErrosDeConta.TokenDeTrocaDeEmailInvalido;
        }

        var troca = await identity.ConfirmarTrocaDeEmailAsync(token, cancellationToken);
        if (!troca.Sucesso)
        {
            return troca.Erro!;
        }

        // O aviso vai para o endereço antigo: se quem trocou foi um invasor, o dono fica sabendo.
        var trocado = troca.Valor!;
        await emails.EnviarAsync(EmailTemplates.EmailAlterado(trocado.Nome, trocado.EmailAntigo, trocado.EmailNovo), cancellationToken);
        await refreshTokens.RevogarOutrasSessoesAsync(trocado.UserId, sessaoAtual, cancellationToken);

        return Vazio.Valor;
    }

    public async Task<Resultado<Vazio>> TrocarSenhaAsync(
        Guid userId, TrocarSenhaRequest request, Guid? sessaoAtual, CancellationToken cancellationToken)
    {
        var troca = await identity.TrocarSenhaAsync(userId, request.Atual, request.Nova, cancellationToken);
        if (!troca.Sucesso)
        {
            return troca.Erro!;
        }

        var usuario = troca.Valor!;
        await emails.EnviarAsync(EmailTemplates.SenhaAlterada(usuario.Nome, usuario.Email, front.BaseUrl), cancellationToken);
        await refreshTokens.RevogarOutrasSessoesAsync(userId, sessaoAtual, cancellationToken);

        return Vazio.Valor;
    }

    private PerfilResponse Resposta(DadosDoPerfil perfil) => new(
        perfil.Id,
        perfil.Nome,
        perfil.Nick,
        perfil.Email,
        perfil.AvatarPath,
        perfil.AvatarPath is null ? null : storage.ObterUrlPublica(perfil.AvatarPath));

    private async Task ApagarAvatarAntigoAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await storage.ApagarAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            // O perfil já foi salvo: um arquivo órfão no bucket não justifica falhar a requisição.
            logger.LogWarning(ex, "Falha ao apagar o avatar antigo {Path} do storage", path);
        }
    }
}
