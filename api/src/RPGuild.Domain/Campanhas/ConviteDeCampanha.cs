namespace RPGuild.Domain.Campanhas;

public enum StatusDoConvite
{
    Pendente,
    Aceito,
    Recusado,
    Cancelado,
}

/// <summary>
/// Convite do mestre para um amigo jogar na campanha. Só pendente pode ser aceito, recusado ou cancelado; depois de
/// recusado ou cancelado, um convite novo pode ser feito na hora (a linha antiga fica como histórico).
/// </summary>
public sealed class ConviteDeCampanha
{
    private ConviteDeCampanha()
    {
    }

    public Guid Id { get; private set; }

    public Guid CampaignId { get; private set; }

    public Guid InvitedBy { get; private set; }

    public Guid UserId { get; private set; }

    public StatusDoConvite Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    public bool EstaPendente => Status == StatusDoConvite.Pendente;

    public static ConviteDeCampanha Criar(Guid campanhaId, Guid convidadoPor, Guid convidado, DateTimeOffset agora) => new()
    {
        Id = Guid.CreateVersion7(agora),
        CampaignId = campanhaId,
        InvitedBy = convidadoPor,
        UserId = convidado,
        Status = StatusDoConvite.Pendente,
        CreatedAt = agora,
    };

    public void Aceitar(DateTimeOffset agora) => Mudar(StatusDoConvite.Aceito, agora);

    public void Recusar(DateTimeOffset agora) => Mudar(StatusDoConvite.Recusado, agora);

    public void Cancelar(DateTimeOffset agora) => Mudar(StatusDoConvite.Cancelado, agora);

    private void Mudar(StatusDoConvite status, DateTimeOffset agora)
    {
        if (!EstaPendente)
        {
            throw new InvalidOperationException("Só um convite pendente pode ser aceito, recusado ou cancelado.");
        }

        Status = status;
        RespondedAt = agora;
    }
}
