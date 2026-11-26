namespace RPGuild.Application.Notificacoes;

public interface IEmailSender
{
    Task EnviarAsync(EmailMessage msg, CancellationToken ct);
}
