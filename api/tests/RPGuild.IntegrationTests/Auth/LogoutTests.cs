using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class LogoutTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Logout_CookieValido_Retorna204ApagaOCookieERevogaOToken()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.SendAsync(RefreshCookie.Post("/api/auth/logout", logado.RefreshCookie));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var cookie = RefreshCookie.Ler(resposta);
        cookie.ShouldNotBeNull();
        cookie.Value.ToString().ShouldBeEmpty();
        cookie.Path.ToString().ShouldBe("/api/auth");
        cookie.Expires!.Value.ShouldBeLessThan(DateTimeOffset.UtcNow);

        var refresh = await cliente.SendAsync(RefreshCookie.Post("/api/auth/refresh", logado.RefreshCookie));
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
