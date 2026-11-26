using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class RecusarPedidoTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Recusar_DestinatarioDePedidoPendente_Retorna204ESomeDasListasSemVirarAmizade()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);

        // Act
        var resposta = await _factory.PostAsync(b, ClienteDaTaberna.Recusar(pedido.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        foreach (var lado in new[] { a, b })
        {
            var pedidos = await _factory.PedidosDeAsync(lado);
            pedidos.Recebidos.ShouldBeEmpty();
            pedidos.Enviados.ShouldBeEmpty();
            (await _factory.AmigosDeAsync(lado)).ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Recusar_DepoisQuemPediuReenvia_Retorna201ComPedidoPendenteNovo()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);
        (await _factory.PostAsync(b, ClienteDaTaberna.Recusar(pedido.Id))).EnsureSuccessStatusCode();

        // Act
        var resposta = await _factory.PedirAsync(a, b.Dados.Nick);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await _factory.PedidosDeAsync(b)).Recebidos.ShouldHaveSingleItem().De.UserId.ShouldBe(a.Id);
        (await _factory.PedidosDeAsync(a)).Enviados.ShouldHaveSingleItem().Para.UserId.ShouldBe(b.Id);
    }

    [Fact]
    public async Task Recusar_DepoisQuemRecusouPede_Retorna201NoOutroSentido()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);
        (await _factory.PostAsync(b, ClienteDaTaberna.Recusar(pedido.Id))).EnsureSuccessStatusCode();

        // Act
        var resposta = await _factory.PedirAsync(b, a.Dados.Nick);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await _factory.PedidosDeAsync(a)).Recebidos.ShouldHaveSingleItem().De.UserId.ShouldBe(b.Id);
        (await _factory.PedidosDeAsync(a)).Enviados.ShouldBeEmpty();
    }

    [Fact]
    public async Task Recusar_QuemNaoEODestinatario_Retorna404()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(3);
        var (a, b, terceiro) = (usuarios[0], usuarios[1], usuarios[2]);
        var pedido = await _factory.PedirComSucessoAsync(a, b);

        // Act
        var peloRemetente = await _factory.PostAsync(a, ClienteDaTaberna.Recusar(pedido.Id));
        var porTerceiro = await _factory.PostAsync(terceiro, ClienteDaTaberna.Recusar(pedido.Id));

        // Assert
        peloRemetente.DeveSerProblema(HttpStatusCode.NotFound);
        porTerceiro.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.PedidosDeAsync(b)).Recebidos.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Recusar_PedidoJaRespondido_Retorna409()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);
        (await _factory.PostAsync(b, ClienteDaTaberna.Aceitar(pedido.Id))).EnsureSuccessStatusCode();

        // Act
        var resposta = await _factory.PostAsync(b, ClienteDaTaberna.Recusar(pedido.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await _factory.AmigosDeAsync(b)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Recusar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsync(ClienteDaTaberna.Recusar(Guid.NewGuid()), content: null);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
