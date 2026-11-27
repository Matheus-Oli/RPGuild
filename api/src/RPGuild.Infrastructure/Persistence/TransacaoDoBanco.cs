using RPGuild.Application.Comum;

namespace RPGuild.Infrastructure.Persistence;

public sealed class TransacaoDoBanco(AppDbContext db) : ITransacao
{
    public async Task ExecutarAsync(Func<Task> trabalho, CancellationToken cancellationToken)
    {
        // O Npgsql não aninha transações: quem já está numa só continua nela.
        if (db.Database.CurrentTransaction is not null)
        {
            await trabalho();
            return;
        }

        // Sem commit, o dispose desfaz tudo.
        await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);
        await trabalho();
        await transacao.CommitAsync(cancellationToken);
    }
}
