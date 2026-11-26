namespace RPGuild.Application.Notificacoes;

/// <summary>
/// E-mail em texto simples.
/// </summary>
public sealed record EmailMessage(string Para, string Assunto, string Texto);
