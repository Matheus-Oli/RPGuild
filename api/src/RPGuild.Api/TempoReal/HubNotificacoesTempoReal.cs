using Microsoft.AspNetCore.SignalR;
using RPGuild.Application.Notificacoes;

namespace RPGuild.Api.TempoReal;

/// <summary>
/// Empurra os eventos para todas as conexões do usuário no <see cref="NotificationHub"/>. Payload em camelCase (JSON do SignalR).
/// </summary>
public sealed class HubNotificacoesTempoReal(IHubContext<NotificationHub> hub) : INotificacoesTempoReal
{
    public Task NotificacaoCriadaAsync(Guid userId, NotificacaoResponse notificacao, CancellationToken cancellationToken) =>
        Usuario(userId).SendAsync("NotificationCreated", notificacao, cancellationToken);

    private IClientProxy Usuario(Guid userId) => hub.Clients.User(userId.ToString());
}
