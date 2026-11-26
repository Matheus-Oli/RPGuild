using System.Net;
using System.Net.Http.Json;
using RPGuild.Application.Contas;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class ResetPasswordTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/auth/reset-password";
    private const string NovaSenha = "novaSenha99";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ResetPassword_TokenValido_Retorna204()
    {
        // Arrange
        var (_, token) = await _factory.PedirRedefinicaoAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token, novaSenha = NovaSenha });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetPassword_TokenValido_LoginSoFuncionaComASenhaNova()
    {
        // Arrange
        var (usuario, token) = await _factory.PedirRedefinicaoAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token, novaSenha = NovaSenha });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var loginNova = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario.Email, senha = NovaSenha });
        loginNova.StatusCode.ShouldBe(HttpStatusCode.OK);
        var loginAntiga = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario.Email, usuario.Senha });
        loginAntiga.DeveSerProblema(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_TokenValido_DerrubaAsSessoesAbertas()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var token = await _factory.PedirRedefinicaoAsync(logado.Dados.Email);
        using var cliente = _factory.CriarCliente();
        (await cliente.PostAsJsonAsync(Url, new { token, novaSenha = NovaSenha })).EnsureSuccessStatusCode();

        // Act
        var refresh = await cliente.SendAsync(RefreshCookie.Post("/api/auth/refresh", logado.RefreshCookie));

        // Assert
        refresh.DeveSerProblema(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_TokenJaUsado_Retorna400()
    {
        // Arrange
        var (_, token) = await _factory.PedirRedefinicaoAsync();
        using var cliente = _factory.CriarCliente();
        (await cliente.PostAsJsonAsync(Url, new { token, novaSenha = NovaSenha })).EnsureSuccessStatusCode();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token, novaSenha = "outraSenha77" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("lixo")]
    [InlineData("token-identity-alterado")]
    [InlineData("outro-usuario")]
    public async Task ResetPassword_TokenInvalidoOuAdulterado_Retorna400(string caso)
    {
        // Arrange
        var (_, token) = await _factory.PedirRedefinicaoAsync();
        var original = TokenDeRedefinicao.Decodificar(token)!;
        var adulterado = caso switch
        {
            "lixo" => "nao-e-um-token",
            "token-identity-alterado" => (original with { TokenIdentity = original.TokenIdentity + "x" }).Codificar(),
            _ => (original with { UserId = Guid.NewGuid() }).Codificar(),
        };
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token = adulterado, novaSenha = NovaSenha });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_SenhaForaDaPolitica_Retorna400()
    {
        // Arrange
        var (_, token) = await _factory.PedirRedefinicaoAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token, novaSenha = "curta" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }
}
