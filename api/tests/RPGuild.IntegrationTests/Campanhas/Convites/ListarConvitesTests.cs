using System.Net;
using System.Net.Http.Json;
using RPGuild.Infrastructure.Comum;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class ListarConvitesTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ConvitesDaCampanha_Mestre_VeSoOsPendentesComOsMaisNovosPrimeiro()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(3);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var agora = RelogioEmMicrossegundos.Truncar(DateTimeOffset.UtcNow);
        var antigo = await _factory.ConvidarNoBancoAsync(campanha.Id, mestre, amigos[0], criadoEm: agora.AddMinutes(-10));
        var novo = await _factory.ConvidarNoBancoAsync(campanha.Id, mestre, amigos[1], criadoEm: agora.AddMinutes(-5));
        await _factory.ConvidarNoBancoAsync(campanha.Id, mestre, amigos[2], status: "recusado", criadoEm: agora);

        // Act
        var resposta = await _factory.GetAsync(mestre, ConvitesDeTeste.ConvitesDaCampanha(campanha.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<List<ConviteDaCampanhaResponse>>())!.ShouldBe(
        [
            new ConviteDaCampanhaResponse(novo, Pessoa(amigos[1]), agora.AddMinutes(-5)),
            new ConviteDaCampanhaResponse(antigo, Pessoa(amigos[0]), agora.AddMinutes(-10)),
        ]);
    }

    [Fact]
    public async Task ConvitesDaCampanha_Jogador_Retorna403()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[0]);

        // Act
        var resposta = await _factory.GetAsync(amigos[0], ConvitesDeTeste.ConvitesDaCampanha(campanha.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConvitesDaCampanha_QuemNaoParticipa_Retorna404()
    {
        // Arrange
        var (mestre, estranho) = await _factory.CriarDoisUsuariosLogadosAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.GetAsync(estranho, ConvitesDeTeste.ConvitesDaCampanha(campanha.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MeusConvites_Convidado_VeSoOsSeusPendentesComOsMaisNovosPrimeiro()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var eu = amigos[0];
        var capa = mestre.NovaImagemDeCampanha();
        var primeira = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida", capaPath: capa);
        var segunda = await _factory.CriarCampanhaAsync(mestre, nome: "Maldição de Strahd");
        var terceira = await _factory.CriarCampanhaAsync(mestre, nome: "Tumba da Aniquilação");
        var agora = RelogioEmMicrossegundos.Truncar(DateTimeOffset.UtcNow);
        var antigo = await _factory.ConvidarNoBancoAsync(primeira.Id, mestre, eu, criadoEm: agora.AddMinutes(-10));
        var novo = await _factory.ConvidarNoBancoAsync(segunda.Id, mestre, eu, criadoEm: agora.AddMinutes(-5));
        await _factory.ConvidarNoBancoAsync(terceira.Id, mestre, eu, status: "cancelado", criadoEm: agora);
        await _factory.ConvidarNoBancoAsync(primeira.Id, mestre, amigos[1], criadoEm: agora);

        // Act
        var resposta = await _factory.GetAsync(eu, ConvitesDeTeste.MeusConvites);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dnd = new SistemaResponse("dnd2024", "D&D 5e (regras de 2024)");
        (await resposta.Content.ReadFromJsonAsync<List<MeuConviteResponse>>())!.ShouldBe(
        [
            new MeuConviteResponse(novo, new CampanhaDoConviteResponse(segunda.Id, "Maldição de Strahd", dnd, null), Pessoa(mestre), agora.AddMinutes(-5)),
            new MeuConviteResponse(
                antigo, new CampanhaDoConviteResponse(primeira.Id, "A Mina Perdida", dnd, _factory.Storage.ObterUrlPublica(capa)), Pessoa(mestre),
                agora.AddMinutes(-10)),
        ]);
    }

    [Fact]
    public async Task MeusConvites_Resposta_NaoTemEmailDeNinguem()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.ConvidarNoBancoAsync(campanha.Id, mestre, amigos[0]);

        // Act
        var resposta = await _factory.GetAsync(amigos[0], ConvitesDeTeste.MeusConvites);

        // Assert
        RespostaSemEmail.Conferir(await resposta.Content.ReadAsStringAsync(), mestre, amigos[0]);
    }

    [Fact]
    public async Task ConvitesDaCampanha_Resposta_NaoTemEmailDeNinguem()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.ConvidarNoBancoAsync(campanha.Id, mestre, amigos[0]);

        // Act
        var resposta = await _factory.GetAsync(mestre, ConvitesDeTeste.ConvitesDaCampanha(campanha.Id));

        // Assert
        RespostaSemEmail.Conferir(await resposta.Content.ReadAsStringAsync(), mestre, amigos[0]);
    }

    [Theory]
    [InlineData(ConvitesDeTeste.MeusConvites)]
    [InlineData("/api/campaigns/0193c0de-0000-7000-8000-000000000000/invites")]
    public async Task Listas_SemToken_Retorna401(string url)
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(url);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static PessoaDaCampanhaResponse Pessoa(UsuarioLogado usuario) => new(usuario.Id, usuario.Dados.Nome, usuario.Dados.Nick, null);
}
