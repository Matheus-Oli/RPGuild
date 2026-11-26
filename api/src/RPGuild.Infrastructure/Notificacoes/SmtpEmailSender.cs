using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using RPGuild.Application.Notificacoes;

namespace RPGuild.Infrastructure.Notificacoes;

/// <summary>
/// Envio por SMTP (Gmail, com senha de app). Uma conexão por e-mail: o volume é baixo.
/// </summary>
public sealed class SmtpEmailSender(EmailOptions options) : IEmailSender
{
    public async Task EnviarAsync(EmailMessage msg, CancellationToken ct)
    {
        using var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(options.RemetenteNome, options.RemetenteEmail));
        mensagem.To.Add(MailboxAddress.Parse(msg.Para));
        mensagem.Subject = msg.Assunto;
        mensagem.Body = new TextPart(TextFormat.Plain) { Text = msg.Texto };

        using var cliente = new SmtpClient();
        var seguranca = options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await cliente.ConnectAsync(options.Host, options.Port, seguranca, ct);
        await cliente.AuthenticateAsync(options.Usuario, options.Senha, ct);
        await cliente.SendAsync(new SendRequest(mensagem), ct);
        await cliente.DisconnectAsync(true, ct);
    }
}
