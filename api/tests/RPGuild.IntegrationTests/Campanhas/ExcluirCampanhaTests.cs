using System.Net;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas;

[Collection(IntegrationCollection.Name)]
public sealed class ExcluirCampanhaTests(PostgresFixture postgres) : IDisposable
{
    private const string Frase = "desejo excluir a campanha";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Excluir_ConfirmacaoCerta_Retorna204EASomeDoGetDeTodos()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre, nome: "Maldição de Strahd");
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, criada.Id, "  Maldição de Strahd ", $" {Frase}  ");

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.CampanhasDeAsync(mestre)).ShouldBeEmpty();
        (await _factory.CampanhasDeAsync(jogador)).ShouldBeEmpty();
        (await _factory.GetAsync(mestre, CampanhasDeTeste.Campanha(criada.Id))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Excluir_ConfirmacaoCerta_ApagaACampanhaOsMembrosEAsPosicoesNaGrade()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);
        var outra = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, criada.Id, criada.Nome, Frase);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.LinhasDaCampanhaAsync(criada.Id)).ShouldBe((0, 0, 0));
        (await _factory.OrdemNoBancoAsync(mestre.Id)).ShouldBe([outra.Id]);
    }

    [Fact]
    public async Task Excluir_ComCapaEBanner_ApagaAsDuasDoStorage()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var (capa, banner) = (mestre.NovaImagemDeCampanha(), mestre.NovaImagemDeCampanha());
        var criada = await _factory.CriarCampanhaAsync(mestre, capaPath: capa, bannerPath: banner);

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, criada.Id, criada.Nome, Frase);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _factory.Storage.Apagados.ShouldBe([capa, banner], ignoreOrder: true);
    }

    [Fact]
    public async Task Excluir_FalhaAoApagarAsImagens_Retorna204EApagaACampanha()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre, capaPath: mestre.NovaImagemDeCampanha());
        _factory.Storage.FalharAoApagar = true;

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, criada.Id, criada.Nome, Frase);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.LinhasDaCampanhaAsync(criada.Id)).Campanhas.ShouldBe(0);
    }

    [Fact]
    public async Task Excluir_ConfirmacaoCerta_AvisaOsJogadoresNoSininhoEPorEmail()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(3);
        var (mestre, jogadores) = (usuarios[0], usuarios[1..]);
        var criada = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");
        foreach (var jogador in jogadores)
        {
            await _factory.AdicionarJogadorAsync(criada.Id, jogador);
        }

        var emailsDoMestre = _factory.EmailsPara(mestre);

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, criada.Id, criada.Nome, Frase);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        foreach (var jogador in jogadores)
        {
            var aviso = (await _factory.NotificacoesNoBancoAsync(jogador.Id)).Where(n => n.Tipo == "campanha_excluida").ShouldHaveSingleItem();
            var dados = JsonDocument.Parse(aviso.Dados).RootElement;
            dados.GetProperty("campanhaNome").GetString().ShouldBe("A Mina Perdida");
            dados.GetProperty("mestreNome").GetString().ShouldBe(mestre.Dados.Nome);
            _factory.Fila.Enfileiradas.ShouldContain(m =>
                m.Para == jogador.Dados.Email && m.Assunto == "A campanha A Mina Perdida foi excluída");
        }

        (await _factory.NotificacoesNoBancoAsync(mestre.Id)).ShouldNotContain(n => n.Tipo == "campanha_excluida");
        _factory.EmailsPara(mestre).ShouldBe(emailsDoMestre);
    }

    [Theory]
    [InlineData("maldição de strahd", Frase)]
    [InlineData("Maldicao de Strahd", Frase)]
    [InlineData("Maldição de Strahd", "Desejo excluir a campanha")]
    [InlineData("Maldição de Strahd", "desejo excluir a campanha!")]
    [InlineData("Maldição de Strahd", "desejo  excluir a campanha")]
    [InlineData("Outra campanha", Frase)]
    public async Task Excluir_ConfirmacaoErrada_Retorna400ENaoApagaNada(string confirmacaoNome, string confirmacaoFrase)
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre, nome: "Maldição de Strahd", capaPath: mestre.NovaImagemDeCampanha());
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, criada.Id, confirmacaoNome, confirmacaoFrase);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.LinhasDaCampanhaAsync(criada.Id)).ShouldBe((1, 1, 2));
        _factory.Storage.Apagados.ShouldBeEmpty();
        (await _factory.NotificacoesNoBancoAsync(jogador.Id)).ShouldNotContain(n => n.Tipo == "campanha_excluida");
    }

    [Theory]
    [InlineData(null, Frase)]
    [InlineData("Nome", null)]
    [InlineData("  ", Frase)]
    public async Task Excluir_ConfirmacaoVazia_Retorna400(string? confirmacaoNome, string? confirmacaoFrase)
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre, nome: "Nome");

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, criada.Id, confirmacaoNome, confirmacaoFrase);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.LinhasDaCampanhaAsync(criada.Id)).Campanhas.ShouldBe(1);
    }

    [Fact]
    public async Task Excluir_Jogador_Retorna403ENaoApaga()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.ExcluirAsync(jogador, criada.Id, criada.Nome, Frase);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Forbidden);
        (await _factory.LinhasDaCampanhaAsync(criada.Id)).Campanhas.ShouldBe(1);
    }

    [Fact]
    public async Task Excluir_QuemNaoParticipa_Retorna404ENaoApaga()
    {
        // Arrange
        var (mestre, estranho) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.ExcluirAsync(estranho, criada.Id, criada.Nome, Frase);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.LinhasDaCampanhaAsync(criada.Id)).Campanhas.ShouldBe(1);
    }

    [Fact]
    public async Task Excluir_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.DeleteAsync(CampanhasDeTeste.Campanha(Guid.NewGuid()));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
