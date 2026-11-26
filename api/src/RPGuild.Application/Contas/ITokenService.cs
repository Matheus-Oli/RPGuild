namespace RPGuild.Application.Contas;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken GerarAccessToken(UsuarioResponse usuario);
}
