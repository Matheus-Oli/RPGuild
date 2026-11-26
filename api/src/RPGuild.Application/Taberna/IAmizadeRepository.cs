using RPGuild.Domain.Taberna;

namespace RPGuild.Application.Taberna;

public interface IAmizadeRepository
{
    Task<Amizade?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// A linha do par, em qualquer sentido (quem pediu pode ser qualquer um dos dois).
    /// </summary>
    Task<Amizade?> BuscarDoParAsync(Guid usuarioA, Guid usuarioB, CancellationToken cancellationToken);

    Task AdicionarAsync(Amizade amizade, CancellationToken cancellationToken);

    void Remover(Amizade amizade);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
