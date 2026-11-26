using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Time.Testing;
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
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

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
        perfil.AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetMe_ComAvatar_RetornaAUrlPublicaDoAvatar()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var avatar = logado.NovoAvatar();
        var atualizacao = await cliente.PutAsJsonAsync(Url, new { logado.Dados.Nome, logado.Dados.Nick, avatarPath = avatar });

        // Act
        var resposta = await cliente.GetAsync(Url);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var perfil = (await resposta.Content.ReadFromJsonAsync<PerfilResponse>())!;
        perfil.AvatarPath.ShouldBe(avatar);
        perfil.AvatarUrl.ShouldBe(_factory.Storage.ObterUrlPublica(avatar));
        (await atualizacao.Content.ReadFromJsonAsync<PerfilResponse>())!.AvatarUrl.ShouldBe(perfil.AvatarUrl);
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

    [Fact]
    public async Task GetMe_TokenExpiradoHaUmMinuto_Retorna401()
    {
        // Arrange: o token é emitido 16 min no passado, então venceu há 1 min.
        var relogioNoPassado = new FakeTimeProvider(DateTimeOffset.UtcNow.AddMinutes(-16));
        await using var factoryNoPassado = new RPGuildApiFactory(postgres, relogioNoPassado);
        var logado = await factoryNoPassado.CriarUsuarioELogarAsync();

        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.GetAsync(Url);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
