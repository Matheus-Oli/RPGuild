namespace RPGuild.Application.Contas;

public sealed record UsuarioResponse(Guid Id, string Nome, string Nick, string Email);
