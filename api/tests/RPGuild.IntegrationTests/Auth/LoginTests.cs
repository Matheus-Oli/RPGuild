using System.Net;
using System.Net.Http.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class LoginTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/auth/login";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Login_CredenciaisValidas_Retorna200ComAccessToken()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var (usuario, id) = await RegistrarAsync(cliente);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { usuario.Email, usuario.Senha });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = (await resposta.Content.ReadFromJsonAsync<TokenResponse>())!;
        corpo.ExpiresAt.ShouldBe(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));

        var jwt = new JsonWebToken(corpo.AccessToken);
        jwt.Alg.ShouldBe("HS256");
        jwt.Subject.ShouldBe(id.ToString());
        jwt.GetClaim("email").Value.ShouldBe(usuario.Email);
        jwt.GetClaim("nick").Value.ShouldBe(usuario.Nick);
        jwt.GetClaim("name").Value.ShouldBe(usuario.Nome);
    }

    [Fact]
    public async Task Login_CredenciaisValidas_DefineCookieDeRefreshSeguro()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var (usuario, _) = await RegistrarAsync(cliente);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { usuario.Email, usuario.Senha });

        // Assert
        var cookie = RefreshCookie.Ler(resposta);
        cookie.ShouldNotBeNull();
        cookie.Value.Length.ShouldBeGreaterThan(0);
        cookie.HttpOnly.ShouldBeTrue();
        cookie.Secure.ShouldBeTrue();
        cookie.SameSite.ShouldBe(Microsoft.Net.Http.Headers.SameSiteMode.None);
        cookie.Path.ToString().ShouldBe("/api/auth");
        cookie.Expires!.Value.ShouldBe(DateTimeOffset.UtcNow.AddDays(14), TimeSpan.FromMinutes(1));

        var corpo = await resposta.Content.ReadAsStringAsync();
        corpo.ShouldNotContain(cookie.Value.ToString());
    }

    [Fact]
    public async Task Login_SenhaErradaOuEmailInexistente_Retorna401ComAMesmaMensagem()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var (usuario, _) = await RegistrarAsync(cliente);

        // Act
        var senhaErrada = await cliente.PostAsJsonAsync(Url, new { usuario.Email, Senha = "outra1234" });
        var emailInexistente = await cliente.PostAsJsonAsync(Url, new { Email = $"ninguem_{Guid.NewGuid():N}@teste.rpguild", usuario.Senha });

        // Assert
        senhaErrada.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        emailInexistente.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        RefreshCookie.Ler(senhaErrada).ShouldBeNull();

        var problemaSenha = (await senhaErrada.Content.ReadFromJsonAsync<ProblemaResponse>())!;
        var problemaEmail = (await emailInexistente.Content.ReadFromJsonAsync<ProblemaResponse>())!;
        problemaSenha.Detail.ShouldNotBeNullOrWhiteSpace();
        problemaSenha.ShouldBe(problemaEmail);
    }

    private static async Task<(NovoUsuario Usuario, Guid Id)> RegistrarAsync(HttpClient cliente)
    {
        var usuario = NovoUsuario.Gerar();
        var resposta = await cliente.PostAsJsonAsync("/api/auth/register", usuario);
        resposta.EnsureSuccessStatusCode();
        return (usuario, (await resposta.Content.ReadFromJsonAsync<UsuarioResponse>())!.Id);
    }
}
