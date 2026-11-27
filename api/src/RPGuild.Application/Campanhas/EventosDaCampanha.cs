using Microsoft.Extensions.Logging;

namespace RPGuild.Application.Campanhas;

/// <summary>
/// Ponto único de emissão dos eventos de campanha em tempo real. Chame só depois de gravar no banco.
/// Uma falha ao empurrar é registrada (sem dados pessoais) e não quebra a requisição: o GET continua com tudo.
/// </summary>
public sealed class EventosDaCampanha(ICampanhaTempoReal tempoReal, ILogger<EventosDaCampanha> logger)
{
    // TODO(saida-de-player): quando o jogador for removido (ou sair), as conexões dele saem dos grupos da campanha e o
    // grupo recebe MemberChanged com tipo "saiu".
    public Task JogadorEntrouAsync(Guid campanhaId, MembroDaCampanhaResponse membro, CancellationToken cancellationToken) =>
        EmpurrarAsync(
            "membro mudou",
            () => tempoReal.MembroMudouAsync(new MembroMudouEvento(campanhaId, TiposDeMudancaDeMembro.Entrou, membro), cancellationToken));

    private async Task EmpurrarAsync(string evento, Func<Task> empurrar)
    {
        try
        {
            await empurrar();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao empurrar o evento de campanha {Evento} em tempo real", evento);
        }
    }
}
