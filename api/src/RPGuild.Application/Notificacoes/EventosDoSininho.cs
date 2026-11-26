using Microsoft.Extensions.Logging;
using RPGuild.Domain.Notificacoes;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Ponto único de emissão dos eventos do sininho em tempo real. Chame só depois de gravar no banco.
/// Uma falha ao empurrar é registrada (sem dados pessoais) e não quebra a requisição: o GET continua com tudo.
/// </summary>
public sealed class EventosDoSininho(INotificacoesTempoReal tempoReal, ILogger<EventosDoSininho> logger)
{
    public Task CriadaAsync(Guid userId, Notificacao notificacao, CancellationToken cancellationToken) =>
        EmpurrarAsync("notificação criada", () => tempoReal.NotificacaoCriadaAsync(userId, NotificacaoResponse.De(notificacao), cancellationToken));

    private async Task EmpurrarAsync(string evento, Func<Task> empurrar)
    {
        try
        {
            await empurrar();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao empurrar o evento de {Evento} em tempo real", evento);
        }
    }
}
