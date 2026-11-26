using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class CancelarPedidoTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Cancelar_QuemEnviouPedidoPendente_Retorna204ESomeDosDoisLados()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);

        // Act
        var resposta = await _factory.DeleteAsync(a, ClienteDaTaberna.Pedido(pedido.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.PedidosDeAsync(a)).Enviados.ShouldBeEmpty();
        (await _factory.PedidosDeAsync(b)).Recebidos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancelar_DestinatarioTentaCancelar_Retorna404()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);

        // Act
        var resposta = await _factory.DeleteAsync(b, ClienteDaTaberna.Pedido(pedido.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.PedidosDeAsync(b)).Recebidos.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Cancelar_PedidoJaRespondido_Retorna409()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);
        (await _factory.PostAsync(b, ClienteDaTaberna.Recusar(pedido.Id))).EnsureSuccessStatusCode();

        // Act
        var resposta = await _factory.DeleteAsync(a, ClienteDaTaberna.Pedido(pedido.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancelar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.DeleteAsync(ClienteDaTaberna.Pedido(Guid.NewGuid()));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
