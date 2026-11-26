using Microsoft.Extensions.Logging;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Ponto único de envio de e-mail dos casos de uso: uma falha no envio é registrada e não quebra a requisição.
/// </summary>
public sealed class EnvioDeEmail(IEmailSender emails, ILogger<EnvioDeEmail> logger)
{
    public async Task EnviarAsync(EmailMessage mensagem, CancellationToken cancellationToken)
    {
        try
        {
            // TODO(hangfire): mover envio para fila
            await emails.EnviarAsync(mensagem, cancellationToken);
        }
        catch (Exception ex)
        {
            // Só o assunto e o destinatário: o corpo pode ter token.
            logger.LogError(ex, "Falha ao enviar o e-mail \"{Assunto}\" para {Para}", mensagem.Assunto, mensagem.Para);
        }
    }
}
