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
