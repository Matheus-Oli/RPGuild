using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes;

/// <summary>
/// Os e-mails de Contas continuam saindo, cada um pelo canal da matriz, e nenhum grava no sininho.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class ContasNotificacoesTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ForgotPassword_EmailComToken_SaiDiretoSemFilaESemSininho()
    {
        // Arrange
        var (usuario, criado) = await _factory.RegistrarUsuarioAsync();

        // Act
        await _factory.PedirRedefinicaoAsync(usuario.Email);

        // Assert
        _factory.Emails.UltimaPara(usuario.Email)!.Assunto.ShouldBe("Redefinição de senha da RPGuild");
        _factory.Fila.Enfileiradas.ShouldNotContain(m => m.Assunto == "Redefinição de senha da RPGuild");
        await SoBoasVindasAsync(criado.Id);
    }

    [Fact]
    public async Task TrocaDeEmail_ConfirmacaoDiretaEAvisoNoAntigoPelaFila_SemSininho()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        var (novoEmail, token) = await _factory.PedirTrocaDeEmailAsync(logado);
        using var cliente = _factory.CriarCliente();
        (await cliente.PostAsJsonAsync("/api/me/email/confirm", new { token })).EnsureSuccessStatusCode();

        // Assert: a confirmação leva token, então sai direto para o endereço novo.
        _factory.Emails.Enviadas.ShouldContain(m => m.Para == novoEmail && m.Assunto == "Confirme seu novo e-mail na RPGuild");
        _factory.Fila.PassouPelaFila(novoEmail).ShouldBeFalse();

        // O aviso nº 4 vai para o endereço antigo, pela fila.
        _factory.Fila.Enfileiradas.ShouldContain(
            m => m.Para == logado.Dados.Email && m.Assunto == "O e-mail da sua conta na RPGuild foi alterado");
        await SoBoasVindasAsync(logado.Id);
    }

    [Fact]
    public async Task TrocaDeSenha_AvisoPelaFila_SemSininho()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        (await cliente.PutAsJsonAsync("/api/me/password", new { atual = logado.Dados.Senha, nova = "novaSenha99" })).EnsureSuccessStatusCode();

        // Assert
        _factory.Fila.Enfileiradas.ShouldContain(
            m => m.Para == logado.Dados.Email && m.Assunto == "A senha da sua conta na RPGuild foi alterada");
        await SoBoasVindasAsync(logado.Id);
    }

    private async Task SoBoasVindasAsync(Guid userId) =>
        (await _factory.NotificacoesNoBancoAsync(userId)).ShouldHaveSingleItem().Tipo.ShouldBe("boas_vindas");
}
