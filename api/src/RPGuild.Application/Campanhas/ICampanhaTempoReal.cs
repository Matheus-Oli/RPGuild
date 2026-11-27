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

    /// <summary>
    /// Para o grupo da campanha.
    /// </summary>
    Task CampanhaAtualizadaAsync(CampanhaAtualizadaEvento evento, CancellationToken cancellationToken);

    /// <summary>
    /// Para o grupo da campanha.
    /// </summary>
    Task CampanhaExcluidaAsync(CampanhaExcluidaEvento evento, CancellationToken cancellationToken);
}

/// <param name="Tipo">"entrou" (e, no módulo de saída de player, "saiu").</param>
/// <param name="Membro">No formato do GET /campaigns/{id}/members.</param>
public sealed record MembroMudouEvento(Guid CampanhaId, string Tipo, MembroDaCampanhaResponse Membro);

/// <summary>
/// A campanha como ficou depois de editada. Sem o papel: o evento é igual para o mestre e os jogadores.
/// </summary>
public sealed record CampanhaAtualizadaEvento(
    Guid Id, string Nome, string? Descricao, SistemaDaCampanhaResponse Sistema, string? CapaUrl, string? BannerUrl);

public sealed record CampanhaExcluidaEvento(Guid CampanhaId, string CampanhaNome);

public static class TiposDeMudancaDeMembro
{
    public const string Entrou = "entrou";
}
