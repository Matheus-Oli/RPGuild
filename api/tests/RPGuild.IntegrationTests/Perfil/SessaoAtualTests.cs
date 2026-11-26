using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Perfil;

[Collection(IntegrationCollection.Name)]
public sealed class SessaoAtualTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Login_CredenciaisValidas_AccessTokenTemSidIgualAFamiliaDoRefresh()
    {
        // Act
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Assert
        var familia = await FamiliaUnicaDoUsuarioAsync(ClaimsDoToken.UserId(logado.AccessToken));
        ClaimsDoToken.Sid(logado.AccessToken).ShouldBe(familia);
    }

    [Fact]
    public async Task Refresh_CookieValido_AccessTokenNovoMantemOSidDaFamilia()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.SendAsync(RefreshCookie.Post("/api/auth/refresh", logado.RefreshCookie));

        // Assert
        resposta.EnsureSuccessStatusCode();
        var novo = (await resposta.Content.ReadFromJsonAsync<TokenResponse>())!;
        var familia = await FamiliaUnicaDoUsuarioAsync(ClaimsDoToken.UserId(logado.AccessToken));
        ClaimsDoToken.Sid(novo.AccessToken).ShouldBe(familia);
        ClaimsDoToken.Sid(novo.AccessToken).ShouldBe(ClaimsDoToken.Sid(logado.AccessToken));
    }

    private async Task<Guid> FamiliaUnicaDoUsuarioAsync(Guid userId)
    {
        await using var db = postgres.CriarDbContext();
        var familias = await db.RefreshTokens.Where(t => t.UserId == userId).Select(t => t.FamilyId).Distinct().ToListAsync();
        return familias.ShouldHaveSingleItem();
    }
}
