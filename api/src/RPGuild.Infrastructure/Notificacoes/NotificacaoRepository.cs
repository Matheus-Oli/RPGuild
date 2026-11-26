using RPGuild.Application.Notificacoes;
using RPGuild.Domain.Notificacoes;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Notificacoes;

public sealed class NotificacaoRepository(AppDbContext db) : INotificacaoRepository
{
    public async Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken) =>
        await db.Notificacoes.AddAsync(notificacao, cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
