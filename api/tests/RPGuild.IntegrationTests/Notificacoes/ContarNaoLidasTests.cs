using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes;

[Collection(IntegrationCollection.Name)]
public sealed class ContarNaoLidasTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UnreadCount_LidasENaoLidasDeDoisUsuarios_ContaSoAsNaoLidasDoUsuario()
    {
        // Arrange: as boas-vindas + 2 não lidas + 1 lida; o outro usuário tem as dele.
        var (eu, outro) = (await _factory.CriarUsuarioELogarAsync(), await _factory.CriarUsuarioELogarAsync());
        await _factory.CriarNotificacaoAsync(eu.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        await _factory.CriarNotificacaoAsync(eu.Id, DateTimeOffset.UtcNow.AddMinutes(-2));
        await _factory.CriarNotificacaoAsync(eu.Id, DateTimeOffset.UtcNow.AddMinutes(-3), lida: true);
        await _factory.CriarNotificacaoAsync(outro.Id, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var total = await _factory.NaoLidasAsync(eu);

        // Assert
        total.ShouldBe(3);
    }

    [Fact]
    public async Task UnreadCount_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync($"{ClienteDeNotificacoes.Url}/unread-count");

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
