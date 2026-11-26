using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class RegisterTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/auth/register";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_DadosValidos_Retorna201ComOUsuario()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var usuario = NovoUsuario.Gerar();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, usuario);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var corpo = (await resposta.Content.ReadFromJsonAsync<UsuarioResponse>())!;
        corpo.Id.ShouldNotBe(Guid.Empty);
        corpo.Nome.ShouldBe(usuario.Nome);
        corpo.Nick.ShouldBe(usuario.Nick);
        corpo.Email.ShouldBe(usuario.Email);
    }

    [Fact]
    public async Task Register_EmailJaEmUso_Retorna409()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var existente = NovoUsuario.Gerar();
        (await cliente.PostAsJsonAsync(Url, existente)).EnsureSuccessStatusCode();
        var novo = NovoUsuario.Gerar() with { Email = existente.Email.ToUpperInvariant() };

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, novo);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Register_NickJaEmUsoComOutraCaixa_Retorna409()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var existente = NovoUsuario.Gerar() with { Nick = $"Mestre_{Guid.NewGuid():N}"[..20] };
        (await cliente.PostAsJsonAsync(Url, existente)).EnsureSuccessStatusCode();
        var novo = NovoUsuario.Gerar() with { Nick = existente.Nick.ToLowerInvariant() };

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, novo);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Register_CamposInvalidos_Retorna400ComErrosPorCampo()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var invalido = new NovoUsuario(Nome: "", Nick: "a b", Email: "sem-arroba", Senha: "curta");

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, invalido);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var campos = corpo.RootElement.GetProperty("errors").EnumerateObject().Select(p => p.Name);
        campos.ShouldBe(["nome", "nick", "email", "senha"], ignoreOrder: true);
    }
}
