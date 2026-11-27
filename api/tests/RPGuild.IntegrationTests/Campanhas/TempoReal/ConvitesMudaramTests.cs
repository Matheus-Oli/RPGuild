using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RPGuild.Application.Campanhas;
using RPGuild.IntegrationTests.Campanhas.Convites;
using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class ConvitesMudaramTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Convidar_SoOGrupoDoMestreRecebeComAcaoCriado()
    {
        // Arrange
        var (mestre, jogador, convidado, campanhaId) = await PrepararAsync();
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanhaId);
        await using var doJogador = await _factory.EntrarNaCampanhaAsync(jogador, campanhaId);

        // Act
        var criado = (await _factory.ConvidarComSucessoAsync(mestre, campanhaId, convidado)).Single();

        // Assert
        var evento = await doMestre.EsperarAsync("InvitesChanged");
        evento.GetProperty("campanhaId").GetGuid().ShouldBe(campanhaId);
        evento.GetProperty("acao").GetString().ShouldBe("criado");
        evento.GetProperty("convite").Deserialize<ConviteDoEventoResponse>(JsonSerializerOptions.Web)
            .ShouldBe(new ConviteDoEventoResponse(criado.Id, "pendente", convidado.ComoPessoa(), criado.CriadoEm));
        evento.GetProperty("convite").GetProperty("convidado").TryGetProperty("email", out _).ShouldBeFalse();
        await doJogador.NaoRecebeAsync("InvitesChanged");
    }

    [Fact]
    public async Task CriarCampanhaComConvidados_SaiOEventoCriadoMesmoSemNinguemNoGrupo()
    {
        // Arrange: ninguém pode estar no grupo de uma campanha que ainda não existe; o evento sai do mesmo jeito.
        var gravado = new TempoRealGravado();
        await using var factory = new RPGuildApiFactory(
            postgres,
            configurarServicos: services => services.RemoveAll<ICampanhaTempoReal>().AddSingleton<ICampanhaTempoReal>(gravado));
        var (mestre, amigos) = await factory.CriarMestreComAmigosAsync(1);

        // Act
        var resposta = await factory.CriarAsync(
            mestre, new { nome = "Com convidados", sistema = CampanhasDeTeste.SistemaAtivo, convidados = new[] { amigos[0].Id } });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var evento = gravado.ConvitesMudaram.ShouldHaveSingleItem();
        evento.Acao.ShouldBe("criado");
        evento.Convite.Convidado.UserId.ShouldBe(amigos[0].Id);
    }

    [Fact]
    public async Task Aceitar_OGrupoDoMestreRecebeComAcaoAceito()
    {
        // Arrange
        var (mestre, jogador, convidado, campanhaId) = await PrepararAsync();
        var conviteId = await _factory.ConvidarNoBancoAsync(campanhaId, mestre, convidado);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanhaId);
        await using var doJogador = await _factory.EntrarNaCampanhaAsync(jogador, campanhaId);

        // Act
        (await _factory.AceitarAsync(convidado, conviteId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        await DeveTerChegadoAsync(doMestre, conviteId, "aceito", "aceito", convidado);
        await doJogador.NaoRecebeAsync("InvitesChanged");
    }

    [Fact]
    public async Task Recusar_OGrupoDoMestreRecebeComAcaoRecusado()
    {
        // Arrange
        var (mestre, jogador, convidado, campanhaId) = await PrepararAsync();
        var conviteId = await _factory.ConvidarNoBancoAsync(campanhaId, mestre, convidado);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanhaId);
        await using var doJogador = await _factory.EntrarNaCampanhaAsync(jogador, campanhaId);

        // Act
        (await _factory.RecusarAsync(convidado, conviteId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        await DeveTerChegadoAsync(doMestre, conviteId, "recusado", "recusado", convidado);
        await doJogador.NaoRecebeAsync("InvitesChanged");
    }

    [Fact]
    public async Task Cancelar_OGrupoDoMestreRecebeComAcaoCancelado()
    {
        // Arrange
        var (mestre, jogador, convidado, campanhaId) = await PrepararAsync();
        var conviteId = await _factory.ConvidarNoBancoAsync(campanhaId, mestre, convidado);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanhaId);
        await using var doJogador = await _factory.EntrarNaCampanhaAsync(jogador, campanhaId);

        // Act
        (await _factory.CancelarAsync(mestre, conviteId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        await DeveTerChegadoAsync(doMestre, conviteId, "cancelado", "cancelado", convidado);
        await doJogador.NaoRecebeAsync("InvitesChanged");
    }

    [Fact]
    public async Task AmizadeDesfeita_OGrupoDoMestreRecebeComAcaoCancelado()
    {
        // Arrange
        var (mestre, _, convidado, campanhaId) = await PrepararAsync();
        var conviteId = await _factory.ConvidarNoBancoAsync(campanhaId, mestre, convidado);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanhaId);

        // Act
        (await _factory.DeleteAsync(convidado, ClienteDaTaberna.Amigo(mestre.Id))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        await DeveTerChegadoAsync(doMestre, conviteId, "cancelado", "cancelado", convidado);
    }

    // Um mestre, um jogador já na mesa e um amigo do mestre para convidar.
    private async Task<(UsuarioLogado Mestre, UsuarioLogado Jogador, UsuarioLogado Convidado, Guid CampanhaId)> PrepararAsync()
    {
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[0]);
        return (mestre, amigos[0], amigos[1], campanha.Id);
    }

    private static async Task DeveTerChegadoAsync(
        ConexaoNoHub conexao, Guid conviteId, string acao, string status, UsuarioLogado convidado)
    {
        var evento = await conexao.EsperarAsync("InvitesChanged");
        evento.GetProperty("acao").GetString().ShouldBe(acao);
        var convite = evento.GetProperty("convite").Deserialize<ConviteDoEventoResponse>(JsonSerializerOptions.Web)!;
        convite.Id.ShouldBe(conviteId);
        convite.Status.ShouldBe(status);
        convite.Convidado.ShouldBe(convidado.ComoPessoa());
    }

    private sealed record ConviteDoEventoResponse(Guid Id, string Status, PessoaDaCampanhaResponse Convidado, DateTimeOffset CriadoEm);
}
