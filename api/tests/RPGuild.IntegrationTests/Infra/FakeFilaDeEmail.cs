using System.Collections.Concurrent;
using RPGuild.Application.Notificacoes;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// No lugar do Hangfire: "executa o job" na hora, mandando para o sender da factory (o <see cref="FakeEmailSender"/>).
/// Como num job de verdade, uma falha no envio não volta para a requisição.
/// </summary>
public sealed class FakeFilaDeEmail(IEmailSender sender) : IFilaDeEmail
{
    private readonly ConcurrentQueue<EmailMessage> _enfileiradas = new();

    public IReadOnlyCollection<EmailMessage> Enfileiradas => _enfileiradas;

    public void Enfileirar(EmailMessage mensagem)
    {
        _enfileiradas.Enqueue(mensagem);
        try
        {
            sender.EnviarAsync(mensagem, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // O job falharia e o Hangfire tentaria de novo; a requisição já respondeu.
        }
    }

    /// <summary>
    /// Se alguma mensagem para este e-mail passou pela fila (sem diferenciar caixa).
    /// </summary>
    public bool PassouPelaFila(string email) =>
        _enfileiradas.Any(m => m.Para.Equals(email, StringComparison.OrdinalIgnoreCase));
}
