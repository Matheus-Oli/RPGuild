using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Perfil;

[Collection(IntegrationCollection.Name)]
public sealed class ConfirmarEmailTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/me/email/confirm";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Confirm_TokenValido_Retorna204ETrocaOEmail()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var (novoEmail, token) = await _factory.PedirTrocaDeEmailAsync(logado);
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var novaSessao = await _factory.LogarAsync(logado.Dados with { Email = novoEmail });
        using var autenticado = _factory.CriarClienteAutenticado(novaSessao.AccessToken);
        (await autenticado.GetFromJsonAsync<PerfilResponse>("/api/me"))!.Email.ShouldBe(novoEmail);

        var loginAntigo = await cliente.PostAsJsonAsync("/api/auth/login", new { logado.Dados.Email, logado.Dados.Senha });
        loginAntigo.DeveSerProblema(HttpStatusCode.Unauthorized);

        await using var db = postgres.CriarDbContext();
        var usuario = await db.Users.SingleAsync(u => u.Id == logado.Id);
        usuario.EmailPendente.ShouldBeNull();
        usuario.UserName.ShouldBe(novoEmail);
    }

    [Fact]
    public async Task Confirm_TokenValido_AvisaOEmailAntigo()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var (_, token) = await _factory.PedirTrocaDeEmailAsync(logado);
        using var cliente = _factory.CriarCliente();

        // Act
        (await cliente.PostAsJsonAsync(Url, new { token })).EnsureSuccessStatusCode();

        // Assert
        var aviso = _factory.Emails.UltimaPara(logado.Dados.Email);
        aviso.ShouldNotBeNull();
        aviso.Assunto.ShouldBe("O e-mail da sua conta na RPGuild foi alterado");
    }

    [Fact]
    public async Task Confirm_ComBearer_DerrubaAsOutrasSessoesEMantemADoSid()
    {
        // Arrange
        var atual = await _factory.CriarUsuarioELogarAsync();
        var outra = await _factory.LogarAsync(atual.Dados);
        var (_, token) = await _factory.PedirTrocaDeEmailAsync(atual);
        using var cliente = _factory.CriarClienteAutenticado(atual.AccessToken);

        // Act
        (await cliente.PostAsJsonAsync(Url, new { token })).EnsureSuccessStatusCode();

        // Assert
        (await _factory.SessaoContinuaAsync(outra.RefreshCookie)).ShouldBeFalse();
        (await _factory.SessaoContinuaAsync(atual.RefreshCookie)).ShouldBeTrue();
    }

    [Fact]
    public async Task Confirm_SemBearer_DerrubaTodasAsSessoes()
    {
        // Arrange
        var primeira = await _factory.CriarUsuarioELogarAsync();
        var segunda = await _factory.LogarAsync(primeira.Dados);
        var (_, token) = await _factory.PedirTrocaDeEmailAsync(primeira);
        using var cliente = _factory.CriarCliente();

        // Act
        (await cliente.PostAsJsonAsync(Url, new { token })).EnsureSuccessStatusCode();

        // Assert
        (await _factory.SessaoContinuaAsync(primeira.RefreshCookie)).ShouldBeFalse();
        (await _factory.SessaoContinuaAsync(segunda.RefreshCookie)).ShouldBeFalse();
    }

    [Fact]
    public async Task Confirm_TokenJaUsado_Retorna400()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var (_, token) = await _factory.PedirTrocaDeEmailAsync(logado);
        using var cliente = _factory.CriarCliente();
        (await cliente.PostAsJsonAsync(Url, new { token })).EnsureSuccessStatusCode();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(TokenAdulterado.Lixo)]
    [InlineData(TokenAdulterado.TokenIdentityAlterado)]
    [InlineData(TokenAdulterado.OutroUsuario)]
    public async Task Confirm_TokenInvalidoOuAdulterado_Retorna400SemTrocarOEmail(string caso)
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var (_, token) = await _factory.PedirTrocaDeEmailAsync(logado);
        var adulterado = TokenAdulterado.Gerar(token, caso);
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token = adulterado });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        using var autenticado = _factory.CriarClienteAutenticado(logado.AccessToken);
        (await autenticado.GetFromJsonAsync<PerfilResponse>("/api/me"))!.Email.ShouldBe(logado.Dados.Email);
    }

    [Fact]
    public async Task Confirm_TokenExpirado_Retorna400()
    {
        // Arrange: o provider de token do Identity usa o relógio real, então a validade é encurtada em vez de avançar o relógio.
        await using var factory = new RPGuildApiFactory(postgres, validadeDoTokenDeRedefinicao: TimeSpan.FromSeconds(1));
        var logado = await factory.CriarUsuarioELogarAsync();
        var (_, token) = await factory.PedirTrocaDeEmailAsync(logado);
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        using var cliente = factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Confirm_EmailTomadoDepoisDoPedido_Retorna409SemTrocarOEmail()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        var (novoEmail, token) = await _factory.PedirTrocaDeEmailAsync(logado);
        await _factory.RegistrarUsuarioAsync(NovoUsuario.Gerar() with { Email = novoEmail });
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(Url, new { token });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        using var autenticado = _factory.CriarClienteAutenticado(logado.AccessToken);
        (await autenticado.GetFromJsonAsync<PerfilResponse>("/api/me"))!.Email.ShouldBe(logado.Dados.Email);
    }
}
