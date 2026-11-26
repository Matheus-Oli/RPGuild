using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Notificacoes.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class VariasConexoesTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task PedidoDeAmizade_DestinatarioComDuasConexoes_AsDuasRecebem()
    {
        // Arrange: duas abas (ou aparelhos), cada uma com a sua sessão.
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var outraSessaoDeB = await _factory.LogarAsync(b.Dados);
        await using var abaUm = await ConexaoNoHub.AbrirAsync(_factory, b.AccessToken);
        await using var abaDois = await ConexaoNoHub.AbrirAsync(_factory, outraSessaoDeB.AccessToken);

        // Act
        await _factory.PedirComSucessoAsync(a, b);

        // Assert
        var naUm = await abaUm.EsperarAsync("NotificationCreated");
        var naDois = await abaDois.EsperarAsync("NotificationCreated");
        naDois.GetProperty("id").GetGuid().ShouldBe(naUm.GetProperty("id").GetGuid());
    }
}
