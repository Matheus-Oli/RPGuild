using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Health;

[Collection(IntegrationCollection.Name)]
public class HealthTests(PostgresFixture postgres)
{
    [Fact]
    public async Task GetHealth_BancoDisponivel_Retorna200Healthy()
    {
        // Arrange
        await using var factory = new RPGuildApiFactory(postgres);
        using var cliente = factory.CreateClient();

        // Act
        var resposta = await cliente.GetAsync("/health");

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}
