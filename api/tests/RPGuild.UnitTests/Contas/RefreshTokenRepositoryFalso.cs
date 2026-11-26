using RPGuild.Application.Contas;
using RPGuild.Domain.Contas;

namespace RPGuild.UnitTests.Contas;

/// <summary>
/// Repositório em memória: guarda as instâncias para o teste inspecionar o estado depois.
/// </summary>
public sealed class RefreshTokenRepositoryFalso : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public int VezesSalvo { get; private set; }

    public Task AdicionarAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> BuscarPorHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.SingleOrDefault(t => t.TokenHash == tokenHash));

    public Task<IReadOnlyList<RefreshToken>> ListarDaFamiliaAsync(Guid familyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>([.. Tokens.Where(t => t.FamilyId == familyId)]);

    public Task<IReadOnlyList<RefreshToken>> ListarNaoRevogadosDoUsuarioAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>([.. Tokens.Where(t => t.UserId == userId && !t.EstaRevogado)]);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        VezesSalvo++;
        return Task.CompletedTask;
    }
}
