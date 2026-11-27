using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Campanhas.Convites;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class MembroMudouTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AceitarConvite_MestreEJogadorNoGrupo_RecebemOMembroNoFormatoDoMembers()
    {
        // Arrange
        var (mestre, jogador, convidado, campanhaId, conviteId) = await PrepararAsync();
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanhaId);
        await using var doJogador = await _factory.EntrarNaCampanhaAsync(jogador, campanhaId);

        // Act
        (await _factory.AceitarAsync(convidado, conviteId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        var membros = await (await _factory.GetAsync(mestre, CampanhasDeTeste.Membros(campanhaId))).Content
            .ReadFromJsonAsync<List<MembroResponse>>();
        var esperado = membros!.Single(m => m.UserId == convidado.Id);
        foreach (var conexao in new[] { doMestre, doJogador })
        {
            var evento = await conexao.EsperarAsync("MemberChanged");
            evento.GetProperty("campanhaId").GetGuid().ShouldBe(campanhaId);
            evento.GetProperty("tipo").GetString().ShouldBe("entrou");
            evento.GetProperty("membro").Deserialize<MembroResponse>(JsonSerializerOptions.Web).ShouldBe(esperado);
            evento.GetProperty("membro").TryGetProperty("email", out _).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task AceitarConvite_UsuarioDeOutraCampanhaConectado_NaoRecebe()
    {
        // Arrange
        var (mestre, _, convidado, campanhaId, conviteId) = await PrepararAsync();
        var deFora = await _factory.CriarUsuarioELogarAsync();
        var outra = await _factory.CriarCampanhaAsync(deFora);
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanhaId);
        await using var naOutra = await _factory.EntrarNaCampanhaAsync(deFora, outra.Id);

        // Act
        await _factory.AceitarAsync(convidado, conviteId);

        // Assert: o mestre recebe, então o evento já saiu.
        await doMestre.EsperarAsync("MemberChanged");
        await naOutra.NaoRecebeAsync("MemberChanged");
    }

    [Fact]
    public async Task AceitarConvite_JogadorQueSaiuDoGrupo_NaoRecebe()
    {
        // Arrange
        var (mestre, jogador, convidado, campanhaId, conviteId) = await PrepararAsync();
        await using var doMestre = await _factory.EntrarNaCampanhaAsync(mestre, campanhaId);
        await using var doJogador = await _factory.EntrarNaCampanhaAsync(jogador, campanhaId);
        await doJogador.SairDaCampanhaAsync(campanhaId);

        // Act
        await _factory.AceitarAsync(convidado, conviteId);

        // Assert
        await doMestre.EsperarAsync("MemberChanged");
        await doJogador.NaoRecebeAsync("MemberChanged");
    }

    // Um mestre, um jogador já na mesa e um amigo com convite pendente.
    private async Task<(UsuarioLogado Mestre, UsuarioLogado Jogador, UsuarioLogado Convidado, Guid CampanhaId, Guid ConviteId)> PrepararAsync()
    {
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[0]);
        var conviteId = await _factory.ConvidarNoBancoAsync(campanha.Id, mestre, amigos[1]);
        return (mestre, amigos[0], amigos[1], campanha.Id, conviteId);
    }
}
