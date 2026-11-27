using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class CampanhaAtualizadaTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Editar_MestreMudaACampanha_OGrupoRecebeOPayloadSemPapel()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, jogador);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanha.Id);
        await using var doJogador = await _factory.EntrarNaCampanhaAsync(jogador, campanha.Id);

        // Act
        var resposta = await _factory.EditarAsync(mestre, campanha.Id, new
        {
            nome = "Novo nome",
            descricao = "Nova descrição.",
            capaPath = mestre.NovaImagemDeCampanha(),
            bannerPath = mestre.NovaImagemDeCampanha(),
        });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var editada = (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!;
        foreach (var conexao in new[] { doMestre, doJogador })
        {
            var evento = await conexao.EsperarAsync("CampaignUpdated");
            evento.EnumerateObject().Select(p => p.Name).ShouldBe(["id", "nome", "descricao", "sistema", "capaUrl", "bannerUrl"]);
            evento.Deserialize<CampanhaAtualizadaResponse>(JsonSerializerOptions.Web).ShouldBe(
                new CampanhaAtualizadaResponse(editada.Id, "Novo nome", "Nova descrição.", editada.Sistema, editada.CapaUrl, editada.BannerUrl));
            editada.CapaUrl.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task Editar_IgualAoEstadoAtual_NaoMandaEvento()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanha.Id);

        // Act
        var resposta = await _factory.EditarAsync(
            mestre, campanha.Id, new { nome = campanha.Nome, descricao = campanha.Descricao, capaPath = (string?)null, bannerPath = (string?)null });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        await doMestre.NaoRecebeAsync("CampaignUpdated");
    }

    private sealed record CampanhaAtualizadaResponse(
        Guid Id, string Nome, string? Descricao, SistemaResponse Sistema, string? CapaUrl, string? BannerUrl);
}
