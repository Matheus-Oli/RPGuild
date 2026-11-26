using Microsoft.EntityFrameworkCore;
using RPGuild.Application.Notificacoes;
using RPGuild.Domain.Notificacoes;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Notificacoes;

public sealed class NotificacaoRepository(AppDbContext db) : INotificacaoRepository
{
    public async Task<IReadOnlyList<Notificacao>> ListarAsync(
        Guid userId, bool naoLidas, CursorDeNotificacoes? apos, int quantidade, CancellationToken cancellationToken)
    {
        var consulta = db.Notificacoes.Where(n => n.UserId == userId);
        if (naoLidas)
        {
            consulta = consulta.Where(n => n.LidaEm == null);
        }

        if (apos is not null)
        {
            // Mesma ordem do índice (created_at desc, id): o que vem depois do último item da página anterior.
            var (criadoEm, id) = (apos.CriadoEm, apos.Id);
            consulta = consulta.Where(n => n.CreatedAt < criadoEm || (n.CreatedAt == criadoEm && n.Id > id));
        }

        return await consulta
            .OrderByDescending(n => n.CreatedAt)
            .ThenBy(n => n.Id)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarNaoLidasAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Notificacoes.CountAsync(n => n.UserId == userId && n.LidaEm == null, cancellationToken);

    public Task<Notificacao?> BuscarDoUsuarioAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        db.Notificacoes.SingleOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

    public Task MarcarTodasComoLidasAsync(Guid userId, DateTimeOffset agora, CancellationToken cancellationToken) =>
        db.Notificacoes
            .Where(n => n.UserId == userId && n.LidaEm == null)
            .ExecuteUpdateAsync(set => set.SetProperty(n => n.LidaEm, agora), cancellationToken);

    public void Remover(Notificacao notificacao) => db.Notificacoes.Remove(notificacao);

    public async Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken) =>
        await db.Notificacoes.AddAsync(notificacao, cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
