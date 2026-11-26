using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class ErrosDoEnvioTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("@")]
    [InlineData("ab")]
    [InlineData("nick com espaco")]
    [InlineData("sem-dominio@")]
    public async Task Enviar_VazioOuInvalido_Retorna400ComErroNoCampo(string emailOuNick)
    {
        // Arrange
        var a = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.PedirAsync(a, emailOuNick);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("\"emailOuNick\"");
    }

    [Theory]
    [InlineData("nick")]
    [InlineData("email")]
    public async Task Enviar_ParaSiMesmo_Retorna400(string forma)
    {
        // Arrange
        var a = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.PedirAsync(a, forma == "nick" ? $"@{a.Dados.Nick}" : a.Dados.Email);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.PedidosDeAsync(a)).Enviados.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ninguem_tem_esse")]
    [InlineData("ninguem@nao-existe.rpguild")]
    public async Task Enviar_NinguemComEsseEmailOuNick_Retorna404(string emailOuNick)
    {
        // Arrange
        var a = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.PedirAsync(a, emailOuNick);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Enviar_JaSaoAmigos_Retorna409()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        await _factory.TornarAmigosAsync(b, a);

        // Act
        var resposta = await _factory.PedirAsync(a, b.Dados.Nick);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.Content.ReadFromJsonAsync<ProblemaResponse>())!.Title.ShouldBe("Já são amigos");
    }

    [Fact]
    public async Task Enviar_JaTemPedidoPendenteMeu_Retorna409()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        await _factory.PedirComSucessoAsync(a, b);

        // Act
        var resposta = await _factory.PedirAsync(a, b.Dados.Email);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.Content.ReadFromJsonAsync<ProblemaResponse>())!.Title.ShouldBe("Pedido já enviado");
    }

    [Fact]
    public async Task Enviar_JaTemPedidoPendenteDele_Retorna409ComAMensagemSemAceitar()
    {
        // Arrange
        var (a, b) = await DoisUsuariosAsync();
        await _factory.PedirComSucessoAsync(b, a);

        // Act
        var resposta = await _factory.PedirAsync(a, b.Dados.Nick);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.Content.ReadFromJsonAsync<ProblemaResponse>())!.Detail
            .ShouldBe("Essa pessoa já te enviou um pedido. Aceite na sua lista.");
        (await _factory.PedidosDeAsync(a)).Recebidos.ShouldHaveSingleItem().De.UserId.ShouldBe(b.Id);
    }

    [Fact]
    public async Task Enviar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(ClienteDaTaberna.Pedidos, new { emailOuNick = "alguem" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<(UsuarioLogado A, UsuarioLogado B)> DoisUsuariosAsync()
    {
        var usuarios = await _factory.CriarUsuariosLogadosAsync(2);
        return (usuarios[0], usuarios[1]);
    }
}
