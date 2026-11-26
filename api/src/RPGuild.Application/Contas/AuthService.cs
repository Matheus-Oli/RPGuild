using RPGuild.Application.Comum;
using RPGuild.Application.Notificacoes;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public sealed class AuthService(
    IIdentityService identity,
    ITokenService tokens,
    RefreshTokenService refreshTokens,
    EnvioDeEmail emails,
    FrontOptions front) : IAuthService
{
    public async Task<Resultado<UsuarioResponse>> RegistrarAsync(RegistrarRequest request, CancellationToken cancellationToken)
    {
        var criado = await identity.CriarUsuarioAsync(
            request.Nome.Trim(), Nick.Criar(request.Nick), request.Email.Trim(), request.Senha, cancellationToken);

        if (criado.Sucesso)
        {
            var usuario = criado.Valor!;
            await emails.EnviarAsync(EmailTemplates.BoasVindas(usuario.Nome, usuario.Email), cancellationToken);
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
        return new SessaoAutenticada(tokens.GerarAccessToken(usuario, refresh.FamilyId), refresh);
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
        return new SessaoAutenticada(tokens.GerarAccessToken(usuario, refresh.FamilyId), refresh);
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
        await emails.EnviarAsync(EmailTemplates.RedefinicaoDeSenha(dados.Nome, dados.Email, link), cancellationToken);

        return Vazio.Valor;
    }

    public async Task<Resultado<Vazio>> RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken cancellationToken)
    {
        var token = TokenDeRedefinicao.Decodificar(request.Token);
        if (token is null || !await identity.RedefinirSenhaAsync(token, request.NovaSenha, cancellationToken))
        {
            return ErrosDeConta.TokenDeRedefinicaoInvalido;
        }

        // Quem pediu o reset pode estar tirando um invasor da conta: todas as sessões caem.
        await refreshTokens.RevogarOutrasSessoesAsync(token.UserId, sessaoAtual: null, cancellationToken);
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
}
