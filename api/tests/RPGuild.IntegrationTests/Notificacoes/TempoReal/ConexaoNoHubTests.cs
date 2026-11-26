using System.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Time.Testing;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class ConexaoNoHubTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Conectar_TokenNaQuery_Conecta()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        await using var conexao = await ConexaoNoHub.AbrirAsync(_factory, logado.AccessToken);

        // Assert: sem exceção ao abrir.
        conexao.Recebidos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Conectar_SemToken_ERecusado()
    {
        // Act & Assert
        var erro = await Should.ThrowAsync<HttpRequestException>(() => ConexaoNoHub.AbrirAsync(_factory, accessToken: null));
        erro.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Conectar_TokenInvalido_ERecusado()
    {
        // Act & Assert
        var erro = await Should.ThrowAsync<HttpRequestException>(() => ConexaoNoHub.AbrirAsync(_factory, "nao-e-um-jwt"));
        erro.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Conectar_TokenExpiradoHaUmMinuto_ERecusado()
    {
        // Arrange: o token é emitido 16 min no passado, então venceu há 1 min.
        await using var factoryNoPassado = new RPGuildApiFactory(postgres, new FakeTimeProvider(DateTimeOffset.UtcNow.AddMinutes(-16)));
        var logado = await factoryNoPassado.CriarUsuarioELogarAsync();

        // Act & Assert
        var erro = await Should.ThrowAsync<HttpRequestException>(() => ConexaoNoHub.AbrirAsync(_factory, logado.AccessToken));
        erro.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndpointRest_TokenNaQueryForaDeHubs_NaoAutentica()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync($"/api/me?access_token={Uri.EscapeDataString(logado.AccessToken)}");

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
