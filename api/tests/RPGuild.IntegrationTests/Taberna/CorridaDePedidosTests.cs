using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RPGuild.Infrastructure.Persistence;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class CorridaDePedidosTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Enviar_PedidoCruzadoGravadoAoMesmoTempo_Retorna409ComAMensagemDoPedidoCruzado()
    {
        // Arrange: o banco recusa o pedido novo pelo índice único do par, como na corrida A→B e B→A.
        await using var factory = new RPGuildApiFactory(
            postgres,
            configurarServicos: services => services.ConfigureDbContext<AppDbContext>(
                options => options.AddInterceptors(new ParDeAmizadeDuplicadoNoBanco())));
        var (a, b) = await factory.CriarDoisUsuariosLogadosAsync();

        // Act
        var resposta = await factory.PedirAsync(a, b.Dados.Nick);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.Content.ReadFromJsonAsync<ProblemaResponse>())!.Detail
            .ShouldBe("Essa pessoa já te enviou um pedido. Aceite na sua lista.");
    }
}
