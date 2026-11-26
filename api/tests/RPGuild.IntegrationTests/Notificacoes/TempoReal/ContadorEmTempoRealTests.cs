using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Notificacoes.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class ContadorEmTempoRealTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UnreadCount_CriarLerLerTodasEApagar_ChegaSempreComOTotalAtualizado()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        await using var hub = await ConexaoNoHub.AbrirAsync(_factory, b.AccessToken);

        // Act & Assert: criar (boas-vindas + pedido).
        await _factory.PedirComSucessoAsync(a, b);
        (await TotalAsync(hub)).ShouldBe(2);

        // Ler uma.
        var pedido = (await _factory.PaginaAsync(b)).Itens.Single(n => n.Tipo == "amizade_recebida");
        hub.Esquecer();
        (await _factory.PostAsync(b, ClienteDeNotificacoes.Ler(pedido.Id))).EnsureSuccessStatusCode();
        (await TotalAsync(hub)).ShouldBe(1);

        // Ler todas.
        hub.Esquecer();
        (await _factory.PostAsync(b, ClienteDeNotificacoes.LerTodas)).EnsureSuccessStatusCode();
        (await TotalAsync(hub)).ShouldBe(0);

        // Apagar uma não lida (as duas novas entram direto no banco, sem evento).
        var apagar = await _factory.CriarNotificacaoAsync(b.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        await _factory.CriarNotificacaoAsync(b.Id, DateTimeOffset.UtcNow.AddMinutes(-2));
        hub.Esquecer();
        (await _factory.DeleteAsync(b, ClienteDeNotificacoes.Notificacao(apagar))).EnsureSuccessStatusCode();
        (await TotalAsync(hub)).ShouldBe(1);
    }

    private static async Task<int> TotalAsync(ConexaoNoHub hub) =>
        (await hub.EsperarAsync("UnreadCount")).GetProperty("total").GetInt32();
}
