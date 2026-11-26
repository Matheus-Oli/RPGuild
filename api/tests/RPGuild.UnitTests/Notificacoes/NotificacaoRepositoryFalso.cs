using RPGuild.Application.Notificacoes;
using RPGuild.Domain.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

/// <summary>
/// Repositório em memória: guarda as instâncias para o teste inspecionar o estado depois.
/// </summary>
public sealed class NotificacaoRepositoryFalso : INotificacaoRepository
{
    public List<Notificacao> Notificacoes { get; } = [];

    public int VezesSalvo { get; private set; }

    // A listagem é uma consulta por chave no banco: coberta pelos testes de integração.
    public Task<IReadOnlyList<Notificacao>> ListarAsync(
        Guid userId, bool naoLidas, CursorDeNotificacoes? apos, int quantidade, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> ContarNaoLidasAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Notificacoes.Count(n => n.UserId == userId && !n.Lida));

    public Task<Notificacao?> BuscarDoUsuarioAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Notificacoes.SingleOrDefault(n => n.Id == id && n.UserId == userId));

    public Task MarcarTodasComoLidasAsync(Guid userId, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        foreach (var notificacao in Notificacoes.Where(n => n.UserId == userId))
        {
            notificacao.MarcarComoLida(agora);
        }

        return Task.CompletedTask;
    }

    public Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken)
    {
        Notificacoes.Add(notificacao);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        VezesSalvo++;
        return Task.CompletedTask;
    }
}
