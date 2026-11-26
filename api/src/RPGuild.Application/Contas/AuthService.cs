using Microsoft.Extensions.Logging;
using RPGuild.Application.Comum;
using RPGuild.Application.Notificacoes;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public sealed class AuthService(
    IIdentityService identity,
    ITokenService tokens,
    RefreshTokenService refreshTokens,
    IEmailSender emails,
    FrontOptions front,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Resultado<UsuarioResponse>> RegistrarAsync(RegistrarRequest request, CancellationToken cancellationToken)
    {
        var criado = await identity.CriarUsuarioAsync(
            request.Nome.Trim(), Nick.Criar(request.Nick), request.Email.Trim(), request.Senha, cancellationToken);

        if (criado.Sucesso)
        {
            var usuario = criado.Valor!;
            var mensagem = EmailTemplates.BoasVindas(usuario.Nome, usuario.Email);
            try
            {
                // TODO(hangfire): mover envio para fila
                await emails.EnviarAsync(mensagem, cancellationToken);
            }
            catch (Exception ex)
            {
                // A conta já foi criada; o e-mail perdido não deve virar erro para o usuário.
                logger.LogError(ex, "Falha ao enviar o e-mail \"{Assunto}\" para {Para}", mensagem.Assunto, mensagem.Para);
            }
        }

        return criado;
    }

    public async Task<Resultado<SessaoAutenticada>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var credenciais = await identity.VerificarCredenciaisAsync(request.Email.Trim(), request.Senha, cancellationToken);
        if (!credenciais.Sucesso)
        {
            return credenciais.Erro!;
        }

        var usuario = credenciais.Valor!;
        var refresh = await refreshTokens.EmitirAsync(usuario.Id, cancellationToken);
        return new SessaoAutenticada(tokens.GerarAccessToken(usuario), refresh);
    }

    public async Task<Resultado<SessaoAutenticada>> RenovarAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return ErrosDeConta.SessaoInvalida;
        }

        var rotacao = await refreshTokens.RotacionarAsync(refreshToken, cancellationToken);
        if (!rotacao.Sucesso)
        {
            return rotacao.Erro!;
        }

        var refresh = rotacao.Valor!;
        var perfil = await identity.BuscarPerfilAsync(refresh.UserId, cancellationToken);
        if (perfil is null)
        {
            return ErrosDeConta.SessaoInvalida;
        }

        var usuario = new UsuarioResponse(perfil.Id, perfil.Nome, perfil.Nick, perfil.Email);
        return new SessaoAutenticada(tokens.GerarAccessToken(usuario), refresh);
    }

    public async Task<Resultado<Vazio>> EsqueciSenhaAsync(EsqueciSenhaRequest request, CancellationToken cancellationToken)
    {
        var dados = await identity.GerarTokenDeRedefinicaoAsync(request.Email.Trim(), cancellationToken);
        if (dados is null)
        {
            return ErrosDeConta.EmailNaoCadastrado;
        }

        // Só o token opaco vai na URL; o e-mail não.
        var link = $"{front.BaseUrl.TrimEnd('/')}/redefinir-senha?token={dados.Token.Codificar()}";
        var mensagem = EmailTemplates.RedefinicaoDeSenha(dados.Nome, dados.Email, link);
        try
        {
            // TODO(hangfire): mover envio para fila
            await emails.EnviarAsync(mensagem, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar o e-mail \"{Assunto}\" para {Para}", mensagem.Assunto, mensagem.Para);
        }

        return Vazio.Valor;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        // Logout é idempotente: sem cookie ou com token já revogado, só não há o que revogar.
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await refreshTokens.RevogarAsync(refreshToken, cancellationToken);
        }
    }

    public async Task<Resultado<PerfilResponse>> ObterPerfilAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Token válido de um usuário que não existe mais: trata como sessão inválida.
        var perfil = await identity.BuscarPerfilAsync(userId, cancellationToken);
        return perfil is null ? ErrosDeConta.SessaoInvalida : perfil;
    }
}
