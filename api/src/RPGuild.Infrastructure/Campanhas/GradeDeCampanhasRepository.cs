using Microsoft.EntityFrameworkCore;
using RPGuild.Application.Campanhas;
using RPGuild.Domain.Campanhas;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Campanhas;

public sealed class GradeDeCampanhasRepository(AppDbContext db) : IGradeDeCampanhasRepository
{
    public async Task<IReadOnlyList<Guid>> OrdemAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.OrdemDasCampanhas
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Posicao)
            .Select(p => p.CampaignId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> CampanhasDoUsuarioAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Campanhas.Where(c => c.OwnerId == userId).Select(c => c.Id)
            .Union(db.MembrosDeCampanha.Where(m => m.UserId == userId).Select(m => m.CampaignId))
            .ToListAsync(cancellationToken);

    public async Task GravarOrdemAsync(Guid userId, IReadOnlyList<Guid> ordem, CancellationToken cancellationToken)
    {
        await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.OrdemDasCampanhas.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.OrdemDasCampanhas.AddRangeAsync(ordem.Select((campanhaId, posicao) => new PosicaoNaGrade(userId, campanhaId, posicao)), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await transacao.CommitAsync(cancellationToken);
    }
}
