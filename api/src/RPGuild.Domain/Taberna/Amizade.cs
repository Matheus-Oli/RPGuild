namespace RPGuild.Domain.Taberna;

public enum StatusDaAmizade
{
    Pendente,
    Aceita,
    Recusada,
}

/// <summary>
/// Pedido de amizade e, depois de aceito, a amizade. Há uma linha só por par de usuários, não importa quem pediu.
/// Remover o amigo ou cancelar o pedido apaga a linha.
/// </summary>
public sealed class Amizade
{
    private Amizade()
    {
    }

    public Guid Id { get; private set; }

    public Guid RequesterId { get; private set; }

    public Guid AddresseeId { get; private set; }

    public StatusDaAmizade Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Quando o pedido foi aceito ou recusado. Na amizade, é o "amigos desde".
    /// </summary>
    public DateTimeOffset? RespondedAt { get; private set; }

    public bool EstaPendente => Status == StatusDaAmizade.Pendente;

    public static Amizade Pedir(Guid de, Guid para, DateTimeOffset agora)
    {
        if (de == para)
        {
            throw new ArgumentException("Não dá para pedir amizade para si mesmo.", nameof(para));
        }

        return new Amizade
        {
            Id = Guid.CreateVersion7(agora),
            RequesterId = de,
            AddresseeId = para,
            Status = StatusDaAmizade.Pendente,
            CreatedAt = agora,
        };
    }

    public bool Envolve(Guid userId) => RequesterId == userId || AddresseeId == userId;

    public void Aceitar(DateTimeOffset agora) => Responder(StatusDaAmizade.Aceita, agora);

    public void Recusar(DateTimeOffset agora) => Responder(StatusDaAmizade.Recusada, agora);

    /// <summary>
    /// Depois de uma recusa, qualquer um dos dois pode pedir de novo na hora: como o par é único, a mesma linha
    /// vira um pedido pendente novo, de <paramref name="de"/> para <paramref name="para"/>.
    /// </summary>
    public void Reenviar(Guid de, Guid para, DateTimeOffset agora)
    {
        if (Status != StatusDaAmizade.Recusada || de == para || !Envolve(de) || !Envolve(para))
        {
            throw new InvalidOperationException("Só um pedido recusado pode ser reenviado, e entre o mesmo par.");
        }

        RequesterId = de;
        AddresseeId = para;
        Status = StatusDaAmizade.Pendente;
        CreatedAt = agora;
        RespondedAt = null;
    }

    private void Responder(StatusDaAmizade status, DateTimeOffset agora)
    {
        if (!EstaPendente)
        {
            throw new InvalidOperationException("Só um pedido pendente pode ser respondido.");
        }

        Status = status;
        RespondedAt = agora;
    }
}
