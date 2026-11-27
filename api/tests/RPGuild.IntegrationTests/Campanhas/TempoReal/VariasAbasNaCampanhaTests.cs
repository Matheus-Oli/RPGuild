using RPGuild.IntegrationTests.Campanhas.Convites;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class VariasAbasNaCampanhaTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Editar_MestreComDuasAbasNoGrupo_AsDuasRecebem()
    {
        // Arrange: duas abas (ou aparelhos), cada uma com a sua sessão.
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var outraSessao = await _factory.LogarAsync(mestre.Dados);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await using var abaUm = await _factory.EntrarNaCampanhaAsync(mestre, campanha.Id);
        await using var abaDois = await _factory.EntrarNaCampanhaAsync(outraSessao, campanha.Id);

        // Act
        await _factory.EditarAsync(mestre, campanha.Id, new { nome = "Outro nome" });

        // Assert
        (await abaUm.EsperarAsync("CampaignUpdated")).GetProperty("nome").GetString().ShouldBe("Outro nome");
        (await abaDois.EsperarAsync("CampaignUpdated")).GetProperty("nome").GetString().ShouldBe("Outro nome");
    }

    [Fact]
    public async Task Convidar_MestreComDuasAbasNoGrupo_AsDuasRecebemOInvitesChanged()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var outraSessao = await _factory.LogarAsync(mestre.Dados);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await using var abaUm = await _factory.EntrarNaCampanhaAsync(mestre, campanha.Id);
        await using var abaDois = await _factory.EntrarNaCampanhaAsync(outraSessao, campanha.Id);

        // Act
        await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0]);

        // Assert
        await abaUm.EsperarAsync("InvitesChanged");
        await abaDois.EsperarAsync("InvitesChanged");
    }
}
