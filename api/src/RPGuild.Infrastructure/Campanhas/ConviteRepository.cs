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

    public async Task<IReadOnlyList<ConviteDeCampanha>> PendentesEntreAsync(
        Guid mestreId, Guid convidadoId, CancellationToken cancellationToken) =>
        await (
            from c in db.ConvitesDeCampanha
            where c.UserId == convidadoId && c.Status == StatusDoConvite.Pendente
            join campanha in db.Campanhas on c.CampaignId equals campanha.Id
            where campanha.OwnerId == mestreId
            select c)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConviteComConvidado>> ListarPendentesDaCampanhaAsync(
        Guid campanhaId, CancellationToken cancellationToken) =>
        await (
            from c in db.ConvitesDeCampanha
            where c.CampaignId == campanhaId && c.Status == StatusDoConvite.Pendente
            join u in db.Users on c.UserId equals u.Id
            orderby c.CreatedAt descending, c.Id
            select new ConviteComConvidado(c.Id, new PessoaGuardada(u.Id, u.Nome, u.Nick, u.AvatarPath), c.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConviteGuardado>> ListarPendentesDoUsuarioAsync(Guid userId, CancellationToken cancellationToken) =>
        await (
            from c in db.ConvitesDeCampanha
            where c.UserId == userId && c.Status == StatusDoConvite.Pendente
            join campanha in db.Campanhas on c.CampaignId equals campanha.Id
            join s in db.SistemasDeJogo on campanha.SystemId equals s.Id
            join u in db.Users on campanha.OwnerId equals u.Id
            orderby c.CreatedAt descending, c.Id
            select new ConviteGuardado(
                c.Id, campanha.Id, campanha.Nome, s, campanha.CapaPath, new PessoaGuardada(u.Id, u.Nome, u.Nick, u.AvatarPath), c.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
