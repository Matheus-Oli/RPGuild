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

    private static async Task<(NovoUsuario Usuario, Guid Id)> RegistrarAsync(HttpClient cliente)
    {
        var usuario = NovoUsuario.Gerar();
        var resposta = await cliente.PostAsJsonAsync("/api/auth/register", usuario);
        resposta.EnsureSuccessStatusCode();
        return (usuario, (await resposta.Content.ReadFromJsonAsync<UsuarioResponse>())!.Id);
    }
}
