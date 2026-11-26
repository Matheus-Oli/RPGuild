using RPGuild.Application.Taberna;
using RPGuild.Domain.Taberna;

namespace RPGuild.UnitTests.Taberna;

/// <summary>
/// Repositório em memória: guarda as instâncias para o teste inspecionar o estado depois.
/// </summary>
public sealed class AmizadeRepositoryFalso : IAmizadeRepository
{
    public List<Amizade> Amizades { get; } = [];

    public int VezesSalvo { get; private set; }

    public Task<Amizade?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Amizades.SingleOrDefault(a => a.Id == id));

    public Task<Amizade?> BuscarDoParAsync(Guid usuarioA, Guid usuarioB, CancellationToken cancellationToken) =>
        Task.FromResult(Amizades.SingleOrDefault(a => a.Envolve(usuarioA) && a.Envolve(usuarioB)));

    public Task AdicionarAsync(Amizade amizade, CancellationToken cancellationToken)
    {
        Amizades.Add(amizade);
        return Task.CompletedTask;
    }

    public void Remover(Amizade amizade) => Amizades.Remove(amizade);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        VezesSalvo++;
        return Task.CompletedTask;
    }
}
