namespace RPGuild.Application.Contas;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    /// <param name="sessaoId">Família do refresh token desta sessão; vai na claim "sid".</param>
    AccessToken GerarAccessToken(UsuarioResponse usuario, Guid sessaoId);
}
