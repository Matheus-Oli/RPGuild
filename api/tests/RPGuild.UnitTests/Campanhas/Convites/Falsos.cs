using RPGuild.Application.Campanhas;
using RPGuild.Application.Comum;
using RPGuild.Domain.Campanhas;

namespace RPGuild.UnitTests.Campanhas.Convites;

/// <summary>
/// Campanhas, jogadores e pessoas em memória.
/// </summary>
public sealed class CampanhaRepositoryFalso : ICampanhaRepository
{
    private static readonly SistemaDeJogo Dnd = new(1, "dnd2024", "D&D 5e (regras de 2024)", ativo: true);

    public List<Campanha> Campanhas { get; } = [];

    public List<MembroDaCampanha> Membros { get; } = [];

    public Dictionary<Guid, PessoaGuardada> Pessoas { get; } = [];

    public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
    {
        Campanhas.Add(campanha);
        return Task.CompletedTask;
    }

    public Task AdicionarJogadorAsync(MembroDaCampanha membro, CancellationToken cancellationToken)
    {
        Membros.Add(membro);
        return Task.CompletedTask;
    }

    public Task<Campanha?> BuscarPorIdAsync(Guid campanhaId, CancellationToken cancellationToken) =>
        Task.FromResult(Campanhas.SingleOrDefault(c => c.Id == campanhaId));

    public Task<bool> EhJogadorAsync(Guid campanhaId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Membros.Any(m => m.CampaignId == campanhaId && m.UserId == userId));

    public Task<CampanhaGuardada?> BuscarComPessoasAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        var campanha = Campanhas.SingleOrDefault(c => c.Id == campanhaId);
        return Task.FromResult(campanha is null
            ? null
            : new CampanhaGuardada(
                campanha,
                Dnd,
                Pessoas[campanha.OwnerId],
                [.. Membros.Where(m => m.CampaignId == campanhaId).Select(m => new JogadorGuardado(Pessoas[m.UserId], m.JoinedAt))]));
    }

    // Consultas com join: cobertas pelos testes de integração.
    public Task<IReadOnlyList<CampanhaGuardada>> ListarDoUsuarioAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public void Remover(Campanha campanha) => Campanhas.Remove(campanha);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Convites em memória, para o teste inspecionar o estado depois.
/// </summary>
public sealed class ConviteRepositoryFalso : IConviteRepository
{
    public List<ConviteDeCampanha> Convites { get; } = [];

    public Task AdicionarAsync(ConviteDeCampanha convite, CancellationToken cancellationToken)
    {
        Convites.Add(convite);
        return Task.CompletedTask;
    }

    public Task<ConviteDeCampanha?> BuscarPorIdAsync(Guid conviteId, CancellationToken cancellationToken) =>
        Task.FromResult(Convites.SingleOrDefault(c => c.Id == conviteId));

    public Task<IReadOnlyList<ConviteDeCampanha>> PendentesDaCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ConviteDeCampanha>>([.. Convites.Where(c => c.CampaignId == campanhaId && c.EstaPendente)]);

    // Consultas com join: cobertas pelos testes de integração.
    public Task<IReadOnlyList<ConviteDeCampanha>> PendentesEntreAsync(Guid mestreId, Guid convidadoId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ConviteComConvidado>> ListarPendentesDaCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ConviteGuardado>> ListarPendentesDoUsuarioAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Sem banco: o trabalho só roda.
/// </summary>
public sealed class TransacaoFalsa : ITransacao
{
    public Task ExecutarAsync(Func<Task> trabalho, CancellationToken cancellationToken) => trabalho();
}
