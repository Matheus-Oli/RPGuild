using System.Net;
using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Campanhas;

[Collection(IntegrationCollection.Name)]
public sealed class ListarCampanhasTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Listar_MestreEJogador_OsDoisVeemACampanhaComOPapelDeCada()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var capa = $"campaigns/{mestre.Id}/{Guid.NewGuid()}.png";
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "Maldição de Strahd", capaPath: capa);
        await _factory.AdicionarJogadorAsync(campanha.Id, jogador);

        // Act
        var doMestre = (await _factory.CampanhasDeAsync(mestre)).ShouldHaveSingleItem();
        var doJogador = (await _factory.CampanhasDeAsync(jogador)).ShouldHaveSingleItem();

        // Assert
        doMestre.Papel.ShouldBe("mestre");
        doJogador.Papel.ShouldBe("jogador");
        foreach (var card in new[] { doMestre, doJogador })
        {
            card.Id.ShouldBe(campanha.Id);
            card.Nome.ShouldBe("Maldição de Strahd");
            card.Sistema.ShouldBe(new SistemaResponse("dnd2024", "D&D 5e (regras de 2024)"));
            card.CapaUrl.ShouldBe(_factory.Storage.ObterUrlPublica(capa));
            card.Mestre.ShouldBe(new PessoaDaCampanhaResponse(mestre.Id, mestre.Dados.Nome, mestre.Dados.Nick, null));
            card.Jogadores.ShouldBe([new PessoaDaCampanhaResponse(jogador.Id, jogador.Dados.Nome, jogador.Dados.Nick, null)]);
        }
    }

    [Fact]
    public async Task Listar_QuemNaoParticipa_NaoVeACampanha()
    {
        // Arrange
        var (mestre, estranho) = await _factory.CriarDoisUsuariosLogadosAsync();
        await _factory.CriarCampanhaAsync(mestre);

        // Act
        var campanhas = await _factory.CampanhasDeAsync(estranho);

        // Assert
        campanhas.ShouldBeEmpty();
    }

    [Fact]
    public async Task Listar_QuemNuncaReordenou_VeAMaisNovaPrimeiro()
    {
        // Arrange
        var (mestre, criadas) = await _factory.CriarUsuarioComCampanhasAsync(3);

        // Act
        var campanhas = await _factory.CampanhasDeAsync(mestre);

        // Assert
        campanhas.Select(c => c.Id).ShouldBe([criadas[2], criadas[1], criadas[0]]);
    }

    [Fact]
    public async Task Listar_JogadorQueEntrouDepois_VeACampanhaEmQueEntrouNoInicio()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var propria = await _factory.CriarCampanhaAsync(jogador);
        var outra = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(outra.Id, jogador);

        // Act
        var campanhas = await _factory.CampanhasDeAsync(jogador);

        // Assert
        campanhas.Select(c => (c.Id, c.Papel)).ShouldBe([(outra.Id, "jogador"), (propria.Id, "mestre")]);
    }

    [Fact]
    public async Task Listar_CamposDeOutrosModulos_VemVazios()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        await _factory.CriarCampanhaAsync(mestre);

        // Act
        var card = (await _factory.CampanhasDeAsync(mestre)).ShouldHaveSingleItem();

        // Assert
        card.ProximaSessao.ShouldBeNull();
        card.CapaUrl.ShouldBeNull();
        card.Jogadores.ShouldBeEmpty();
    }

    [Fact]
    public async Task Listar_Resposta_NaoTemEmailDeNinguem()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, jogador);
        using var cliente = _factory.CriarClienteAutenticado(jogador.AccessToken);

        // Act
        var json = await cliente.GetStringAsync(CampanhasDeTeste.Url);

        // Assert
        RespostaSemEmail.Conferir(json, mestre, jogador);
    }

    [Fact]
    public async Task Listar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(CampanhasDeTeste.Url);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
