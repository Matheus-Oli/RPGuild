using System.Net;
using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Notificacoes;

[Collection(IntegrationCollection.Name)]
public sealed class LerNotificacoesTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Read_NotificacaoMinha_Retorna204MarcaComoLidaEOContadorCai()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var id = await _factory.CriarNotificacaoAsync(logado.Id, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var resposta = await _factory.PostAsync(logado, ClienteDeNotificacoes.Ler(id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.NaoLidasAsync(logado)).ShouldBe(1);
        (await _factory.PaginaAsync(logado)).Itens.Single(n => n.Id == id).Lida.ShouldBeTrue();
    }

    [Fact]
    public async Task Read_DeNovo_Continua204()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var id = await _factory.CriarNotificacaoAsync(logado.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        (await _factory.PostAsync(logado, ClienteDeNotificacoes.Ler(id))).EnsureSuccessStatusCode();

        // Act
        var resposta = await _factory.PostAsync(logado, ClienteDeNotificacoes.Ler(id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.NaoLidasAsync(logado)).ShouldBe(1);
    }

    [Fact]
    public async Task Read_NotificacaoDeOutroUsuario_Retorna404SemMarcar()
    {
        // Arrange
        var (eu, outro) = (await _factory.CriarUsuarioELogarAsync(), await _factory.CriarUsuarioELogarAsync());
        var deOutro = await _factory.CriarNotificacaoAsync(outro.Id, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var resposta = await _factory.PostAsync(eu, ClienteDeNotificacoes.Ler(deOutro));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.NaoLidasAsync(outro)).ShouldBe(2);
    }

    [Fact]
    public async Task Read_IdInexistente_Retorna404()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.PostAsync(logado, ClienteDeNotificacoes.Ler(Guid.NewGuid()));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadAll_VariasNaoLidas_ZeraOContadorSemMexerNasDeOutros()
    {
        // Arrange
        var (eu, outro) = (await _factory.CriarUsuarioELogarAsync(), await _factory.CriarUsuarioELogarAsync());
        await _factory.CriarNotificacaoAsync(eu.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        await _factory.CriarNotificacaoAsync(eu.Id, DateTimeOffset.UtcNow.AddMinutes(-2));

        // Act
        var resposta = await _factory.PostAsync(eu, ClienteDeNotificacoes.LerTodas);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.NaoLidasAsync(eu)).ShouldBe(0);
        (await _factory.PaginaAsync(eu)).Itens.ShouldAllBe(n => n.Lida);
        (await _factory.NaoLidasAsync(outro)).ShouldBe(1);
    }

    [Theory]
    [InlineData("read")]
    [InlineData("read-all")]
    public async Task Ler_SemToken_Retorna401(string rota)
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var url = rota == "read" ? ClienteDeNotificacoes.Ler(Guid.NewGuid()) : ClienteDeNotificacoes.LerTodas;

        // Act
        var resposta = await cliente.PostAsync(url, content: null);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
