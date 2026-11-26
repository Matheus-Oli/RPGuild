using Microsoft.EntityFrameworkCore;
using RPGuild.Application.Contas;
using RPGuild.Domain.Contas;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Contas;

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task AdicionarAsync(RefreshToken token, CancellationToken cancellationToken) =>
        await db.RefreshTokens.AddAsync(token, cancellationToken);

    public Task<RefreshToken?> BuscarPorHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListarDaFamiliaAsync(Guid familyId, CancellationToken cancellationToken) =>
        await db.RefreshTokens.Where(t => t.FamilyId == familyId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListarNaoRevogadosDoUsuarioAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
