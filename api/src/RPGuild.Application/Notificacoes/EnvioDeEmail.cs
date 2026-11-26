using Microsoft.Extensions.Logging;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Envio na hora, sem fila, usado pelo <see cref="Notificador"/> só nos e-mails com token (<see cref="EnvioPorEmail.Direto"/>):
/// uma falha no envio é registrada e não quebra a requisição.
/// </summary>
public sealed class EnvioDeEmail(IEmailSender emails, ILogger<EnvioDeEmail> logger)
{
    public async Task EnviarAsync(EmailMessage mensagem, CancellationToken cancellationToken)
    {
        try
        {
            await emails.EnviarAsync(mensagem, cancellationToken);
        }
        catch (Exception ex)
        {
            // Só o assunto e o destinatário mascarado: o corpo pode ter token.
            logger.LogError(
                ex, "Falha ao enviar o e-mail \"{Assunto}\" para {Para}", mensagem.Assunto, MascaraDeEmail.Mascarar(mensagem.Para));
        }
    }
}
