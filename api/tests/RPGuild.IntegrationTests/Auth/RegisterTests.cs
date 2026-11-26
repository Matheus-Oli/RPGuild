using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
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
    public async Task Register_DadosValidos_EnviaBoasVindasComONome()
    {
        // Act
        var (usuario, _) = await _factory.RegistrarUsuarioAsync();

        // Assert
        var mensagem = _factory.Emails.UltimaPara(usuario.Email);
        mensagem.ShouldNotBeNull();
        mensagem.Assunto.ShouldBe("Boas-vindas à RPGuild");
        mensagem.Texto.ShouldContain(usuario.Nome);
    }

    [Fact]
    public async Task Register_EmailJaEmUso_Retorna409()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var (existente, _) = await _factory.RegistrarUsuarioAsync();
        var novo = NovoUsuario.Gerar() with { Email = existente.Email.ToUpperInvariant() };

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, novo);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_NickJaEmUsoComOutraCaixa_Retorna409()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var (existente, _) = await _factory.RegistrarUsuarioAsync(NovoUsuario.Gerar() with { Nick = $"Mestre_{Guid.NewGuid():N}"[..20] });
        var novo = NovoUsuario.Gerar() with { Nick = existente.Nick.ToLowerInvariant() };

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, novo);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
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
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var campos = corpo.RootElement.GetProperty("errors").EnumerateObject().Select(p => p.Name);
        campos.ShouldBe(["nome", "nick", "email", "senha"], ignoreOrder: true);
    }

    [Fact]
    public async Task Register_DadosValidos_GravaSomenteOHashDaSenha()
    {
        // Act
        var (usuario, _) = await _factory.RegistrarUsuarioAsync();

        // Assert
        await using var conexao = new NpgsqlConnection(postgres.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT password_hash FROM users WHERE email = @email", conexao);
        comando.Parameters.AddWithValue("email", usuario.Email);
        var hash = (string?)await comando.ExecuteScalarAsync();

        hash.ShouldNotBeNullOrWhiteSpace();
        hash.ShouldNotContain(usuario.Senha);
        Convert.FromBase64String(hash)[0].ShouldBe((byte)0x01); // formato v3 do PasswordHasher (PBKDF2)
    }
}
