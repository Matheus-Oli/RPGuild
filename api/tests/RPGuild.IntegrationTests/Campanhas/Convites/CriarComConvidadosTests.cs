using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class CriarComConvidadosTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Criar_ComAmigosConvidados_Retorna201ComConvitesPendentesEAvisaCadaUm()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);

        // Act
        var resposta = await _factory.CriarAsync(mestre, new
        {
            nome = "A Mina Perdida",
            sistema = CampanhasDeTeste.SistemaAtivo,
            convidados = amigos.Select(a => a.Id),
        });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var campanha = (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!;
        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(2);
        foreach (var amigo in amigos)
        {
            var aviso = (await _factory.NotificacoesNoBancoAsync(amigo.Id)).Where(n => n.Tipo == "convite_recebido").ShouldHaveSingleItem();
            var dados = JsonDocument.Parse(aviso.Dados).RootElement;
            var conviteId = dados.GetProperty("conviteId").GetGuid();
            dados.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id);
            dados.GetProperty("campanhaNome").GetString().ShouldBe("A Mina Perdida");
            dados.GetProperty("mestreNome").GetString().ShouldBe(mestre.Dados.Nome);
            dados.GetProperty("mestreNick").GetString().ShouldBe(mestre.Dados.Nick);
            (await _factory.StatusNoBancoAsync(conviteId)).ShouldBe("pendente");

            var email = _factory.Fila.Enfileiradas.Where(m => m.Para == amigo.Dados.Email && m.Assunto.Contains("te convidou")).ShouldHaveSingleItem();
            email.Texto.ShouldContain($"{RPGuildApiFactory.FrontBaseUrl}/convites/{conviteId}");
        }
    }

    [Fact]
    public async Task Criar_ComUmNaoAmigo_Retorna400ComOMotivoENaoCriaNadaNemAvisa()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var estranho = await _factory.CriarUsuarioELogarAsync();
        var emailsDoAmigo = _factory.EmailsPara(amigos[0]);

        // Act
        var resposta = await _factory.CriarAsync(mestre, new
        {
            nome = "A Mina Perdida",
            sistema = CampanhasDeTeste.SistemaAtivo,
            convidados = new[] { amigos[0].Id, estranho.Id },
        });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await ConvidadosInvalidosAsync(resposta)).ShouldBe([new ConvidadoInvalidoResponse(estranho.Id, "nao_amigo")]);
        (await _factory.CampanhasNoBancoAsync(mestre.Id)).ShouldBe(0);
        (await _factory.NotificacoesNoBancoAsync(amigos[0].Id)).ShouldNotContain(n => n.Tipo == "convite_recebido");
        _factory.EmailsPara(amigos[0]).ShouldBe(emailsDoAmigo);
    }

    [Fact]
    public async Task Criar_ConvidandoOProprioMestre_Retorna400ENaoCria()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.CriarAsync(
            mestre, new { nome = "Campanha", sistema = CampanhasDeTeste.SistemaAtivo, convidados = new[] { mestre.Id } });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await ConvidadosInvalidosAsync(resposta)).ShouldBe([new ConvidadoInvalidoResponse(mestre.Id, "mestre")]);
        (await _factory.CampanhasNoBancoAsync(mestre.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Criar_ComConvidadoRepetido_Retorna400()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);

        // Act
        var resposta = await _factory.CriarAsync(
            mestre, new { nome = "Campanha", sistema = CampanhasDeTeste.SistemaAtivo, convidados = new[] { amigos[0].Id, amigos[0].Id } });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.CampanhasNoBancoAsync(mestre.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Criar_SemConvidados_Retorna201SemConvites()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();

        // Act
        var campanha = await _factory.CriarCampanhaAsync(mestre);

        // Assert
        (await _factory.ConvitesNoBancoAsync(campanha.Id)).ShouldBe(0);
    }

    private static async Task<List<ConvidadoInvalidoResponse>> ConvidadosInvalidosAsync(HttpResponseMessage resposta)
    {
        var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        return problema.GetProperty("convidados").Deserialize<List<ConvidadoInvalidoResponse>>(JsonSerializerOptions.Web)!;
    }
}
