using Microsoft.EntityFrameworkCore;
using RPGuild.Application.Campanhas;
using RPGuild.Domain.Campanhas;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Campanhas;

public sealed class CampanhaRepository(AppDbContext db) : ICampanhaRepository
{
    public async Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) =>
        await db.Campanhas.AddAsync(campanha, cancellationToken);

    public async Task<CampanhaGuardada?> BuscarComPessoasAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        var campanha = await (
            from c in db.Campanhas.AsNoTracking()
            where c.Id == campanhaId
            join s in db.SistemasDeJogo on c.SystemId equals s.Id
            join u in db.Users on c.OwnerId equals u.Id
            select new { Campanha = c, Sistema = s, Mestre = new PessoaGuardada(u.Id, u.Nome, u.Nick, u.AvatarPath) })
            .SingleOrDefaultAsync(cancellationToken);
        if (campanha is null)
        {
            return null;
        }

        var jogadores = await JogadoresAsync([campanhaId], cancellationToken);
        return new CampanhaGuardada(campanha.Campanha, campanha.Sistema, campanha.Mestre, jogadores.GetValueOrDefault(campanhaId, []));
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    // Os jogadores de cada campanha, do que entrou primeiro para o último.
    private async Task<Dictionary<Guid, List<JogadorGuardado>>> JogadoresAsync(
        IReadOnlyCollection<Guid> campanhaIds, CancellationToken cancellationToken)
    {
        var jogadores = await (
            from m in db.MembrosDeCampanha
            where campanhaIds.Contains(m.CampaignId)
            join u in db.Users on m.UserId equals u.Id
            orderby m.JoinedAt, u.Id
            select new { m.CampaignId, Jogador = new JogadorGuardado(new PessoaGuardada(u.Id, u.Nome, u.Nick, u.AvatarPath), m.JoinedAt) })
            .ToListAsync(cancellationToken);

        return jogadores.GroupBy(j => j.CampaignId).ToDictionary(g => g.Key, g => g.Select(j => j.Jogador).ToList());
    }
}
