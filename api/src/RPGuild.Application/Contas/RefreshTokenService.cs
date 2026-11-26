using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using RPGuild.Application.Comum;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public sealed record RefreshTokenEmitido(string Token, Guid UserId, DateTimeOffset ExpiresAt);

public sealed class RefreshTokenService(IRefreshTokenRepository repositorio, TimeProvider relogio)
{
    public const int TamanhoEmBytes = 64;

    public static readonly TimeSpan Validade = TimeSpan.FromDays(14);

    public async Task<RefreshTokenEmitido> EmitirAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (token, emitido) = Gerar(userId, Guid.CreateVersion7(relogio.GetUtcNow()));

        await repositorio.AdicionarAsync(token, cancellationToken);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        return emitido;
    }

    /// <summary>
    /// Troca um refresh token por um novo da mesma família, revogando o atual.
    /// </summary>
    public async Task<Resultado<RefreshTokenEmitido>> RotacionarAsync(string token, CancellationToken cancellationToken)
    {
        var atual = await repositorio.BuscarPorHashAsync(CalcularHash(token), cancellationToken);
        var agora = relogio.GetUtcNow();
        if (atual is null || atual.EstaExpirado(agora))
        {
            return ErrosDeConta.SessaoInvalida;
        }

        var (novo, emitido) = Gerar(atual.UserId, atual.FamilyId);
        atual.Revogar(agora, novo.Id);

        await repositorio.AdicionarAsync(novo, cancellationToken);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        return emitido;
    }

    public static string CalcularHash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private (RefreshToken Token, RefreshTokenEmitido Emitido) Gerar(Guid userId, Guid familyId)
    {
        var valor = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TamanhoEmBytes));
        var token = RefreshToken.Criar(userId, CalcularHash(valor), familyId, relogio.GetUtcNow(), Validade);

        return (token, new RefreshTokenEmitido(valor, userId, token.ExpiresAt));
    }
}
