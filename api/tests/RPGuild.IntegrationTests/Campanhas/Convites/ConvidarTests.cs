using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class ConvidarTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Convidar_MestreConvidaAmigos_Retorna201ComOsConvitesEAvisaCadaUm()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");

        // Act
        var resposta = await _factory.ConvidarAsync(mestre, campanha.Id, [.. amigos.Select(a => a.Id)]);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var convites = (await resposta.Content.ReadFromJsonAsync<List<ConviteDaCampanhaResponse>>())!;
        convites.Select(c => c.Convidado).ShouldBe(
            [.. amigos.Select(a => new PessoaDaCampanhaResponse(a.Id, a.Dados.Nome, a.Dados.Nick, null))]);
        foreach (var (convite, amigo) in convites.Zip(amigos))
        {
            (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("pendente");
            var aviso = (await _factory.NotificacoesNoBancoAsync(amigo.Id)).Where(n => n.Tipo == "convite_recebido").ShouldHaveSingleItem();
            JsonDocument.Parse(aviso.Dados).RootElement.GetProperty("conviteId").GetGuid().ShouldBe(convite.Id);
            _factory.Fila.Enfileiradas.ShouldContain(m => m.Para == amigo.Dados.Email && m.Texto.Contains($"/convites/{convite.Id}"));
        }
    }

    [Fact]
    public async Task Convidar_NaoAmigo_Retorna400ENaoConvidaNinguem()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var estranho = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.ConvidarAsync(mestre, campanha.Id, amigos[0].Id, estranho.Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await resposta.ConvidadosInvalidosAsync()).ShouldBe([new ConvidadoInvalidoResponse(estranho.Id, "nao_amigo")]);
        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(0);
        (await _factory.NotificacoesNoBancoAsync(amigos[0].Id)).ShouldNotContain(n => n.Tipo == "convite_recebido");
    }

    [Fact]
    public async Task Convidar_OProprioMestre_Retorna400()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.ConvidarAsync(mestre, campanha.Id, mestre.Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await resposta.ConvidadosInvalidosAsync()).ShouldBe([new ConvidadoInvalidoResponse(mestre.Id, "mestre")]);
    }

    [Fact]
    public async Task Convidar_QuemJaEJogador_Retorna409ENaoConvidaNinguem()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[0]);

        // Act
        var resposta = await _factory.ConvidarAsync(mestre, campanha.Id, amigos[1].Id, amigos[0].Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.ConvidadosInvalidosAsync()).ShouldBe([new ConvidadoInvalidoResponse(amigos[0].Id, "ja_jogador")]);
        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Convidar_QuemJaTemConvitePendente_Retorna409()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0]);

        // Act
        var resposta = await _factory.ConvidarAsync(mestre, campanha.Id, amigos[0].Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.ConvidadosInvalidosAsync()).ShouldBe([new ConvidadoInvalidoResponse(amigos[0].Id, "ja_convidado")]);
        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task Convidar_ErrosDe400EDe409Juntos_Retorna400ComTodos()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var estranho = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[0]);

        // Act
        var resposta = await _factory.ConvidarAsync(mestre, campanha.Id, amigos[0].Id, estranho.Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await resposta.ConvidadosInvalidosAsync()).ShouldBe(
        [
            new ConvidadoInvalidoResponse(amigos[0].Id, "ja_jogador"),
            new ConvidadoInvalidoResponse(estranho.Id, "nao_amigo"),
        ]);
    }

    [Fact]
    public async Task Convidar_ListaVaziaOuRepetida_Retorna400()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var vazia = await _factory.ConvidarAsync(mestre, campanha.Id);
        var repetida = await _factory.ConvidarAsync(mestre, campanha.Id, amigos[0].Id, amigos[0].Id);

        // Assert
        vazia.DeveSerProblema(HttpStatusCode.BadRequest);
        repetida.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Convidar_Jogador_Retorna403()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[0]);
        await _factory.TornarAmigosAsync(amigos[0], amigos[1]);

        // Act
        var resposta = await _factory.ConvidarAsync(amigos[0], campanha.Id, amigos[1].Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Forbidden);
        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Convidar_QuemNaoParticipa_Retorna404()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.ConvidarAsync(amigos[0], campanha.Id, mestre.Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Convidar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(ConvitesDeTeste.ConvitesDaCampanha(Guid.NewGuid()), new { userIds = new[] { Guid.NewGuid() } });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
