using System.Net;

namespace RPGuild.IntegrationTests.Infra;

[Collection(IntegrationCollection.Name)]
public class ApiFactoryTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Get_RotaInexistente_RetornaNotFound()
    {
        // Arrange
        await using var factory = new RPGuildApiFactory(postgres);
        using var cliente = factory.CreateClient();

        // Act
        var resposta = await cliente.GetAsync("/rota-que-nao-existe");

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
