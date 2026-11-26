using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Perfil;

[Collection(IntegrationCollection.Name)]
public sealed class TrocarEmailTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/me/email";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task PutEmail_DadosValidos_Retorna202EGravaOPendenteSemTrocarOAtual()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var novoEmail = NovoUsuario.Gerar().Email;

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { novoEmail, senha = logado.Dados.Senha });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await EmailPendenteAsync(logado)).ShouldBe(novoEmail);
        (await cliente.GetFromJsonAsync<PerfilResponse>("/api/me"))!.Email.ShouldBe(logado.Dados.Email);
    }

    [Fact]
    public async Task PutEmail_DadosValidos_MandaOLinkDeConfirmacaoParaONovoEnderecoSemOEmailNaUrl()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var novoEmail = NovoUsuario.Gerar().Email;

        // Act
        (await cliente.PutAsJsonAsync(Url, new { novoEmail, senha = logado.Dados.Senha })).EnsureSuccessStatusCode();

        // Assert
        var mensagem = _factory.Emails.UltimaPara(novoEmail);
        mensagem.ShouldNotBeNull();
        mensagem.Assunto.ShouldBe("Confirme seu novo e-mail na RPGuild");
        _factory.Emails.UltimaPara(logado.Dados.Email)!.Assunto.ShouldNotBe(mensagem.Assunto);

        var link = LinkNoEmail.Extrair(mensagem, LinkNoEmail.ConfirmarEmail);
        link.ShouldStartWith($"{RPGuildApiFactory.FrontBaseUrl}/confirmar-email?token=");
        foreach (var email in new[] { novoEmail, logado.Dados.Email })
        {
            link.ShouldNotContain(email, Case.Insensitive);
            link.ShouldNotContain(Uri.EscapeDataString(email), Case.Insensitive);
        }
    }

    [Fact]
    public async Task PutEmail_SenhaErrada_Retorna400SemGravarOPendente()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);
        var novoEmail = NovoUsuario.Gerar().Email;

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { novoEmail, senha = "senhaErrada9" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await EmailPendenteAsync(logado)).ShouldBeNull();
        _factory.Emails.UltimaPara(novoEmail).ShouldBeNull();
    }

    [Fact]
    public async Task PutEmail_EmailDeOutraConta_Retorna409()
    {
        // Arrange
        var (outro, _) = await _factory.RegistrarUsuarioAsync();
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { novoEmail = outro.Email.ToUpperInvariant(), senha = logado.Dados.Senha });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Conflict);
        (await resposta.Content.ReadFromJsonAsync<ProblemaResponse>())!.Title.ShouldBe("E-mail em uso");
    }

    [Fact]
    public async Task PutEmail_MesmoEmailAtual_Retorna400()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { novoEmail = logado.Dados.Email.ToUpperInvariant(), senha = logado.Dados.Senha });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await EmailPendenteAsync(logado)).ShouldBeNull();
    }

    [Fact]
    public async Task PutEmail_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PutAsJsonAsync(Url, new { novoEmail = NovoUsuario.Gerar().Email, senha = "qualquer1" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string?> EmailPendenteAsync(UsuarioLogado logado)
    {
        await using var db = postgres.CriarDbContext();
        var userId = ClaimsDoToken.UserId(logado.AccessToken);
        return await db.Users.Where(u => u.Id == userId).Select(u => u.EmailPendente).SingleAsync();
    }
}
