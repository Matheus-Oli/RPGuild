using Microsoft.AspNetCore.SignalR;
using RPGuild.Application.Campanhas;

namespace RPGuild.Api.TempoReal;

/// <summary>
/// Empurra os eventos para os grupos da campanha no <see cref="CampaignHub"/>. Payload em camelCase (JSON do SignalR).
/// </summary>
public sealed class HubCampanhaTempoReal(IHubContext<CampaignHub> hub) : ICampanhaTempoReal
{
    public Task MembroMudouAsync(MembroMudouEvento evento, CancellationToken cancellationToken) =>
        Todos(evento.CampanhaId).SendAsync(CampaignHub.Eventos.MemberChanged, evento, cancellationToken);

    public Task CampanhaAtualizadaAsync(CampanhaAtualizadaEvento evento, CancellationToken cancellationToken) =>
        Todos(evento.Id).SendAsync(CampaignHub.Eventos.CampaignUpdated, evento, cancellationToken);

    public Task CampanhaExcluidaAsync(CampanhaExcluidaEvento evento, CancellationToken cancellationToken) =>
        Todos(evento.CampanhaId).SendAsync(CampaignHub.Eventos.CampaignDeleted, evento, cancellationToken);

    public Task ConvitesMudaramAsync(ConvitesMudaramEvento evento, CancellationToken cancellationToken) =>
        hub.Clients.Group(GruposDaCampanha.Mestre(evento.CampanhaId))
            .SendAsync(CampaignHub.Eventos.InvitesChanged, evento, cancellationToken);

    private IClientProxy Todos(Guid campanhaId) => hub.Clients.Group(GruposDaCampanha.Todos(campanhaId));
}
