using Microsoft.EntityFrameworkCore;
using RPGuild.Application.Campanhas;
using RPGuild.Application.Taberna;
using RPGuild.Domain.Campanhas;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure.Campanhas;

public sealed class CampanhaRepository(AppDbContext db) : ICampanhaRepository, ICampanhasEmComum
{
    public async Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) =>
        await db.Campanhas.AddAsync(campanha, cancellationToken);

    public async Task AdicionarJogadorAsync(MembroDaCampanha membro, CancellationToken cancellationToken) =>
        await db.MembrosDeCampanha.AddAsync(membro, cancellationToken);

    public Task<Campanha?> BuscarPorIdAsync(Guid campanhaId, CancellationToken cancellationToken) =>
        db.Campanhas.SingleOrDefaultAsync(c => c.Id == campanhaId, cancellationToken);

    public Task<bool> EhJogadorAsync(Guid campanhaId, Guid userId, CancellationToken cancellationToken) =>
        db.MembrosDeCampanha.AnyAsync(m => m.CampaignId == campanhaId && m.UserId == userId, cancellationToken);

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

    public async Task<IReadOnlyList<CampanhaGuardada>> ListarDoUsuarioAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Proteção: a criação grava a campanha e a posição juntas, mas, se uma campanha ficar sem posição na grade,
        // ela vai para o início, as mais novas primeiro.
        var campanhas = await (
            from c in db.Campanhas.AsNoTracking()
            where c.OwnerId == userId || db.MembrosDeCampanha.Any(m => m.CampaignId == c.Id && m.UserId == userId)
            join s in db.SistemasDeJogo on c.SystemId equals s.Id
            join u in db.Users on c.OwnerId equals u.Id
            join p in db.OrdemDasCampanhas.Where(p => p.UserId == userId) on c.Id equals p.CampaignId into posicoes
            from p in posicoes.DefaultIfEmpty()
            orderby p == null ? -1 : p.Posicao, c.CreatedAt descending
            select new { Campanha = c, Sistema = s, Mestre = new PessoaGuardada(u.Id, u.Nome, u.Nick, u.AvatarPath) })
            .ToListAsync(cancellationToken);

        var jogadores = await JogadoresAsync([.. campanhas.Select(c => c.Campanha.Id)], cancellationToken);
        return
        [
            .. campanhas.Select(c =>
                new CampanhaGuardada(c.Campanha, c.Sistema, c.Mestre, jogadores.GetValueOrDefault(c.Campanha.Id, []))),
        ];
    }

    public async Task<IReadOnlyDictionary<Guid, int>> ContarAsync(
        Guid userId, IReadOnlyCollection<Guid> amigos, CancellationToken cancellationToken)
    {
        // Quem participa de cada campanha: o mestre e os jogadores.
        var participacoes = db.Campanhas.Select(c => new { CampanhaId = c.Id, UserId = c.OwnerId })
            .Concat(db.MembrosDeCampanha.Select(m => new { CampanhaId = m.CampaignId, m.UserId }));
        var minhas = participacoes.Where(p => p.UserId == userId).Select(p => p.CampanhaId);

        return await participacoes
            .Where(p => amigos.Contains(p.UserId) && minhas.Contains(p.CampanhaId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Total, cancellationToken);
    }

    public void Remover(Campanha campanha) => db.Campanhas.Remove(campanha);

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
