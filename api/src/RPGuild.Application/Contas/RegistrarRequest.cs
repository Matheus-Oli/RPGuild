namespace RPGuild.Application.Contas;

public sealed record RegistrarRequest(string Nome, string Nick, string Email, string Senha);
