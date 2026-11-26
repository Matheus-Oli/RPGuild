using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Perfil;

[Collection(IntegrationCollection.Name)]
public sealed class TrocarSenhaTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/me/password";
    private const string NovaSenha = "novaSenha99";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task PutPassword_AtualCerta_Retorna204ELoginSoFuncionaComANova()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { atual = logado.Dados.Senha, nova = NovaSenha });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var anonimo = _factory.CriarCliente();
        var loginNova = await anonimo.PostAsJsonAsync("/api/auth/login", new { logado.Dados.Email, senha = NovaSenha });
        loginNova.StatusCode.ShouldBe(HttpStatusCode.OK);
        var loginAntiga = await anonimo.PostAsJsonAsync("/api/auth/login", new { logado.Dados.Email, logado.Dados.Senha });
        loginAntiga.DeveSerProblema(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutPassword_AtualCerta_EnviaOAvisoDeSenhaAlterada()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        (await cliente.PutAsJsonAsync(Url, new { atual = logado.Dados.Senha, nova = NovaSenha })).EnsureSuccessStatusCode();

        // Assert
        var aviso = _factory.Emails.UltimaPara(logado.Dados.Email);
        aviso.ShouldNotBeNull();
        aviso.Assunto.ShouldBe("A senha da sua conta na RPGuild foi alterada");
    }

    [Fact]
    public async Task PutPassword_AtualCerta_DerrubaAsOutrasSessoesEMantemAAtual()
    {
        // Arrange
        var atual = await _factory.CriarUsuarioELogarAsync();
        var outra = await _factory.LogarAsync(atual.Dados);
        using var cliente = _factory.CriarClienteAutenticado(atual.AccessToken);

        // Act
        (await cliente.PutAsJsonAsync(Url, new { atual = atual.Dados.Senha, nova = NovaSenha })).EnsureSuccessStatusCode();

        // Assert
        (await _factory.SessaoContinuaAsync(outra.RefreshCookie)).ShouldBeFalse();
        (await _factory.SessaoContinuaAsync(atual.RefreshCookie)).ShouldBeTrue();
    }

    [Fact]
    public async Task PutPassword_AtualErrada_Retorna400SemTrocarASenha()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { atual = "senhaErrada9", nova = NovaSenha });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.LogarAsync(logado.Dados)).AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PutPassword_NovaForaDaPolitica_Retorna400()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { atual = logado.Dados.Senha, nova = "curta" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutPassword_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { atual = "qualquer1", nova = NovaSenha });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
