using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class ReconviteTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Convidar_DepoisDeRecusado_Retorna201EOConvidadoConsegueAceitar()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var primeiro = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        (await _factory.RecusarAsync(amigos[0], primeiro.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var resposta = await _factory.ConvidarAsync(mestre, campanha.Id, amigos[0].Id);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        await AceitaONovoAsync(campanha.Id, amigos[0], primeiro.Id);
    }

    [Fact]
    public async Task Convidar_DepoisDeCancelado_Retorna201EOConvidadoConsegueAceitar()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var primeiro = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        (await _factory.CancelarAsync(mestre, primeiro.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var resposta = await _factory.ConvidarAsync(mestre, campanha.Id, amigos[0].Id);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        await AceitaONovoAsync(campanha.Id, amigos[0], primeiro.Id);
    }

    // O convite novo é outro (o antigo fica como histórico), e o convidado vira jogador ao aceitar.
    private async Task AceitaONovoAsync(Guid campanhaId, UsuarioLogado convidado, Guid antigo)
    {
        (await _factory.ConvitesNoBancoAsync(campanhaId)).ShouldBe(2);
        var meus = await (await _factory.GetAsync(convidado, ConvitesDeTeste.MeusConvites)).Content
            .ReadFromJsonAsync<List<MeuConviteResponse>>();
        var novo = meus!.ShouldHaveSingleItem();
        novo.Id.ShouldNotBe(antigo);

        (await _factory.AceitarAsync(convidado, novo.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.CampanhasDeAsync(convidado)).ShouldHaveSingleItem().Id.ShouldBe(campanhaId);
    }
}
