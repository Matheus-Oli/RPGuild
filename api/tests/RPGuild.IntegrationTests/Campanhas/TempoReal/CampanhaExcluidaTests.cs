using System.Net;
using Microsoft.AspNetCore.SignalR;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class CampanhaExcluidaTests(PostgresFixture postgres) : IDisposable
{
    private const string Frase = "desejo excluir a campanha";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Excluir_Mestre_OGrupoRecebeOEvento()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, jogador);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanha.Id);
        await using var doJogador = await _factory.EntrarNaCampanhaAsync(jogador, campanha.Id);

        // Act
        (await _factory.ExcluirAsync(mestre, campanha.Id, campanha.Nome, Frase)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        foreach (var conexao in new[] { doMestre, doJogador })
        {
            var evento = await conexao.EsperarAsync("CampaignDeleted");
            evento.EnumerateObject().Select(p => p.Name).ShouldBe(["campanhaId", "campanhaNome"]);
            evento.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id);
            evento.GetProperty("campanhaNome").GetString().ShouldBe(campanha.Nome);
        }
    }

    [Fact]
    public async Task JoinCampaign_DepoisDeExcluida_RecebeCampanhaNaoEncontrada()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanha.Id);
        await _factory.ExcluirAsync(mestre, campanha.Id, campanha.Nome, Frase);
        await doMestre.EsperarAsync("CampaignDeleted");

        // Act
        var erro = await Should.ThrowAsync<HubException>(() => doMestre.EntrarNaCampanhaAsync(campanha.Id));

        // Assert
        erro.Message.ShouldEndWith("HubException: Campanha não encontrada");
    }
}
