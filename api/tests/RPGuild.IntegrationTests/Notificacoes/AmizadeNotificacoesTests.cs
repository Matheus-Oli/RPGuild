using System.Text.Json;
using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Notificacoes;

[Collection(IntegrationCollection.Name)]
public sealed class AmizadeNotificacoesTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Pedido_Enviado_DestinatarioRecebeAmizadeRecebidaComQuemPediuEOEmail()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();

        // Act
        var pedido = await _factory.PedirComSucessoAsync(a, b);

        // Assert
        var notificacao = (await SemBoasVindasAsync(b)).ShouldHaveSingleItem();
        notificacao.Tipo.ShouldBe("amizade_recebida");
        var dados = JsonDocument.Parse(notificacao.Dados).RootElement;
        dados.GetProperty("pedidoId").GetGuid().ShouldBe(pedido.Id);
        dados.GetProperty("deUserId").GetGuid().ShouldBe(a.Id);
        dados.GetProperty("deNome").GetString().ShouldBe(a.Dados.Nome);
        dados.GetProperty("deNick").GetString().ShouldBe(a.Dados.Nick);

        _factory.Fila.Enfileiradas.ShouldContain(m => m.Para == b.Dados.Email && m.Assunto.Contains("pedido de amizade"));
        (await SemBoasVindasAsync(a)).ShouldBeEmpty();
    }

    private async Task<List<NotificacaoNoBanco>> SemBoasVindasAsync(UsuarioLogado usuario) =>
        [.. (await _factory.NotificacoesNoBancoAsync(usuario.Id)).Where(n => n.Tipo != "boas_vindas")];
}
