using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Notificacoes.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class TokenForaDoLogTests(PostgresFixture postgres)
{
    [Fact]
    public async Task ConectarNoHub_LogDoAspNetEmInformation_TokenDaQueryNaoVaiParaOLog()
    {
        // Arrange: alguém sobe o log do ASP.NET para depurar; o log de requisição imprimiria a URL com a query.
        var logs = new LogsCapturados();
        await using var factory = new RPGuildApiFactory(
            postgres,
            configurarServicos: services => services.AddSingleton<ILoggerProvider>(logs),
            configuracao: new Dictionary<string, string?> { ["Logging:LogLevel:Microsoft.AspNetCore"] = "Information" });
        var (a, b) = await factory.CriarDoisUsuariosLogadosAsync();

        // Act: conectar e receber um evento passa várias vezes pelo hub com o token na query.
        await using (var hub = await ConexaoNoHub.AbrirAsync(factory, b.AccessToken))
        {
            await factory.PedirComSucessoAsync(a, b);
            await hub.EsperarAsync("NotificationCreated");
        }

        // Assert
        logs.Linhas.ShouldNotBeEmpty();
        var vazadas = logs.Linhas.Where(linha => linha.Contains(b.AccessToken)).Select(l => l.Split(':')[0]).Distinct();
        vazadas.ShouldBeEmpty("O access token apareceu no log destas categorias.");
    }
}
