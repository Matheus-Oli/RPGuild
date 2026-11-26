using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Notificacoes.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class NotificacaoCriadaTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task PedidoDeAmizade_DestinatarioConectado_RecebeOItemNoFormatoDoGet()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        await using var hubDeB = await ConexaoNoHub.AbrirAsync(_factory, b.AccessToken);

        // Act
        var pedido = await _factory.PedirComSucessoAsync(a, b);

        // Assert
        var evento = await hubDeB.EsperarAsync("NotificationCreated");
        var item = (await _factory.PaginaAsync(b)).Itens[0];
        item.Tipo.ShouldBe("amizade_recebida");

        evento.GetProperty("id").GetGuid().ShouldBe(item.Id);
        evento.GetProperty("tipo").GetString().ShouldBe("amizade_recebida");
        evento.GetProperty("lida").GetBoolean().ShouldBeFalse();
        evento.GetProperty("criadoEm").GetDateTimeOffset().ShouldBe(item.CriadoEm);
        evento.GetProperty("dados").GetProperty("pedidoId").GetGuid().ShouldBe(pedido.Id);
        evento.GetProperty("dados").GetProperty("deNick").GetString().ShouldBe(a.Dados.Nick);
        evento.EnumerateObject().Select(p => p.Name).ShouldBe(["id", "tipo", "dados", "lida", "criadoEm"], ignoreOrder: true);
    }

    [Fact]
    public async Task PedidoDeAmizade_QuemPediuEUmTerceiroConectados_NaoRecebem()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(3);
        var (a, b, terceiro) = (usuarios[0], usuarios[1], usuarios[2]);
        await using var hubDeA = await ConexaoNoHub.AbrirAsync(_factory, a.AccessToken);
        await using var hubDoTerceiro = await ConexaoNoHub.AbrirAsync(_factory, terceiro.AccessToken);
        await using var hubDeB = await ConexaoNoHub.AbrirAsync(_factory, b.AccessToken);

        // Act
        await _factory.PedirComSucessoAsync(a, b);

        // Assert: B recebe (o evento saiu), A e o terceiro não.
        await hubDeB.EsperarAsync("NotificationCreated");
        await hubDeA.NaoRecebeAsync("NotificationCreated");
        await hubDoTerceiro.NaoRecebeAsync("NotificationCreated");
    }
}
