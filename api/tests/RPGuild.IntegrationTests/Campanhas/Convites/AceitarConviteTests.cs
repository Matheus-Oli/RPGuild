using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class AceitarConviteTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Aceitar_Convidado_Retorna204EViraJogadorComACampanhaNoInicioDaGrade()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var convidado = amigos[0];
        var propria = await _factory.CriarCampanhaAsync(convidado);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, convidado)).ShouldHaveSingleItem();

        // Act
        var resposta = await _factory.AceitarAsync(convidado, convite.Id);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.CampanhasDeAsync(convidado)).Select(c => (c.Id, c.Papel)).ShouldBe([(campanha.Id, "jogador"), (propria.Id, "mestre")]);
        var membros = await (await _factory.GetAsync(convidado, CampanhasDeTeste.Membros(campanha.Id))).Content
            .ReadFromJsonAsync<List<MembroResponse>>();
        membros!.Select(m => m.UserId).ShouldBe([mestre.Id, convidado.Id]);

        var card = (await (await _factory.GetAsync(convidado, ConvitesDeTeste.Convite(convite.Id))).Content
            .ReadFromJsonAsync<CardDoConviteResponse>())!;
        card.Status.ShouldBe("aceito");
        card.PodeResponder.ShouldBeFalse();
        (await (await _factory.GetAsync(convidado, ConvitesDeTeste.MeusConvites)).Content.ReadFromJsonAsync<List<MeuConviteResponse>>())!
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task Aceitar_Convidado_OMestreRecebeOAvisoNoSininho()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var convidado = amigos[0];
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, convidado)).ShouldHaveSingleItem();
        var emailsDoMestre = _factory.EmailsPara(mestre);

        // Act
        (await _factory.AceitarAsync(convidado, convite.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        var aviso = (await _factory.NotificacoesNoBancoAsync(mestre.Id)).Where(n => n.Tipo == "convite_aceito").ShouldHaveSingleItem();
        var dados = JsonDocument.Parse(aviso.Dados).RootElement;
        dados.GetProperty("conviteId").GetGuid().ShouldBe(convite.Id);
        dados.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id);
        dados.GetProperty("campanhaNome").GetString().ShouldBe("A Mina Perdida");
        dados.GetProperty("userId").GetGuid().ShouldBe(convidado.Id);
        dados.GetProperty("nome").GetString().ShouldBe(convidado.Dados.Nome);
        dados.GetProperty("nick").GetString().ShouldBe(convidado.Dados.Nick);
        _factory.EmailsPara(mestre).ShouldBe(emailsDoMestre, "o aviso nº 10 é só no sininho");
        (await _factory.NotificacoesNoBancoAsync(mestre.Id)).ShouldNotContain(n => n.Tipo == "jogador_entrou");
    }

    [Fact]
    public async Task Aceitar_Convidado_OsOutrosJogadoresRecebemOAvisoNoSininhoEPorEmail()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(3);
        var (convidado, jogadores) = (amigos[0], amigos[1..]);
        var campanha = await _factory.CriarCampanhaAsync(mestre, nome: "A Mina Perdida");
        foreach (var jogador in jogadores)
        {
            await _factory.AdicionarJogadorAsync(campanha.Id, jogador);
        }

        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, convidado)).ShouldHaveSingleItem();

        // Act
        (await _factory.AceitarAsync(convidado, convite.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        foreach (var jogador in jogadores)
        {
            var aviso = (await _factory.NotificacoesNoBancoAsync(jogador.Id)).Where(n => n.Tipo == "jogador_entrou").ShouldHaveSingleItem();
            var dados = JsonDocument.Parse(aviso.Dados).RootElement;
            dados.GetProperty("campanhaId").GetGuid().ShouldBe(campanha.Id);
            dados.GetProperty("campanhaNome").GetString().ShouldBe("A Mina Perdida");
            dados.GetProperty("userId").GetGuid().ShouldBe(convidado.Id);
            dados.GetProperty("nome").GetString().ShouldBe(convidado.Dados.Nome);
            dados.GetProperty("nick").GetString().ShouldBe(convidado.Dados.Nick);

            var email = _factory.Fila.Enfileiradas.Where(m => m.Para == jogador.Dados.Email && m.Assunto.Contains("entrou na campanha"))
                .ShouldHaveSingleItem();
            email.Texto.ShouldContain($"{RPGuildApiFactory.FrontBaseUrl}/campanhas/{campanha.Id}");
        }

        (await _factory.NotificacoesNoBancoAsync(convidado.Id)).ShouldNotContain(n => n.Tipo == "jogador_entrou");
        _factory.Fila.Enfileiradas.ShouldNotContain(m => m.Para == convidado.Dados.Email && m.Assunto.Contains("entrou na campanha"));
    }

    [Fact]
    public async Task Aceitar_OutroUsuario_Retorna404ENaoViraJogador()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();

        foreach (var outro in new[] { mestre, amigos[1] })
        {
            // Act
            var resposta = await _factory.AceitarAsync(outro, convite.Id);

            // Assert
            resposta.DeveSerProblema(HttpStatusCode.NotFound);
        }

        (await _factory.StatusNoBancoAsync(convite.Id)).ShouldBe("pendente");
        (await _factory.CampanhasDeAsync(amigos[1])).ShouldBeEmpty();
    }

    [Fact]
    public async Task Aceitar_DeNovo_Retorna409()
    {
        // Arrange
        var (mestre, amigos) = await _factory.CriarMestreComAmigosAsync(1);
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        var convite = (await _factory.ConvidarComSucessoAsync(mestre, campanha.Id, amigos[0])).ShouldHaveSingleItem();
        (await _factory.AceitarAsync(amigos[0], convite.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var resposta = await _factory.AceitarAsync(amigos[0], convite.Id);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await _factory.NotificacoesNoBancoAsync(mestre.Id)).Count(n => n.Tipo == "convite_aceito").ShouldBe(1);
    }

    [Fact]
    public async Task Aceitar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsync(ConvitesDeTeste.Aceitar(Guid.NewGuid()), content: null);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
