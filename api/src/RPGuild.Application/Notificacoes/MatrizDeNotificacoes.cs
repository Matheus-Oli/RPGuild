namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Os tipos como ficam gravados em notifications.tipo. O front monta o texto a partir do tipo e dos dados.
/// </summary>
public static class TiposDeNotificacao
{
    public const string BoasVindas = "boas_vindas";
    public const string RedefinicaoDeSenha = "redefinicao_senha";
    public const string SenhaAlterada = "senha_alterada";
    public const string EmailAlterado = "email_alterado";
    public const string ConfirmacaoDeEmail = "confirmacao_email";
    public const string AmizadeRecebida = "amizade_recebida";
    public const string AmizadeAceita = "amizade_aceita";
    public const string AmizadeRecusada = "amizade_recusada";
}

public enum EnvioPorEmail
{
    Nenhum,

    /// <summary>
    /// Pela fila (Hangfire), com novas tentativas.
    /// </summary>
    Fila,

    /// <summary>
    /// Na hora, sem fila: o e-mail leva token, e o token não pode ficar gravado nas tabelas da fila.
    /// </summary>
    Direto,
}

public sealed record CanaisDaNotificacao(bool Sininho, EnvioPorEmail Email);

/// <summary>
/// Canal de cada tipo de notificação, num lugar só (a matriz de "Notificações" do documento do projeto).
/// O nº 8 (amigo removido) não tem tipo: ninguém é avisado.
/// </summary>
public static class MatrizDeNotificacoes
{
    private static readonly Dictionary<string, CanaisDaNotificacao> Canais = new()
    {
        [TiposDeNotificacao.BoasVindas] = new(Sininho: true, EnvioPorEmail.Fila),
        [TiposDeNotificacao.RedefinicaoDeSenha] = new(Sininho: false, EnvioPorEmail.Direto),
        [TiposDeNotificacao.SenhaAlterada] = new(Sininho: false, EnvioPorEmail.Fila),
        [TiposDeNotificacao.EmailAlterado] = new(Sininho: false, EnvioPorEmail.Fila),
        [TiposDeNotificacao.ConfirmacaoDeEmail] = new(Sininho: false, EnvioPorEmail.Direto),
        [TiposDeNotificacao.AmizadeRecebida] = new(Sininho: true, EnvioPorEmail.Fila),
        [TiposDeNotificacao.AmizadeAceita] = new(Sininho: true, EnvioPorEmail.Nenhum),
        [TiposDeNotificacao.AmizadeRecusada] = new(Sininho: true, EnvioPorEmail.Nenhum),
    };

    public static CanaisDaNotificacao CanaisDe(string tipo) =>
        Canais.TryGetValue(tipo, out var canais)
            ? canais
            : throw new ArgumentException($"Tipo de notificação desconhecido: \"{tipo}\".", nameof(tipo));
}
