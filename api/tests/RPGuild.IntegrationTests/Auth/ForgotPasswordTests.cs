using System.Net;
using System.Net.Http.Json;
using Npgsql;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class ForgotPasswordTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/auth/forgot-password";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ForgotPassword_EmailCadastrado_Retorna204EEnviaOLinkSemOEmail()
    {
        // Arrange
        var (usuario, _) = await _factory.RegistrarUsuarioAsync();
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { usuario.Email });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var mensagem = _factory.Emails.UltimaPara(usuario.Email);
        mensagem.ShouldNotBeNull();
        mensagem.Assunto.ShouldBe("Redefinição de senha da RPGuild");

        var link = LinkNoEmail.Extrair(mensagem, LinkNoEmail.RedefinirSenha);
        link.ShouldStartWith($"{RPGuildApiFactory.FrontBaseUrl}/redefinir-senha?token=");
        link.ShouldNotContain(usuario.Email, Case.Insensitive);
        link.ShouldNotContain(Uri.EscapeDataString(usuario.Email), Case.Insensitive);
    }

    [Fact]
    public async Task ForgotPassword_EmailCadastrado_GuardaAsChavesDoDataProtectionNoBanco()
    {
        // Act
        await _factory.PedirRedefinicaoAsync();

        // Assert
        await using var conexao = new NpgsqlConnection(postgres.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM data_protection_keys", conexao);
        var chaves = (long)(await comando.ExecuteScalarAsync())!;

        chaves.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ForgotPassword_EmailNaoCadastrado_Retorna404SemEnviarEmail()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();
        var email = NovoUsuario.Gerar().Email;

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { email });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
        var problema = (await resposta.Content.ReadFromJsonAsync<ProblemaResponse>())!;
        problema.Title.ShouldBe("E-mail não cadastrado");
        _factory.Emails.UltimaPara(email).ShouldBeNull();
    }

    [Fact]
    public async Task ForgotPassword_EmailEmFormatoInvalido_Retorna400()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { email = "sem-arroba" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }
}
