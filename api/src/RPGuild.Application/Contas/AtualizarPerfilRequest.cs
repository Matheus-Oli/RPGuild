namespace RPGuild.Application.Contas;

/// <param name="AvatarPath">Path no storage (avatars/{userId}/...), ou null para ficar sem avatar.</param>
public sealed record AtualizarPerfilRequest(string Nome, string Nick, string? AvatarPath);
