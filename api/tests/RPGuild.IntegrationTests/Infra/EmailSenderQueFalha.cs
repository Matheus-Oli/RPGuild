using RPGuild.Application.Notificacoes;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Simula o SMTP fora do ar.
/// </summary>
public sealed class EmailSenderQueFalha : IEmailSender
{
    public Task EnviarAsync(EmailMessage msg, CancellationToken ct) =>
        throw new InvalidOperationException("SMTP indisponível (simulado nos testes).");
}
