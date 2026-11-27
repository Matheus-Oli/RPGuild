using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class TokenForaDoLogNaCampanhaTests(PostgresFixture postgres)
{
    [Fact]
    public async Task UsarOHubDaCampanha_LogDoAspNetEmInformation_TokenDaQueryNaoVaiParaOLog()
    {
        // Arrange: alguém sobe o log do ASP.NET para depurar; o log de requisição imprimiria a URL com a query.
        var logs = new LogsCapturados();
        await using var factory = new RPGuildApiFactory(
            postgres,
            configurarServicos: services => services.AddSingleton<ILoggerProvider>(logs),
            configuracao: new Dictionary<string, string?> { ["Logging:LogLevel:Microsoft.AspNetCore"] = "Information" });
        var mestre = await factory.CriarUsuarioELogarAsync();
        var campanha = await factory.CriarCampanhaAsync(mestre);

        // Act: entrar, ser recusado, receber um evento e sair passam várias vezes pelo hub com o token na query.
        await using (var conexao = await factory.EntrarNaCampanhaAsync(mestre, campanha.Id))
        {
            await Should.ThrowAsync<HubException>(() => conexao.EntrarNaCampanhaAsync(Guid.NewGuid()));
            await factory.EditarAsync(mestre, campanha.Id, new { nome = "Outro nome" });
            await conexao.EsperarAsync("CampaignUpdated");
            await conexao.SairDaCampanhaAsync(campanha.Id);
        }

        // Assert
        logs.Linhas.ShouldNotBeEmpty();
        var vazadas = logs.Linhas.Where(linha => linha.Contains(mestre.AccessToken)).Select(l => l.Split(':')[0]).Distinct();
        vazadas.ShouldBeEmpty("O access token apareceu no log destas categorias.");
    }
}
