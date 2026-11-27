using Microsoft.EntityFrameworkCore;
using RPGuild.Application.Campanhas;
using RPGuild.Domain.Campanhas;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Campanhas;

public sealed class ConviteRepository(AppDbContext db) : IConviteRepository
{
    public async Task AdicionarAsync(ConviteDeCampanha convite, CancellationToken cancellationToken) =>
        await db.ConvitesDeCampanha.AddAsync(convite, cancellationToken);

    public Task<ConviteDeCampanha?> BuscarPorIdAsync(Guid conviteId, CancellationToken cancellationToken) =>
        db.ConvitesDeCampanha.SingleOrDefaultAsync(c => c.Id == conviteId, cancellationToken);

    public async Task<IReadOnlyList<ConviteDeCampanha>> PendentesDaCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken) =>
        await db.ConvitesDeCampanha
            .Where(c => c.CampaignId == campanhaId && c.Status == StatusDoConvite.Pendente)
            .ToListAsync(cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
