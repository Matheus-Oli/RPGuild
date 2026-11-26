using RPGuild.Application.Contas;

namespace RPGuild.Api.Http;

/// <summary>
/// Cookie do refresh token. SameSite=None porque o front (Cloudflare Pages) e a API ficam em domínios diferentes;
/// por isso também precisa de Secure. O Path restringe o envio às rotas de auth.
/// </summary>
public static class RefreshCookie
{
    public const string Nome = "rpguild_refresh";
    public const string Path = "/api/auth";

    public static void Definir(HttpResponse response, RefreshTokenEmitido refresh) =>
        response.Cookies.Append(Nome, refresh.Token, Opcoes(refresh.ExpiresAt));

    public static string? Ler(HttpRequest request) => request.Cookies[Nome];

    public static void Apagar(HttpResponse response) => response.Cookies.Delete(Nome, Opcoes(expiraEm: null));

    private static CookieOptions Opcoes(DateTimeOffset? expiraEm) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = Path,
        Expires = expiraEm,
        IsEssential = true,
    };
}
