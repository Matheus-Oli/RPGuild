using System.Diagnostics;
using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Health;

[Collection(IntegrationCollection.Name)]
public class HealthTests(PostgresFixture postgres)
{
    // Porta 1 no localhost: ninguém escuta, a conexão é recusada.
    private const string ConnectionStringSemBanco =
        "Host=127.0.0.1;Port=1;Database=rpguild;Username=rpguild;Password=rpguild";

    private static readonly TimeSpan TempoMaximoComBancoFora = TimeSpan.FromSeconds(2);

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

    [Fact]
    public async Task GetHealth_BancoIndisponivel_Retorna503Unhealthy()
    {
        // Arrange
        await using var factory = new RPGuildApiFactory(ConnectionStringSemBanco);
        using var cliente = factory.CreateClient();
        var cronometro = Stopwatch.StartNew();

        // Act
        var resposta = await cliente.GetAsync("/health");

        // Assert
        cronometro.Stop();
        resposta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await resposta.Content.ReadAsStringAsync()).ShouldBe("Unhealthy");
        cronometro.Elapsed.ShouldBeLessThan(TempoMaximoComBancoFora);
    }
}
