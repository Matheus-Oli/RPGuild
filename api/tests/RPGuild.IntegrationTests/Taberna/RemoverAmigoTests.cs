using System.Net;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class RemoverAmigoTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Remover_Amigo_Retorna204ESomeDaListaDosDoisSemAvisarORemovido()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        await _factory.TornarAmigosAsync(a, b);
        var emailsDeB = _factory.Emails.Enviadas.Count(m => m.Para == b.Dados.Email);

        // Act
        var resposta = await _factory.DeleteAsync(a, Url(b.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.AmigosDeAsync(a)).ShouldBeEmpty();
        (await _factory.AmigosDeAsync(b)).ShouldBeEmpty();
        _factory.Emails.Enviadas.Count(m => m.Para == b.Dados.Email).ShouldBe(emailsDeB);
    }

    [Fact]
    public async Task Remover_QuemFoiAdicionadoTambemPodeRemover_Retorna204()
    {
        // Arrange: o pedido foi de A; quem remove é B.
        var (a, b) = await DoisUsuariosAsync();
        await _factory.TornarAmigosAsync(a, b);

        // Act
        var resposta = await _factory.DeleteAsync(b, Url(a.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.AmigosDeAsync(a)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Remover_NaoSaoAmigos_Retorna404()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();

        // Act
        var resposta = await _factory.DeleteAsync(a, Url(b.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Remover_SoTemPedidoPendente_Retorna404EMantemOPedido()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        await _factory.PedirComSucessoAsync(a, b);

        // Act
        var resposta = await _factory.DeleteAsync(a, Url(b.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.PedidosDeAsync(b)).Recebidos.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Remover_DepoisOsDoisPodemPedirDeNovo_Retorna201()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        await _factory.TornarAmigosAsync(a, b);
        (await _factory.DeleteAsync(a, Url(b.Id))).EnsureSuccessStatusCode();

        // Act
        var resposta = await _factory.PedirAsync(b, a.Dados.Nick);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Remover_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.DeleteAsync(Url(Guid.NewGuid()));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string Url(Guid userId) => $"{ClienteDaTaberna.Amigos}/{userId}";

    private async Task<(UsuarioLogado A, UsuarioLogado B)> DoisUsuariosAsync()
    {
        var usuarios = await _factory.CriarUsuariosLogadosAsync(2);
        return (usuarios[0], usuarios[1]);
    }
}
