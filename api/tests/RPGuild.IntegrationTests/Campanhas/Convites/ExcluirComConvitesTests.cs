using System.Net;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class ExcluirComConvitesTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Excluir_ComConvitePendente_OConvidadoRecebeOCanceladoEOJogadorOExcluida()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(3);
        var (jogador, pendente, recusou) = (amigos[0], amigos[1], amigos[2]);
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");
        await _factory.AdicionarJogadorAsync(campanha.Id, jogador);
        await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, pendente);
        await _factory.ConvidarNoBancoAsync(campanha.Id, mestre, recusou, status: "recusado");

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, campanha.Id, "A Mina Perdida", "desejo excluir a campanha");

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var cancelado = (await _factory.NotificacoesNoBancoAsync(pendente.Id)).Where(n => n.Tipo == "convite_cancelado").ShouldHaveSingleItem();
        var dados = JsonDocument.Parse(cancelado.Dados).RootElement;
        dados.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id);
        dados.GetProperty("campanhaNome").GetString().ShouldBe("A Mina Perdida");
        (await _factory.NotificacoesNoBancoAsync(pendente.Id)).ShouldNotContain(n => n.Tipo == "campanha_excluida");

        (await _factory.NotificacoesNoBancoAsync(jogador.Id)).ShouldContain(n => n.Tipo == "campanha_excluida");
        (await _factory.NotificacoesNoBancoAsync(jogador.Id)).ShouldNotContain(n => n.Tipo == "convite_cancelado");
        (await _factory.NotificacoesNoBancoAsync(recusou.Id)).ShouldNotContain(n => n.Tipo == "convite_cancelado" || n.Tipo == "campanha_excluida");

        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Excluir_ConfirmacaoErrada_NaoAvisaQuemTemConvitePendente()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");
        await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0]);

        // Act
        var resposta = await _factory.ExcluirAsync(mestre, campanha.Id, "a mina perdida", "desejo excluir a campanha");

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.NotificacoesNoBancoAsync(amigos[0].Id)).ShouldNotContain(n => n.Tipo == "convite_cancelado");
        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(1);
    }
}
