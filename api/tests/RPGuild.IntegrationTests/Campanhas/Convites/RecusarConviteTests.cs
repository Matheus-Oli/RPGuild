using System.Net;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class RecusarConviteTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Recusar_Convidado_Retorna204ENaoViraJogador()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();

        // Act
        var resposta = await _factory.RecusarAsync(amigos[0], convite.Id);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("recusado");
        (await _factory.CampanhasDeAsync(amigos[0])).ShouldBeEmpty();
        (await _factory.LinhasDaCampanhaAsync(campanha.Id)).Membros.ShouldBe(0);
    }

    [Fact]
    public async Task Recusar_Convidado_OMestreRecebeOAvisoSoNoSininho()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        var emailsDoMestre = _factory.EmailsPara(mestre);

        // Act
        (await _factory.RecusarAsync(amigos[0], convite.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        var aviso = (await _factory.NotificacoesNoBancoAsync(mestre.Id)).Where(n => n.Tipo == "convite_recusado").ShouldHaveSingleItem();
        var dados = JsonDocument.Parse(aviso.Dados).RootElement;
        dados.GetProperty("conviteId").GetGuid().ShouldBe(convite.Id);
        dados.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id);
        dados.GetProperty("campanhaNome").GetString().ShouldBe("A Mina Perdida");
        dados.GetProperty("userId").GetGuid().ShouldBe(amigos[0].Id);
        dados.GetProperty("nome").GetString().ShouldBe(amigos[0].Dados.Nome);
        dados.GetProperty("nick").GetString().ShouldBe(amigos[0].Dados.Nick);
        _factory.EmailsPara(mestre).ShouldBe(emailsDoMestre);
    }

    [Fact]
    public async Task Recusar_DeNovo_Retorna409()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        (await _factory.RecusarAsync(amigos[0], convite.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var resposta = await _factory.RecusarAsync(amigos[0], convite.Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await _factory.NotificacoesNoBancoAsync(mestre.Id)).Count(n => n.Tipo == "convite_recusado").ShouldBe(1);
    }

    [Fact]
    public async Task Recusar_OutroUsuario_Retorna404()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();

        // Act
        var resposta = await _factory.RecusarAsync(amigos[1], convite.Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("pendente");
    }

    [Fact]
    public async Task Recusar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsync(ConvitesDeTeste.Recusar(Guid.NewGuid()), content: null);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
