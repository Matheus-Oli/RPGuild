namespace RPGuild.Application.Contas;

public sealed record PerfilResponse(Guid Id, string Nome, string Nick, string Email, string? AvatarPath);
