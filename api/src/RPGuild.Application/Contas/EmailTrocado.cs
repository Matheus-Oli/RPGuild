namespace RPGuild.Application.Contas;

public sealed record EmailTrocado(Guid UserId, string Nome, string EmailAntigo, string EmailNovo);
