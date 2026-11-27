using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class VerConviteTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Ver_Convidado_VeTudoEPodeResponder()
    {
        // Arrange
        var (mestre, convidado, jogador, conviteId, campanha) = await PrepararAsync();

        // Act
        var resposta = await _factory.GetAsync(convidado, ConvitesDeTeste.Convite(conviteId));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var card = (await resposta.Content.ReadFromJsonAsync<CardDoConviteResponse>())!;
        card.Id.ShouldBe(conviteId);
        card.Status.ShouldBe("pendente");
        card.PodeResponder.ShouldBeTrue();
        card.Campanha.ShouldBe(new CampanhaDoCardResponse(
            campanha.Id, campanha.Nome, campanha.Descricao, new SistemaResponse("dnd2024", "D&D 5e (regras de 2024)"),
            campanha.CapaUrl, campanha.BannerUrl));
        card.Mestre.ShouldBe(mestre.ComoPessoa());
        card.NaMesa.ShouldBe([mestre.ComoPessoa(), jogador.ComoPessoa()]);
        card.ConvidadoPor.ShouldBe(new QuemConvidouResponse(mestre.Dados.Nome, mestre.Dados.Nick));
        card.Para.ShouldBe(new ParaQuemResponse(convidado.Dados.Nick));
        card.CriadoEm.ShouldNotBeNull();
    }

    [Fact]
    public async Task Ver_Mestre_VeTudoENaoPodeResponder()
    {
        // Arrange
        var (mestre, convidado, _, conviteId, campanha) = await PrepararAsync();

        // Act
        var resposta = await _factory.GetAsync(mestre, ConvitesDeTeste.Convite(conviteId));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var card = (await resposta.Content.ReadFromJsonAsync<CardDoConviteResponse>())!;
        card.PodeResponder.ShouldBeFalse();
        card.Status.ShouldBe("pendente");
        card.Campanha!.Id.ShouldBe(campanha.Id);
        card.Para.ShouldBe(new ParaQuemResponse(convidado.Dados.Nick));
    }

    [Fact]
    public async Task Ver_OutroUsuario_VeSoParaQuemENenhumDadoDaCampanha()
    {
        // Arrange
        var (mestre, convidado, jogador, conviteId, campanha) = await PrepararAsync();
        var estranho = await _factory.CriarUsuarioELogarAsync();

        foreach (var outro in new[] { estranho, jogador })
        {
            // Act
            var resposta = await _factory.GetAsync(outro, ConvitesDeTeste.Convite(conviteId));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var json = await resposta.Content.ReadAsStringAsync();
            var card = JsonDocument.Parse(json).RootElement;
            card.EnumerateObject().Select(p => p.Name).ShouldBe(["id", "podeResponder", "para"], ignoreOrder: true);
            card.GetProperty("id").GetGuid().ShouldBe(conviteId);
            card.GetProperty("podeResponder").GetBoolean().ShouldBeFalse();
            card.GetProperty("para").EnumerateObject().Select(p => (p.Name, p.Value.GetString())).ShouldBe([("nick", convidado.Dados.Nick)]);
            json.ShouldNotContain(campanha.Nome);
            json.ShouldNotContain(campanha.Id.ToString());
            json.ShouldNotContain(mestre.Dados.Nick);
        }
    }

    [Fact]
    public async Task Ver_ConviteJaAceito_MostraOStatusENaoDeixaResponder()
    {
        // Arrange
        var (mestre, convidado) = await _factory.CriarDoisUsuariosLogadosAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var conviteId = await _factory.ConvidarNoBancoAsync(campanha.Id, mestre, convidado, status: "aceito");

        // Act
        var card = (await (await _factory.GetAsync(convidado, ConvitesDeTeste.Convite(conviteId))).Content
            .ReadFromJsonAsync<CardDoConviteResponse>())!;

        // Assert
        card.Status.ShouldBe("aceito");
        card.PodeResponder.ShouldBeFalse();
    }

    [Fact]
    public async Task Ver_Resposta_NaoTemEmailDeNinguem()
    {
        // Arrange
        var (mestre, convidado, jogador, conviteId, _) = await PrepararAsync();

        // Act
        var resposta = await _factory.GetAsync(convidado, ConvitesDeTeste.Convite(conviteId));

        // Assert
        RespostaSemEmail.Conferir(await resposta.Content.ReadAsStringAsync(), mestre, convidado, jogador);
    }

    [Fact]
    public async Task Ver_ConviteInexistente_Retorna404()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.GetAsync(logado, ConvitesDeTeste.Convite(Guid.NewGuid()));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ver_SemToken_Retorna401()
    {
        // Arrange
        var (_, _, _, conviteId, _) = await PrepararAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(ConvitesDeTeste.Convite(conviteId));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Mestre com um jogador na mesa e um amigo convidado (pendente), numa campanha com capa e banner.
    private async Task<(UsuarioLogado Mestre, UsuarioLogado Convidado, UsuarioLogado Jogador, Guid ConviteId, DetalheResponse Campanha)>
        PrepararAsync()
    {
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(
            mestre, capaPath: mestre.NovaImagemDeCampanha(), bannerPath: mestre.NovaImagemDeCampanha());
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[1]);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        return (mestre, amigos[0], amigos[1], convite.Id, campanha);
    }
}
