using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Campanhas;

[Collection(IntegrationCollection.Name)]
public sealed class DetalheDaCampanhaTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Detalhe_Mestre_Retorna200ComOsCamposDaCampanha()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var banner = $"campaigns/{mestre.Id}/{Guid.NewGuid()}.png";
        var criada = await _factory.CriarCampanhaAsync(mestre, nome: "Tumba da Aniquilação", bannerPath: banner);
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.GetAsync(mestre, CampanhasDeTeste.Campanha(criada.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var detalhe = (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!;
        detalhe.Id.ShouldBe(criada.Id);
        detalhe.Nome.ShouldBe("Tumba da Aniquilação");
        detalhe.Descricao.ShouldBe("Uma aventura de teste.");
        detalhe.Papel.ShouldBe("mestre");
        detalhe.Sistema.ShouldBe(new SistemaResponse("dnd2024", "D&D 5e (regras de 2024)"));
        detalhe.BannerUrl.ShouldBe(_factory.Storage.ObterUrlPublica(banner));
        detalhe.CapaUrl.ShouldBeNull();
        detalhe.CriadaEm.ShouldBe(criada.CriadaEm);
        detalhe.Mestre.UserId.ShouldBe(mestre.Id);
        detalhe.Jogadores.Select(j => j.UserId).ShouldBe([jogador.Id]);
    }

    [Fact]
    public async Task Detalhe_Jogador_Retorna200ComPapelDeJogador()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.GetAsync(jogador, CampanhasDeTeste.Campanha(criada.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!.Papel.ShouldBe("jogador");
    }

    [Fact]
    public async Task Detalhe_CamposDeOutrosModulos_VemVazios()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.GetAsync(mestre, CampanhasDeTeste.Campanha(criada.Id));

        // Assert
        var detalhe = (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!;
        detalhe.ProximaSessao.ShouldBeNull();
        detalhe.OndeParamos.ShouldBeNull();
        detalhe.AtividadeRecente.ShouldBeEmpty();
        detalhe.Estatisticas.ShouldBe(new EstatisticasResponse(SessoesJogadas: 0, HorasDeMesa: 0, Documentos: 0));
    }

    [Fact]
    public async Task Detalhe_QuemNaoParticipa_Retorna404()
    {
        // Arrange
        var (mestre, estranho) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.GetAsync(estranho, CampanhasDeTeste.Campanha(criada.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Detalhe_CampanhaInexistente_Retorna404()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.GetAsync(logado, CampanhasDeTeste.Campanha(Guid.NewGuid()));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Detalhe_Resposta_NaoTemEmailDeNinguem()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.GetAsync(jogador, CampanhasDeTeste.Campanha(criada.Id));

        // Assert
        RespostaSemEmail.Conferir(await resposta.Content.ReadAsStringAsync(), mestre, jogador);
    }

    [Fact]
    public async Task Detalhe_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(CampanhasDeTeste.Campanha(Guid.NewGuid()));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
