using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class CancelarConviteTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Cancelar_Mestre_Retorna204EOConvidadoRecebeOAvisoSoNoSininho()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        var emailsDoConvidado = _factory.EmailsPara(amigos[0]);

        // Act
        var resposta = await _factory.CancelarAsync(mestre, convite.Id);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("cancelado");
        var aviso = (await _factory.NotificacoesNoBancoAsync(amigos[0].Id)).Where(n => n.Tipo == "convite_cancelado").ShouldHaveSingleItem();
        var dados = JsonDocument.Parse(aviso.Dados).RootElement;
        dados.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id);
        dados.GetProperty("campanhaNome").GetString().ShouldBe("A Mina Perdida");
        _factory.EmailsPara(amigos[0]).ShouldBe(emailsDoConvidado);
    }

    [Fact]
    public async Task Cancelar_DepoisDeCancelado_OCardMostraCanceladoEAceitarDa409()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        (await _factory.CancelarAsync(mestre, convite.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var card = (await (await _factory.GetAsync(amigos[0], ConvitesDeTeste.Convite(convite.Id))).Content
            .ReadFromJsonAsync<CardDoConviteResponse>())!;
        var aceite = await _factory.AceitarAsync(amigos[0], convite.Id);
        var cancelarDeNovo = await _factory.CancelarAsync(mestre, convite.Id);

        // Assert
        card.Status.ShouldBe("cancelado");
        card.PodeResponder.ShouldBeFalse();
        aceite.DeveSerProblema(HttpStatusCode.Conflict);
        cancelarDeNovo.DeveSerProblema(HttpStatusCode.Conflict);
        (await _factory.CampanhasDeAsync(amigos[0])).ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancelar_JogadorOuConvidado_Retorna403ENaoCancela()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[1]);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();

        foreach (var quem in new[] { amigos[1], amigos[0] })
        {
            // Act
            var resposta = await _factory.CancelarAsync(quem, convite.Id);

            // Assert
            resposta.DeveSerProblema(HttpStatusCode.Forbidden);
        }

        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("pendente");
    }

    [Fact]
    public async Task Cancelar_QuemNaoParticipaOuConviteInexistente_Retorna404()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var estranho = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();

        // Act
        var deEstranho = await _factory.CancelarAsync(estranho, convite.Id);
        var inexistente = await _factory.CancelarAsync(mestre, Guid.NewGuid());

        // Assert
        deEstranho.DeveSerProblema(HttpStatusCode.NotFound);
        inexistente.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("pendente");
    }

    [Fact]
    public async Task Cancelar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.DeleteAsync(ConvitesDeTeste.Convite(Guid.NewGuid()));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
