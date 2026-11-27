using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RPGuild.Api.Http;
using RPGuild.Application.Campanhas;

namespace RPGuild.Api.TempoReal;

/// <summary>
/// Campanha em tempo real (/hubs/campaign). O front entra no grupo ao abrir a campanha e sai ao fechar; o mestre
/// também entra no grupo só dele.
/// </summary>
[Authorize]
public sealed class CampaignHub(AcessoACampanha acessos) : Hub
{
    public const string Rota = "/hubs/campaign";

    /// <summary>
    /// Nomes dos eventos que o front escuta (os mesmos do documento do projeto).
    /// </summary>
    public static class Eventos
    {
        public const string MemberChanged = nameof(MemberChanged);
        public const string CampaignUpdated = nameof(CampaignUpdated);
        public const string CampaignDeleted = nameof(CampaignDeleted);
        public const string InvitesChanged = nameof(InvitesChanged);
    }

    /// <summary>
    /// Só o mestre ou um jogador, conferido a cada chamada. Para os outros (ou campanha inexistente), a mesma mensagem
    /// genérica, que não revela se a campanha existe.
    /// </summary>
    public async Task JoinCampaign(Guid campanhaId)
    {
        var userId = Context.User!.Id();
        var acesso = await acessos.DeMembroAsync(userId, campanhaId, Context.ConnectionAborted);
        if (!acesso.Sucesso)
        {
            throw new HubException("Campanha não encontrada");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GruposDaCampanha.Todos(campanhaId), Context.ConnectionAborted);
        if (acesso.Valor!.OwnerId == userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GruposDaCampanha.Mestre(campanhaId), Context.ConnectionAborted);
        }
    }

    /// <summary>
    /// Sai dos dois grupos. Sem erro se a conexão não estava neles.
    /// </summary>
    public async Task LeaveCampaign(Guid campanhaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GruposDaCampanha.Todos(campanhaId), Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GruposDaCampanha.Mestre(campanhaId), Context.ConnectionAborted);
    }
}
