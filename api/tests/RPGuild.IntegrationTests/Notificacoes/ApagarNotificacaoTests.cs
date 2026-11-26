using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes;

[Collection(IntegrationCollection.Name)]
public sealed class ApagarNotificacaoTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Delete_NotificacaoMinha_Retorna204ESomeDaListaEDoContador()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var id = await _factory.CriarNotificacaoAsync(logado.Id, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var resposta = await _factory.DeleteAsync(logado, ClienteDeNotificacoes.Notificacao(id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.PaginaAsync(logado)).Itens.ShouldNotContain(n => n.Id == id);
        (await _factory.NaoLidasAsync(logado)).ShouldBe(1);
    }

    [Fact]
    public async Task Delete_NotificacaoDeOutroUsuario_Retorna404EMantem()
    {
        // Arrange
        var (eu, outro) = (await _factory.CriarUsuarioELogarAsync(), await _factory.CriarUsuarioELogarAsync());
        var deOutro = await _factory.CriarNotificacaoAsync(outro.Id, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var resposta = await _factory.DeleteAsync(eu, ClienteDeNotificacoes.Notificacao(deOutro));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.NotificacoesNoBancoAsync(outro.Id)).ShouldContain(n => n.Id == deOutro);
    }

    [Fact]
    public async Task Delete_IdInexistente_Retorna404()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.DeleteAsync(logado, ClienteDeNotificacoes.Notificacao(Guid.NewGuid()));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.DeleteAsync(ClienteDeNotificacoes.Notificacao(Guid.NewGuid()));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
