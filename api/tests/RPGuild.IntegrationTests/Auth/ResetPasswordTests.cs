using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    public void Configuracao_TokenDeRedefinicao_ValeUmaHora()
    {
        // Act
        var options = _factory.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;

        // Assert
        options.TokenLifespan.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task ResetPassword_TokenExpirado_Retorna400()
    {
        // Arrange: o provider de token do Identity usa o relógio real, então a validade é encurtada em vez de avançar o relógio.
        await using var factory = new RPGuildApiFactory(postgres, validadeDoTokenDeRedefinicao: TimeSpan.FromSeconds(1));
        var (_, token) = await factory.PedirRedefinicaoAsync();
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        using var cliente = factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token, novaSenha = NovaSenha });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
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
    [InlineData(TokenAdulterado.Lixo)]
    [InlineData(TokenAdulterado.TokenIdentityAlterado)]
    [InlineData(TokenAdulterado.OutroUsuario)]
    public async Task ResetPassword_TokenInvalidoOuAdulterado_Retorna400(string caso)
    {
        // Arrange
        var (_, token) = await _factory.PedirRedefinicaoAsync();
        var adulterado = TokenAdulterado.Gerar(token, caso);
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
