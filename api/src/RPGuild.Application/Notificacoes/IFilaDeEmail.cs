namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Fila de envio de e-mail (Hangfire, na Infrastructure): a requisição só enfileira e um job envia, com novas
/// tentativas se falhar. A mensagem fica gravada na fila, então nunca enfileire e-mail com token.
/// </summary>
public interface IFilaDeEmail
{
    void Enfileirar(EmailMessage mensagem);
}
