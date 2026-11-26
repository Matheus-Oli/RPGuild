using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public interface IRefreshTokenRepository
{
    Task AdicionarAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> BuscarPorHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> ListarDaFamiliaAsync(Guid familyId, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
