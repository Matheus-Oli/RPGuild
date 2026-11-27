using RPGuild.Application.Comum;
using RPGuild.Domain.Campanhas;

namespace RPGuild.Application.Campanhas;

/// <summary>
/// Quem pode o quê numa campanha, para este e os próximos módulos (sessões, fichas, documentos...). Quem não é mestre
/// nem jogador não fica sabendo que a campanha existe (404).
/// </summary>
public sealed class AcessoACampanha(ICampanhaRepository campanhas)
{
    /// <summary>
    /// O mestre ou um jogador. Devolve a campanha rastreada (pode ser alterada e salva).
    /// </summary>
    public async Task<Resultado<Campanha>> DeMembroAsync(Guid userId, Guid campanhaId, CancellationToken cancellationToken)
    {
        var campanha = await campanhas.BuscarPorIdAsync(campanhaId, cancellationToken);
        if (campanha is null || (campanha.OwnerId != userId && !await campanhas.EhJogadorAsync(campanhaId, userId, cancellationToken)))
        {
            return ErrosDeCampanha.CampanhaNaoEncontrada;
        }

        return campanha;
    }
}
