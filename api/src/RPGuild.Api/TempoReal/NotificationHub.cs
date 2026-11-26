using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace RPGuild.Api.TempoReal;

/// <summary>
/// Sininho em tempo real (/hubs/notifications). O cliente não chama nada: o hub só empurra eventos para o usuário.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public const string Rota = "/hubs/notifications";
}

/// <summary>
/// O usuário de cada conexão é o "sub" do access token: <c>Clients.User(userId)</c> chega em todas as abas e aparelhos dele.
/// </summary>
public sealed class UsuarioPeloSub : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
