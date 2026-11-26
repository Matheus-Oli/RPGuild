using Microsoft.Extensions.Logging;
using RPGuild.Domain.Notificacoes;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Ponto único de emissão dos eventos do sininho em tempo real. Chame só depois de gravar no banco.
/// Uma falha ao empurrar é registrada (sem dados pessoais) e não quebra a requisição: o GET continua com tudo.
/// </summary>
public sealed class EventosDoSininho(
    INotificacoesTempoReal tempoReal, INotificacaoRepository notificacoes, ILogger<EventosDoSininho> logger)
{
    public async Task CriadaAsync(Guid userId, Notificacao notificacao, CancellationToken cancellationToken)
    {
        await EmpurrarAsync("notificação criada", () => tempoReal.NotificacaoCriadaAsync(userId, NotificacaoResponse.De(notificacao), cancellationToken));
        await NaoLidasAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Depois de marcar uma como lida (só quando mudou o estado): as outras abas tiram o destaque dela.
    /// </summary>
    public async Task LidaAsync(Guid userId, Guid notificacaoId, CancellationToken cancellationToken)
    {
        await EmpurrarAsync("notificação lida", () => tempoReal.NotificacaoLidaAsync(userId, notificacaoId, cancellationToken));
        await NaoLidasAsync(userId, cancellationToken);
    }

    public async Task TodasLidasAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EmpurrarAsync("todas lidas", () => tempoReal.TodasLidasAsync(userId, cancellationToken));
        await NaoLidasAsync(userId, cancellationToken);
    }

    public async Task ApagadaAsync(Guid userId, Guid notificacaoId, CancellationToken cancellationToken)
    {
        await EmpurrarAsync("notificação apagada", () => tempoReal.NotificacaoApagadaAsync(userId, notificacaoId, cancellationToken));
        await NaoLidasAsync(userId, cancellationToken);
    }

    // O total é recontado no banco: vale para todas as abas, mesmo as que perderam algum evento.
    private Task NaoLidasAsync(Guid userId, CancellationToken cancellationToken) =>
        EmpurrarAsync("contagem de não lidas", async () =>
        {
            var total = await notificacoes.ContarNaoLidasAsync(userId, cancellationToken);
            await tempoReal.NaoLidasAsync(userId, new ContagemDeNaoLidasResponse(total), cancellationToken);
        });

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
