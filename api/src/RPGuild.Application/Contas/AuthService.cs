using RPGuild.Application.Comum;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public sealed class AuthService(IIdentityService identity, ITokenService tokens, RefreshTokenService refreshTokens) : IAuthService
{
    public Task<Resultado<UsuarioResponse>> RegistrarAsync(RegistrarRequest request, CancellationToken cancellationToken) =>
        identity.CriarUsuarioAsync(
            request.Nome.Trim(), Nick.Criar(request.Nick), request.Email.Trim(), request.Senha, cancellationToken);

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

    public async Task<Resultado<PerfilResponse>> ObterPerfilAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Token válido de um usuário que não existe mais: trata como sessão inválida.
        var perfil = await identity.BuscarPerfilAsync(userId, cancellationToken);
        return perfil is null ? ErrosDeConta.SessaoInvalida : perfil;
    }
}
