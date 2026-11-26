using RPGuild.Domain.Taberna;

namespace RPGuild.Application.Taberna;

public interface IAmizadeRepository
{
    Task<Amizade?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// A linha do par, em qualquer sentido (quem pediu pode ser qualquer um dos dois).
    /// </summary>
    Task<Amizade?> BuscarDoParAsync(Guid usuarioA, Guid usuarioB, CancellationToken cancellationToken);

    /// <summary>
    /// Pedidos pendentes enviados e recebidos pelo usuário, com a outra pessoa, os mais novos primeiro.
    /// </summary>
    Task<IReadOnlyList<PedidoPendente>> ListarPendentesAsync(Guid userId, CancellationToken cancellationToken);

    Task AdicionarAsync(Amizade amizade, CancellationToken cancellationToken);

    void Remover(Amizade amizade);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
