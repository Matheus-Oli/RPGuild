using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class RefreshTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/auth/refresh";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Refresh_CookieValido_Retorna200ComNovoAccessTokenENovoCookie()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.SendAsync(RefreshCookie.Post(Url, logado.RefreshCookie));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = (await resposta.Content.ReadFromJsonAsync<TokenResponse>())!;
        corpo.AccessToken.ShouldNotBeNullOrWhiteSpace();
        corpo.AccessToken.ShouldNotBe(logado.AccessToken);

        var novoCookie = RefreshCookie.Extrair(resposta);
        novoCookie.ShouldNotBeNull();
        novoCookie.ShouldNotBe(logado.RefreshCookie);
    }

    [Fact]
    public async Task Refresh_SemCookie_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.SendAsync(RefreshCookie.Post(Url, cookie: null));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_CookieAntigoReutilizado_Retorna401ERevogaOCookieNovo()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarCliente();
        var rotacao = await cliente.SendAsync(RefreshCookie.Post(Url, logado.RefreshCookie));
        rotacao.EnsureSuccessStatusCode();
        var cookieNovo = RefreshCookie.Extrair(rotacao);

        // Act
        var reuso = await cliente.SendAsync(RefreshCookie.Post(Url, logado.RefreshCookie));
        var comCookieNovo = await cliente.SendAsync(RefreshCookie.Post(Url, cookieNovo));

        // Assert
        reuso.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        comCookieNovo.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
