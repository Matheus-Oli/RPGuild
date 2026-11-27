using System.Net;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class AmizadeDesfeitaTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemoverAmigo_ComConvitePendente_CancelaOConviteEAvisaOConvidado(bool quemRemoveEOMestre)
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var convidado = amigos[0];
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, convidado)).ShouldHaveSingleItem();
        var (quem, outro) = quemRemoveEOMestre ? (mestre, convidado) : (convidado, mestre);

        // Act
        var resposta = await _factory.DeleteAsync(quem, ClienteDaTaberna.Amigo(outro.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("cancelado");
        var aviso = (await _factory.NotificacoesNoBancoAsync(convidado.Id)).Where(n => n.Tipo == "convite_cancelado").ShouldHaveSingleItem();
        var dados = JsonDocument.Parse(aviso.Dados).RootElement;
        dados.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id);
        dados.GetProperty("campanhaNome").GetString().ShouldBe("A Mina Perdida");
        (await _factory.NotificacoesNoBancoAsync(mestre.Id)).ShouldNotContain(n => n.Tipo == "convite_cancelado");
    }

    [Fact]
    public async Task RemoverAmigo_ConvitesNosDoisSentidos_CancelaOsDoisEAvisaCadaConvidado()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        await _factory.TornarAmigosAsync(a, b);
        var deA = await _factory.CriarCampanhaAsync(a);
        var deB = await _factory.CriarCampanhaAsync(b);
        var paraB = (await _factory.ConvidarComSucessoAsync(a, deA.Id, b)).ShouldHaveSingleItem();
        var paraA = (await _factory.ConvidarComSucessoAsync(b, deB.Id, a)).ShouldHaveSingleItem();

        // Act
        (await _factory.DeleteAsync(a, ClienteDaTaberna.Amigo(b.Id))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        (await _factory.StatusNoBancoAsync(paraB.Id)).ShouldBe("cancelado");
        (await _factory.StatusNoBancoAsync(paraA.Id)).ShouldBe("cancelado");
        (await _factory.NotificacoesNoBancoAsync(a.Id)).Count(n => n.Tipo == "convite_cancelado").ShouldBe(1);
        (await _factory.NotificacoesNoBancoAsync(b.Id)).Count(n => n.Tipo == "convite_cancelado").ShouldBe(1);
    }

    [Fact]
    public async Task RemoverAmigo_ConviteJaAceito_NaoMudaEContinuaJogador()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        (await _factory.AceitarAsync(amigos[0], convite.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        (await _factory.DeleteAsync(mestre, ClienteDaTaberna.Amigo(amigos[0].Id))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("aceito");
        (await _factory.CampanhasDeAsync(amigos[0])).ShouldHaveSingleItem().Id.ShouldBe(campanha.Id);
        (await _factory.NotificacoesNoBancoAsync(amigos[0].Id)).ShouldNotContain(n => n.Tipo == "convite_cancelado");
    }

    [Fact]
    public async Task RemoverAmigo_ConviteDeOutroMestre_NaoMuda()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var outroMestre = await _factory.CriarUsuarioELogarAsync();
        await _factory.TornarAmigosAsync(outroMestre, amigos[0]);
        var campanha = await _factory.CriarCampanhaAsync(outroMestre);
        var convite = (await _factory.ConvidarComSucessoAsync(outroMestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();

        // Act
        (await _factory.DeleteAsync(mestre, ClienteDaTaberna.Amigo(amigos[0].Id))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("pendente");
    }
}
