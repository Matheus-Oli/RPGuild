namespace RPGuild.Domain.Contas;

/// <summary>
/// Refresh token persistido. Só o hash fica guardado; o valor em claro vai apenas no cookie do cliente.
/// Tokens rotacionados a partir do mesmo login compartilham a <see cref="FamilyId"/>.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public Guid FamilyId { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedById { get; private set; }

    public static RefreshToken Criar(Guid userId, string tokenHash, Guid familyId, DateTimeOffset agora, TimeSpan validade) => new()
    {
        Id = Guid.CreateVersion7(agora),
        UserId = userId,
        TokenHash = tokenHash,
        FamilyId = familyId,
        CreatedAt = agora,
        ExpiresAt = agora + validade,
    };
}
