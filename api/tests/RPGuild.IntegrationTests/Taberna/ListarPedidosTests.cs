using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class ListarPedidosTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListarPedidos_VariosPedidos_SoPendentesDosDoisLadosMaisNovosPrimeiro()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(7);
        var (a, b, c, d, e, f, g) = (usuarios[0], usuarios[1], usuarios[2], usuarios[3], usuarios[4], usuarios[5], usuarios[6]);

        var paraB = await _factory.PedirComSucessoAsync(a, b);
        var deC = await _factory.PedirComSucessoAsync(c, a);
        var paraG = await _factory.PedirComSucessoAsync(a, g);
        var deF = await _factory.PedirComSucessoAsync(f, a);

        // Fora da lista: um pedido recusado e uma amizade aceita.
        var deD = await _factory.PedirComSucessoAsync(d, a);
        (await _factory.PostAsync(a, $"{ClienteDaTaberna.Pedidos}/{deD.Id}/decline")).EnsureSuccessStatusCode();
        await _factory.TornarAmigosAsync(a, e);

        // Act
        var pedidos = await _factory.PedidosDeAsync(a);

        // Assert
        pedidos.Recebidos.Select(p => p.Id).ShouldBe([deF.Id, deC.Id]);
        pedidos.Enviados.Select(p => p.Id).ShouldBe([paraG.Id, paraB.Id]);
        // O Postgres guarda microssegundos; a resposta do POST traz os ticks do .NET.
        pedidos.Recebidos[0].De.ShouldBe(new PessoaResponse(f.Id, f.Dados.Nome, f.Dados.Nick, null));
        pedidos.Recebidos[0].CriadoEm.ShouldBe(deF.CriadoEm, TimeSpan.FromMilliseconds(1));
        pedidos.Enviados[1].Para.ShouldBe(paraB.Para);
        pedidos.Enviados[1].CriadoEm.ShouldBe(paraB.CriadoEm, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task ListarPedidos_PedidoFeitoPorEmail_MostraONickENaoTemEmailEmLugarNenhum()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(3);
        var (a, b, c) = (usuarios[0], usuarios[1], usuarios[2]);
        (await _factory.PedirAsync(a, b.Dados.Email)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await _factory.PedirAsync(c, a.Dados.Email)).StatusCode.ShouldBe(HttpStatusCode.Created);
        using var cliente = _factory.CriarClienteAutenticado(a.AccessToken);

        // Act
        var json = await cliente.GetStringAsync(ClienteDaTaberna.Pedidos);

        // Assert
        RespostaSemEmail.Conferir(json, a, b, c);
        var pedidos = await _factory.PedidosDeAsync(a);
        pedidos.Enviados.ShouldHaveSingleItem().Para.Nick.ShouldBe(b.Dados.Nick);
        pedidos.Recebidos.ShouldHaveSingleItem().De.Nick.ShouldBe(c.Dados.Nick);
    }

    [Fact]
    public async Task ListarPedidos_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(ClienteDaTaberna.Pedidos);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
