namespace RPGuild.Application.Contas;

/// <summary>
/// Perfil como está guardado. A URL pública do avatar é montada no caso de uso, que conhece o storage.
/// </summary>
public sealed record DadosDoPerfil(Guid Id, string Nome, string Nick, string Email, string? AvatarPath);
