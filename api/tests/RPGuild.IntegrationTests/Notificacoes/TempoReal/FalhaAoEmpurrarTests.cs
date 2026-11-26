using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RPGuild.Application.Notificacoes;
using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Notificacoes.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class FalhaAoEmpurrarTests(PostgresFixture postgres)
{
    [Fact]
    public async Task PedidoDeAmizade_TempoRealFalhando_Retorna201GravaEOGetMostra()
    {
        // Arrange
        await using var factory = new RPGuildApiFactory(
            postgres,
            configurarServicos: services => services.Replace(ServiceDescriptor.Singleton<INotificacoesTempoReal>(new TempoRealQueFalha())));
        var (a, b) = await factory.CriarDoisUsuariosLogadosAsync();

        // Act
        var resposta = await factory.PedirAsync(a, b.Dados.Nick);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await factory.PaginaAsync(b)).Itens.ShouldContain(n => n.Tipo == "amizade_recebida");
        (await factory.NaoLidasAsync(b)).ShouldBe(2);
    }

    /// <summary>
    /// Simula o SignalR fora do ar: todo evento falha.
    /// </summary>
    private sealed class TempoRealQueFalha : INotificacoesTempoReal
    {
        public Task NotificacaoCriadaAsync(
            Guid userId, Application.Notificacoes.NotificacaoResponse notificacao, CancellationToken cancellationToken) => Falhar();

        public Task NaoLidasAsync(Guid userId, ContagemDeNaoLidasResponse contagem, CancellationToken cancellationToken) => Falhar();

        public Task NotificacaoLidaAsync(Guid userId, Guid notificacaoId, CancellationToken cancellationToken) => Falhar();

        public Task TodasLidasAsync(Guid userId, CancellationToken cancellationToken) => Falhar();

        public Task NotificacaoApagadaAsync(Guid userId, Guid notificacaoId, CancellationToken cancellationToken) => Falhar();

        private static Task Falhar() => throw new InvalidOperationException("SignalR indisponível (simulado nos testes).");
    }
}
