using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Perfil;

[Collection(IntegrationCollection.Name)]
public sealed class AtualizarPerfilTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/me";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task PutMe_NomeENickNovos_Retorna200ComOPerfilAtualizado()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var nick = NovoUsuario.Gerar().Nick;

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { nome = "Nome Novo", nick, avatarPath = (string?)null });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var perfil = (await resposta.Content.ReadFromJsonAsync<PerfilResponse>())!;
        perfil.Nome.ShouldBe("Nome Novo");
        perfil.Nick.ShouldBe(nick);
        perfil.Email.ShouldBe(logado.Dados.Email);

        var gravado = (await cliente.GetFromJsonAsync<PerfilResponse>(Url))!;
        gravado.Nome.ShouldBe("Nome Novo");
        gravado.Nick.ShouldBe(nick);
    }

    [Fact]
    public async Task PutMe_ProprioNickEmOutraCaixa_Retorna200()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var nick = logado.Dados.Nick.ToUpperInvariant();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { nome = logado.Dados.Nome, nick });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<PerfilResponse>())!.Nick.ShouldBe(nick);
    }

    [Fact]
    public async Task PutMe_NickDeOutroUsuarioEmOutraCaixa_Retorna409()
    {
        // Arrange
        var (outro, _) = await _factory.RegistrarUsuarioAsync();
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { nome = logado.Dados.Nome, nick = outro.Nick.ToUpperInvariant() });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.Content.ReadFromJsonAsync<ProblemaResponse>())!.Title.ShouldBe("Nick em uso");
    }

    [Fact]
    public async Task PutMe_AvatarPathDeOutroUsuario_Retorna400()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, Request(logado, AvatarDeOutroUsuario()));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await cliente.GetFromJsonAsync<PerfilResponse>(Url))!.AvatarPath.ShouldBeNull();
    }

    [Fact]
    public async Task PutMe_AvatarNaPastaDoUsuario_GravaOAvatar()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var avatar = logado.NovoAvatar();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, Request(logado, avatar));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<PerfilResponse>())!.AvatarPath.ShouldBe(avatar);
        _factory.Storage.Apagados.ShouldBeEmpty();
    }

    [Fact]
    public async Task PutMe_TrocaOAvatar_ApagaOAntigoDoStorage()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var antigo = logado.NovoAvatar();
        (await cliente.PutAsJsonAsync(Url, Request(logado, antigo))).EnsureSuccessStatusCode();
        var novo = logado.NovoAvatar();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, Request(logado, novo));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<PerfilResponse>())!.AvatarPath.ShouldBe(novo);
        _factory.Storage.Apagados.ShouldBe([antigo]);
    }

    [Fact]
    public async Task PutMe_MesmoAvatar_NaoApagaNada()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var avatar = logado.NovoAvatar();
        (await cliente.PutAsJsonAsync(Url, Request(logado, avatar))).EnsureSuccessStatusCode();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, Request(logado, avatar) with { Nome = "Outro Nome" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        _factory.Storage.Apagados.ShouldBeEmpty();
    }

    [Fact]
    public async Task PutMe_RemoveOAvatar_ApagaOAntigoDoStorage()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var antigo = logado.NovoAvatar();
        (await cliente.PutAsJsonAsync(Url, Request(logado, antigo))).EnsureSuccessStatusCode();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, Request(logado, avatarPath: null));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<PerfilResponse>())!.AvatarPath.ShouldBeNull();
        _factory.Storage.Apagados.ShouldBe([antigo]);
    }

    [Fact]
    public async Task PutMe_FalhaAoApagarOAvatarAntigo_Retorna200ComOAvatarNovo()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        (await cliente.PutAsJsonAsync(Url, Request(logado, logado.NovoAvatar()))).EnsureSuccessStatusCode();
        _factory.Storage.FalharAoApagar = true;
        var novo = logado.NovoAvatar();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, Request(logado, novo));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await cliente.GetFromJsonAsync<PerfilResponse>(Url))!.AvatarPath.ShouldBe(novo);
    }

    [Fact]
    public async Task PutMe_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { nome = "Nome", nick = "nick_qualquer" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static AtualizarPerfil Request(UsuarioLogado logado, string? avatarPath) =>
        new(logado.Dados.Nome, logado.Dados.Nick, avatarPath);

    private static string AvatarDeOutroUsuario() => $"avatars/{Guid.NewGuid()}/{Guid.NewGuid()}.png";

    private sealed record AtualizarPerfil(string Nome, string Nick, string? AvatarPath);
}
