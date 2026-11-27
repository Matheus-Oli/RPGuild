using RPGuild.Domain.Campanhas;

namespace RPGuild.Application.Campanhas;

public interface ICampanhaRepository
{
    Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken);

    Task<Campanha?> BuscarPorIdAsync(Guid campanhaId, CancellationToken cancellationToken);

    Task<bool> EhJogadorAsync(Guid campanhaId, Guid userId, CancellationToken cancellationToken);

    Task<CampanhaGuardada?> BuscarComPessoasAsync(Guid campanhaId, CancellationToken cancellationToken);

    /// <summary>
    /// Campanhas em que o usuário é mestre ou jogador, na ordem da grade dele.
    /// </summary>
    Task<IReadOnlyList<CampanhaGuardada>> ListarDoUsuarioAsync(Guid userId, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
