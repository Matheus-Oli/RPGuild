using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes;

[Collection(IntegrationCollection.Name)]
public sealed class ListarNotificacoesTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Listar_VariasNotificacoes_MaisNovasPrimeiroComTipoDadosELida()
    {
        // Arrange: o register já criou as boas-vindas agora; as outras são do passado.
        var logado = await _factory.CriarUsuarioELogarAsync();
        var agora = DateTimeOffset.UtcNow;
        var ha3h = await _factory.CriarNotificacaoAsync(logado.Id, agora.AddHours(-3), lida: true);
        var ha1h = await _factory.CriarNotificacaoAsync(
            logado.Id, agora.AddHours(-1), "amizade_aceita", """{"userId":"00000000-0000-0000-0000-000000000001","nome":"Bilbo","nick":"bilbo"}""");
        var ha2h = await _factory.CriarNotificacaoAsync(logado.Id, agora.AddHours(-2));

        // Act
        var pagina = await _factory.PaginaAsync(logado);

        // Assert
        pagina.Itens.Skip(1).Select(n => n.Id).ShouldBe([ha1h, ha2h, ha3h]);
        pagina.Itens[0].Tipo.ShouldBe("boas_vindas");
        pagina.ProximoCursor.ShouldBeNull();

        var aceita = pagina.Itens[1];
        aceita.Tipo.ShouldBe("amizade_aceita");
        aceita.Dados.GetProperty("nome").GetString().ShouldBe("Bilbo");
        aceita.Lida.ShouldBeFalse();
        pagina.Itens[3].Lida.ShouldBeTrue();
    }

    [Fact]
    public async Task Listar_45Notificacoes_TresPaginasSemRepetirNemPularMesmoComUmaNovaNoMeio()
    {
        // Arrange: 44 no passado (5 delas no mesmo instante, para o desempate pelo id) + as boas-vindas.
        var logado = await _factory.CriarUsuarioELogarAsync();
        var inicio = DateTimeOffset.UtcNow.AddDays(-1);
        for (var i = 0; i < 39; i++)
        {
            await _factory.CriarNotificacaoAsync(logado.Id, inicio.AddMinutes(i));
        }

        for (var i = 0; i < 5; i++)
        {
            await _factory.CriarNotificacaoAsync(logado.Id, inicio.AddMinutes(20).AddSeconds(30));
        }

        var esperadas = (await _factory.NotificacoesNoBancoAsync(logado.Id)).Select(n => n.Id).ToList();
        esperadas.Count.ShouldBe(45);

        // Act
        var primeira = await _factory.PaginaAsync(logado);
        await _factory.CriarNotificacaoAsync(logado.Id);
        var segunda = await _factory.PaginaAsync(logado, $"?cursor={primeira.ProximoCursor}");
        var terceira = await _factory.PaginaAsync(logado, $"?cursor={segunda.ProximoCursor}");

        // Assert
        primeira.Itens.Count.ShouldBe(20);
        segunda.Itens.Count.ShouldBe(20);
        terceira.Itens.Count.ShouldBe(5);
        terceira.ProximoCursor.ShouldBeNull();

        var vistas = primeira.Itens.Concat(segunda.Itens).Concat(terceira.Itens).Select(n => n.Id).ToList();
        vistas.ShouldBe(esperadas);
    }

    [Fact]
    public async Task Listar_LimiteAcimaDe50_DevolveNoMaximo50()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        for (var i = 0; i < 59; i++)
        {
            await _factory.CriarNotificacaoAsync(logado.Id, DateTimeOffset.UtcNow.AddMinutes(-i - 1));
        }

        // Act
        var pagina = await _factory.PaginaAsync(logado, "?limite=100");

        // Assert
        pagina.Itens.Count.ShouldBe(50);
        pagina.ProximoCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task Listar_LimiteMenor_DevolveSoOLimite()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        await _factory.CriarNotificacaoAsync(logado.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        await _factory.CriarNotificacaoAsync(logado.Id, DateTimeOffset.UtcNow.AddMinutes(-2));

        // Act
        var pagina = await _factory.PaginaAsync(logado, "?limite=2");

        // Assert
        pagina.Itens.Count.ShouldBe(2);
        pagina.ProximoCursor.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("?cursor=lixo")]
    [InlineData("?cursor=bm9wZQ")]
    [InlineData("?limite=0")]
    [InlineData("?limite=-1")]
    [InlineData("?limite=abc")]
    public async Task Listar_CursorOuLimiteInvalido_Retorna400(string query)
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.ListarAsync(logado, query);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_NaoLidas_FiltraAsLidas()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        await _factory.CriarNotificacaoAsync(logado.Id, DateTimeOffset.UtcNow.AddMinutes(-1), lida: true);
        var naoLida = await _factory.CriarNotificacaoAsync(logado.Id, DateTimeOffset.UtcNow.AddMinutes(-2));

        // Act
        var pagina = await _factory.PaginaAsync(logado, "?naoLidas=true");

        // Assert: as boas-vindas e a não lida.
        pagina.Itens.ShouldAllBe(n => !n.Lida);
        pagina.Itens.Select(n => n.Id).ShouldContain(naoLida);
        pagina.Itens.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Listar_OutroUsuarioComNotificacoes_SoVemAsMinhas()
    {
        // Arrange
        var (eu, outro) = (await _factory.CriarUsuarioELogarAsync(), await _factory.CriarUsuarioELogarAsync());
        await _factory.CriarNotificacaoAsync(outro.Id, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var pagina = await _factory.PaginaAsync(eu);

        // Assert
        var minhas = (await _factory.NotificacoesNoBancoAsync(eu.Id)).Select(n => n.Id);
        pagina.Itens.Select(n => n.Id).ShouldBe(minhas);
    }

    [Fact]
    public async Task Listar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(ClienteDeNotificacoes.Url);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
