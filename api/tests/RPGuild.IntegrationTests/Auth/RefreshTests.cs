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
}
