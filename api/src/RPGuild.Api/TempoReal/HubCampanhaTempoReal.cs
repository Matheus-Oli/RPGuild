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

    private IClientProxy Todos(Guid campanhaId) => hub.Clients.Group(GruposDaCampanha.Todos(campanhaId));
}
