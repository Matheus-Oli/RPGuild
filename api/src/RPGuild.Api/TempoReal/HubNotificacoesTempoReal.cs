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

    public Task NaoLidasAsync(Guid userId, ContagemDeNaoLidasResponse contagem, CancellationToken cancellationToken) =>
        Usuario(userId).SendAsync("UnreadCount", contagem, cancellationToken);

    public Task NotificacaoLidaAsync(Guid userId, Guid notificacaoId, CancellationToken cancellationToken) =>
        Usuario(userId).SendAsync("NotificationRead", new { id = notificacaoId }, cancellationToken);

    public Task TodasLidasAsync(Guid userId, CancellationToken cancellationToken) =>
        Usuario(userId).SendAsync("NotificationsReadAll", new { }, cancellationToken);

    public Task NotificacaoApagadaAsync(Guid userId, Guid notificacaoId, CancellationToken cancellationToken) =>
        Usuario(userId).SendAsync("NotificationDeleted", new { id = notificacaoId }, cancellationToken);

    private IClientProxy Usuario(Guid userId) => hub.Clients.User(userId.ToString());
}
