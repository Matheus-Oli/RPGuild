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
}
