using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class MeTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/me";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetMe_BearerValido_Retorna200ComOPerfil()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", logado.AccessToken);

        // Act
        var resposta = await cliente.GetAsync(Url);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var perfil = (await resposta.Content.ReadFromJsonAsync<PerfilResponse>())!;
        perfil.Id.ShouldNotBe(Guid.Empty);
        perfil.Nome.ShouldBe(logado.Dados.Nome);
        perfil.Nick.ShouldBe(logado.Dados.Nick);
        perfil.Email.ShouldBe(logado.Dados.Email);
        perfil.AvatarPath.ShouldBeNull();
    }

    [Fact]
    public async Task GetMe_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(Url);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
