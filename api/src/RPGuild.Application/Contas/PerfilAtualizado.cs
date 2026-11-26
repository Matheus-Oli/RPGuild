namespace RPGuild.Application.Contas;

/// <param name="AvatarAnterior">Avatar de antes da atualização, para apagar do storage se mudou.</param>
public sealed record PerfilAtualizado(PerfilResponse Perfil, string? AvatarAnterior);
