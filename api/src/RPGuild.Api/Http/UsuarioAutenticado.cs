using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace RPGuild.Api.Http;

public static class UsuarioAutenticado
{
    /// <summary>
    /// Id do usuário do access token (claim "sub"). Só usar em actions com [Authorize].
    /// </summary>
    public static Guid Id(this ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Access token sem a claim 'sub'."));

    /// <summary>
    /// Sessão do access token (claim "sid" = família do refresh token), ou null em requisição anônima.
    /// </summary>
    public static Guid? SessaoId(this ClaimsPrincipal usuario) =>
        Guid.TryParse(usuario.FindFirstValue(JwtRegisteredClaimNames.Sid), out var sessao) ? sessao : null;
}
