namespace RPGuild.Application.Campanhas;

/// <summary>
/// Porta para empurrar os eventos de campanha às conexões nos grupos dela (SignalR, na Api).
/// Use pelo <see cref="EventosDaCampanha"/>, que só chama depois de gravar e não deixa uma falha quebrar a requisição.
/// </summary>
public interface ICampanhaTempoReal
{
    /// <summary>
    /// Para o grupo da campanha (mestre e jogadores com ela aberta).
    /// </summary>
    Task MembroMudouAsync(MembroMudouEvento evento, CancellationToken cancellationToken);
}

/// <param name="Tipo">"entrou" (e, no módulo de saída de player, "saiu").</param>
/// <param name="Membro">No formato do GET /campaigns/{id}/members.</param>
public sealed record MembroMudouEvento(Guid CampanhaId, string Tipo, MembroDaCampanhaResponse Membro);

public static class TiposDeMudancaDeMembro
{
    public const string Entrou = "entrou";
}
