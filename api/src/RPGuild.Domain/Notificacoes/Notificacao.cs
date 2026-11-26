namespace RPGuild.Domain.Notificacoes;

/// <summary>
/// Notificação do sininho. Guarda só o tipo e os dados (JSON, com os nomes de quando foi criada);
/// o front monta o texto.
/// </summary>
public sealed class Notificacao
{
    private Notificacao()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Tipo { get; private set; } = string.Empty;

    /// <summary>
    /// Objeto JSON (JSONB no banco).
    /// </summary>
    public string Dados { get; private set; } = "{}";

    public DateTimeOffset? LidaEm { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool Lida => LidaEm is not null;

    public static Notificacao Criar(Guid userId, string tipo, string dados, DateTimeOffset agora) => new()
    {
        Id = Guid.CreateVersion7(agora),
        UserId = userId,
        Tipo = tipo,
        Dados = dados,
        CreatedAt = agora,
    };

    /// <summary>
    /// Idempotente: marcar de novo mantém a data da primeira leitura.
    /// </summary>
    public void MarcarComoLida(DateTimeOffset agora) => LidaEm ??= agora;
}
