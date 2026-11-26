using Microsoft.EntityFrameworkCore;
using RPGuild.Application.Campanhas;
using RPGuild.Domain.Campanhas;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Campanhas;

public sealed class SistemasRepository(AppDbContext db) : ISistemasRepository
{
    public async Task<IReadOnlyList<SistemaDeJogo>> ListarAsync(CancellationToken cancellationToken) =>
        await db.SistemasDeJogo.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancellationToken);

    public Task<SistemaDeJogo?> BuscarPorSlugAsync(string slug, CancellationToken cancellationToken) =>
        db.SistemasDeJogo.AsNoTracking().SingleOrDefaultAsync(s => s.Slug == slug, cancellationToken);
}
