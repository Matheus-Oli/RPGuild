using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
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

    public static string CalcularHash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private (RefreshToken Token, RefreshTokenEmitido Emitido) Gerar(Guid userId, Guid familyId)
    {
        var valor = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TamanhoEmBytes));
        var token = RefreshToken.Criar(userId, CalcularHash(valor), familyId, relogio.GetUtcNow(), Validade);

        return (token, new RefreshTokenEmitido(valor, userId, token.ExpiresAt));
    }
}
