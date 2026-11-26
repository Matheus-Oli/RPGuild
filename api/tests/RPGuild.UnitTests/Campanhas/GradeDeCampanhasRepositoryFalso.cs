using RPGuild.Application.Campanhas;

namespace RPGuild.UnitTests.Campanhas;

/// <summary>
/// Repositório em memória: a ordem e as campanhas de cada usuário, para o teste inspecionar depois.
/// </summary>
public sealed class GradeDeCampanhasRepositoryFalso : IGradeDeCampanhasRepository
{
    public Dictionary<Guid, List<Guid>> Ordens { get; } = [];

    public Dictionary<Guid, List<Guid>> Campanhas { get; } = [];

    public Task<IReadOnlyList<Guid>> OrdemAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(Ordens.GetValueOrDefault(userId) ?? []);

    public Task<IReadOnlyCollection<Guid>> CampanhasDoUsuarioAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Guid>>(Campanhas.GetValueOrDefault(userId) ?? []);

    public Task GravarOrdemAsync(Guid userId, IReadOnlyList<Guid> ordem, CancellationToken cancellationToken)
    {
        Ordens[userId] = [.. ordem];
        return Task.CompletedTask;
    }
}
