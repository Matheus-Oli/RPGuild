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

    [Theory]
    [InlineData("accept", "amizade_aceita")]
    [InlineData("decline", "amizade_recusada")]
    public async Task Resposta_AoPedido_QuemPediuRecebeOTipoComQuemRespondeuSemEmail(string acao, string tipo)
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        var pedido = await _factory.PedirComSucessoAsync(a, b);
        var emailsDeA = _factory.EmailsPara(a);

        // Act
        (await _factory.PostAsync(b, $"{ClienteDaTaberna.Pedido(pedido.Id)}/{acao}")).EnsureSuccessStatusCode();

        // Assert
        var notificacao = (await SemBoasVindasAsync(a)).ShouldHaveSingleItem();
        notificacao.Tipo.ShouldBe(tipo);
        var dados = JsonDocument.Parse(notificacao.Dados).RootElement;
        dados.GetProperty("userId").GetGuid().ShouldBe(b.Id);
        dados.GetProperty("nome").GetString().ShouldBe(b.Dados.Nome);
        dados.GetProperty("nick").GetString().ShouldBe(b.Dados.Nick);
        _factory.EmailsPara(a).ShouldBe(emailsDeA);

        // Quem respondeu não ganha nada além do pedido que já tinha.
        (await SemBoasVindasAsync(b)).ShouldHaveSingleItem().Tipo.ShouldBe("amizade_recebida");
    }

    [Fact]
    public async Task Remover_Amigo_NinguemRecebeNada()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        await _factory.TornarAmigosAsync(a, b);
        var (emailsDeA, emailsDeB) = (_factory.EmailsPara(a), _factory.EmailsPara(b));

        // Act
        (await _factory.DeleteAsync(a, ClienteDaTaberna.Amigo(b.Id))).EnsureSuccessStatusCode();

        // Assert
        (await SemBoasVindasAsync(a)).ShouldBeEmpty();
        (await SemBoasVindasAsync(b)).ShouldBeEmpty();
        _factory.EmailsPara(a).ShouldBe(emailsDeA);
        _factory.EmailsPara(b).ShouldBe(emailsDeB);
    }

    private async Task<List<NotificacaoNoBanco>> SemBoasVindasAsync(UsuarioLogado usuario) =>
        [.. (await _factory.NotificacoesNoBancoAsync(usuario.Id)).Where(n => n.Tipo != "boas_vindas")];
}
