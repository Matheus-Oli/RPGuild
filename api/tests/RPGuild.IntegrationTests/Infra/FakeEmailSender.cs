using System.Collections.Concurrent;
using RPGuild.Application.Notificacoes;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Guarda os e-mails em memória no lugar do SMTP.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _enviadas = new();

    public IReadOnlyCollection<EmailMessage> Enviadas => _enviadas;

    public Task EnviarAsync(EmailMessage msg, CancellationToken ct)
    {
        _enviadas.Enqueue(msg);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Última mensagem enviada para o e-mail (sem diferenciar caixa), ou null se não houver nenhuma.
    /// </summary>
    public EmailMessage? UltimaPara(string email) =>
        _enviadas.LastOrDefault(m => m.Para.Equals(email, StringComparison.OrdinalIgnoreCase));
}
