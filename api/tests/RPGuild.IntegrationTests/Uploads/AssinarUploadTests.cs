using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Uploads;

[Collection(IntegrationCollection.Name)]
public sealed class AssinarUploadTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/uploads/sign";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Sign_AvatarPng_Retorna200ComPathNaPastaDoUsuarioEUrls()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { tipo = "avatar", contentType = "image/png" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var assinatura = (await resposta.Content.ReadFromJsonAsync<UrlDeUploadResponse>())!;
        assinatura.Path.ShouldMatch($"^avatars/{logado.Id}/[0-9a-f-]{{36}}\\.png$");
        assinatura.PublicUrl.ShouldBe(_factory.Storage.ObterUrlPublica(assinatura.Path));
        assinatura.UploadUrl.ShouldNotBeNullOrWhiteSpace();
        _factory.Storage.Criados.ShouldBe([assinatura.Path]);
    }

    [Theory]
    [InlineData("capa", "image/jpeg", "jpg")]
    [InlineData("banner", "image/webp", "webp")]
    public async Task Sign_CapaOuBanner_Retorna200ComPathNaPastaDeCampanhasDoUsuario(string tipo, string contentType, string extensao)
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { tipo, contentType });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var assinatura = (await resposta.Content.ReadFromJsonAsync<UrlDeUploadResponse>())!;
        assinatura.Path.ShouldMatch($"^campaigns/{logado.Id}/[0-9a-f-]{{36}}\\.{extensao}$");
        assinatura.PublicUrl.ShouldBe(_factory.Storage.ObterUrlPublica(assinatura.Path));
        _factory.Storage.Criados.ShouldBe([assinatura.Path]);
    }

    [Theory]
    [InlineData("capa", "image/svg+xml")]
    [InlineData("banner", "application/pdf")]
    public async Task Sign_CapaOuBannerComContentTypeForaDaLista_Retorna400(string tipo, string contentType)
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { tipo, contentType });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        _factory.Storage.Criados.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sign_TipoNaoSuportado_Retorna400()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { tipo = "retrato", contentType = "image/png" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        _factory.Storage.Criados.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sign_ContentTypeForaDaLista_Retorna400()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { tipo = "avatar", contentType = "image/svg+xml" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        _factory.Storage.Criados.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sign_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { tipo = "avatar", contentType = "image/png" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record UrlDeUploadResponse(string UploadUrl, string Path, string PublicUrl);
}
