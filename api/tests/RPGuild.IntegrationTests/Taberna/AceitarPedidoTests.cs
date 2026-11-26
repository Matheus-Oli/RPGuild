using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class AceitarPedidoTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Aceitar_DestinatarioDePedidoPendente_Retorna204EOsDoisViramAmigos()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);

        // Act
        var resposta = await _factory.PostAsync(b, Url(pedido.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.AmigosDeAsync(a)).ShouldHaveSingleItem().UserId.ShouldBe(b.Id);
        (await _factory.AmigosDeAsync(b)).ShouldHaveSingleItem().UserId.ShouldBe(a.Id);

        foreach (var lado in new[] { a, b })
        {
            var pedidos = await _factory.PedidosDeAsync(lado);
            pedidos.Recebidos.ShouldBeEmpty();
            pedidos.Enviados.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Aceitar_QuemNaoEODestinatario_Retorna404()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(3);
        var (a, b, terceiro) = (usuarios[0], usuarios[1], usuarios[2]);
        var pedido = await _factory.PedirComSucessoAsync(a, b);

        // Act
        var peloRemetente = await _factory.PostAsync(a, Url(pedido.Id));
        var porTerceiro = await _factory.PostAsync(terceiro, Url(pedido.Id));

        // Assert
        peloRemetente.DeveSerProblema(HttpStatusCode.NotFound);
        porTerceiro.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.AmigosDeAsync(a)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Aceitar_PedidoInexistente_Retorna404()
    {
        // Arrange
        var a = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.PostAsync(a, Url(Guid.NewGuid()));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Aceitar_PedidoJaRespondido_Retorna409()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);
        (await _factory.PostAsync(b, Url(pedido.Id))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var resposta = await _factory.PostAsync(b, Url(pedido.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Aceitar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsync(Url(Guid.NewGuid()), content: null);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string Url(Guid pedidoId) => $"{ClienteDaTaberna.Pedidos}/{pedidoId}/accept";

    private async Task<(UsuarioLogado A, UsuarioLogado B)> DoisUsuariosAsync()
    {
        var usuarios = await _factory.CriarUsuariosLogadosAsync(2);
        return (usuarios[0], usuarios[1]);
    }
}
