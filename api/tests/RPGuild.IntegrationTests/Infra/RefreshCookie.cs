using Microsoft.Net.Http.Headers;

namespace RPGuild.IntegrationTests.Infra;

public static class RefreshCookie
{
    public const string Nome = "rpguild_refresh";

    /// <summary>
    /// Lê o Set-Cookie do refresh na resposta.
    /// </summary>
    public static SetCookieHeaderValue? Ler(HttpResponseMessage resposta)
    {
        if (!resposta.Headers.TryGetValues(HeaderNames.SetCookie, out var valores))
        {
            return null;
        }

        return SetCookieHeaderValue.ParseList([.. valores])
            .SingleOrDefault(cookie => cookie.Name.Equals(Nome, StringComparison.Ordinal));
    }

    /// <summary>
    /// Devolve o par "nome=valor" pronto para o header Cookie.
    /// </summary>
    public static string? Extrair(HttpResponseMessage resposta)
    {
        var cookie = Ler(resposta);
        return cookie is null ? null : $"{cookie.Name}={cookie.Value}";
    }

    public static HttpRequestMessage Post(string url, string? cookie)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, url);
        if (cookie is not null)
        {
            requisicao.Headers.Add(HeaderNames.Cookie, cookie);
        }

        return requisicao;
    }
}
