using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Perfil;

/// <summary>
/// Senha atual errada em PUT /api/me/email e PUT /api/me/password conta para o mesmo bloqueio do login.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class BloqueioPorSenhaErradaTests(PostgresFixture postgres) : IDisposable
{
    private const string TrocaDeEmail = "email";
    private const string TrocaDeSenha = "password";
    private const string SenhaErrada = "senhaErrada9";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData(TrocaDeEmail)]
    [InlineData(TrocaDeSenha)]
    public async Task SenhaAtualErrada_CincoVezes_SextaComASenhaCertaRetorna423(string endpoint)
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        for (var i = 0; i < 5; i++)
        {
            await EnviarAsync(logado, endpoint, SenhaErrada);
        }

        // Act
        var resposta = await EnviarAsync(logado, endpoint, logado.Dados.Senha);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Locked);
    }

    [Theory]
    [InlineData(TrocaDeEmail)]
    [InlineData(TrocaDeSenha)]
    public async Task SenhaAtualErrada_ErrosNoEndpointENoLogin_SomamNoMesmoContador(string endpoint)
    {
        // Arrange: 3 erros no endpoint e 2 no login.
        var logado = await _factory.CriarUsuarioELogarAsync();
        await EnviarAsync(logado, endpoint, SenhaErrada);
        await EnviarAsync(logado, endpoint, SenhaErrada);
        await LoginAsync(logado, SenhaErrada);
        await LoginAsync(logado, SenhaErrada);
        await EnviarAsync(logado, endpoint, SenhaErrada);

        // Act
        var resposta = await EnviarAsync(logado, endpoint, logado.Dados.Senha);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Locked);
        (await LoginAsync(logado, logado.Dados.Senha)).DeveSerProblema(HttpStatusCode.Locked);
    }

    [Theory]
    [InlineData(TrocaDeEmail)]
    [InlineData(TrocaDeSenha)]
    public async Task SenhaAtualCerta_ZeraOContador(string endpoint)
    {
        // Arrange: 4 erros, um acerto e mais 4 erros. Sem zerar, seriam 8 erros e a conta já estaria bloqueada.
        var logado = await _factory.CriarUsuarioELogarAsync();
        for (var i = 0; i < 4; i++)
        {
            await EnviarAsync(logado, endpoint, SenhaErrada);
        }

        (await EnviarAsync(logado, endpoint, logado.Dados.Senha)).EnsureSuccessStatusCode();
        for (var i = 0; i < 4; i++)
        {
            await EnviarAsync(logado, endpoint, SenhaErrada);
        }

        // Act
        var resposta = await EnviarAsync(logado, endpoint, logado.Dados.Senha);

        // Assert
        resposta.IsSuccessStatusCode.ShouldBeTrue($"Esperava sucesso, veio {(int)resposta.StatusCode}.");
    }

    /// <summary>
    /// Na troca de senha, a nova é a própria senha atual: a senha certa continua a mesma durante o teste.
    /// </summary>
    private async Task<HttpResponseMessage> EnviarAsync(UsuarioLogado logado, string endpoint, string senha)
    {
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        return endpoint == TrocaDeEmail
            ? await cliente.PutAsJsonAsync("/api/me/email", new { novoEmail = NovoUsuario.Gerar().Email, senha })
            : await cliente.PutAsJsonAsync("/api/me/password", new { atual = senha, nova = logado.Dados.Senha });
    }

    private async Task<HttpResponseMessage> LoginAsync(UsuarioLogado logado, string senha)
    {
        using var cliente = _factory.CriarCliente();
        return await cliente.PostAsJsonAsync("/api/auth/login", new { logado.Dados.Email, senha });
    }
}
