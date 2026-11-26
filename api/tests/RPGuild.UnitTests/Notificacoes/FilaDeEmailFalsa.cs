using RPGuild.Application.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

/// <summary>
/// Só guarda o que foi enfileirado.
/// </summary>
public sealed class FilaDeEmailFalsa : IFilaDeEmail
{
    public List<EmailMessage> Mensagens { get; } = [];

    public void Enfileirar(EmailMessage mensagem) => Mensagens.Add(mensagem);
}
