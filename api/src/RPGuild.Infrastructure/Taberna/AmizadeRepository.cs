using Microsoft.EntityFrameworkCore;
using RPGuild.Application.Taberna;
using RPGuild.Domain.Taberna;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Taberna;

public sealed class AmizadeRepository(AppDbContext db) : IAmizadeRepository
{
    public Task<Amizade?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Amizades.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Amizade?> BuscarDoParAsync(Guid usuarioA, Guid usuarioB, CancellationToken cancellationToken) =>
        db.Amizades.SingleOrDefaultAsync(
            a => (a.RequesterId == usuarioA && a.AddresseeId == usuarioB) || (a.RequesterId == usuarioB && a.AddresseeId == usuarioA),
            cancellationToken);

    public async Task<IReadOnlyList<PedidoPendente>> ListarPendentesAsync(Guid userId, CancellationToken cancellationToken) =>
        await (
            from a in db.Amizades
            where a.Status == StatusDaAmizade.Pendente && (a.RequesterId == userId || a.AddresseeId == userId)
            join u in db.Users on (a.RequesterId == userId ? a.AddresseeId : a.RequesterId) equals u.Id
            orderby a.CreatedAt descending
            select new PedidoPendente(a.Id, a.RequesterId == userId, new PessoaDaLista(u.Id, u.Nome, u.Nick, u.AvatarPath), a.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Amizade amizade, CancellationToken cancellationToken) =>
        await db.Amizades.AddAsync(amizade, cancellationToken);

    public void Remover(Amizade amizade) => db.Amizades.Remove(amizade);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
