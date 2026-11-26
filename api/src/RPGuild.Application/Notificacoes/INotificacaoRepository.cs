using RPGuild.Domain.Notificacoes;

namespace RPGuild.Application.Notificacoes;

public interface INotificacaoRepository
{
    Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
