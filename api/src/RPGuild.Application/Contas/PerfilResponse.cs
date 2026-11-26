namespace RPGuild.Application.Contas;

/// <param name="AvatarUrl">URL pública do avatar no storage, ou null sem avatar.</param>
public sealed record PerfilResponse(Guid Id, string Nome, string Nick, string Email, string? AvatarPath, string? AvatarUrl);
