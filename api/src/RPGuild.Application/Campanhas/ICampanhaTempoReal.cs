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

    /// <summary>
    /// Só para o grupo do mestre: os jogadores não veem os convites.
    /// </summary>
    Task ConvitesMudaramAsync(ConvitesMudaramEvento evento, CancellationToken cancellationToken);
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

/// <param name="Acao">"criado", "aceito", "recusado" ou "cancelado".</param>
public sealed record ConvitesMudaramEvento(Guid CampanhaId, string Acao, ConviteDoEvento Convite);

/// <param name="Status">O status depois da ação: pendente, aceito, recusado ou cancelado.</param>
public sealed record ConviteDoEvento(Guid Id, string Status, PessoaDaCampanhaResponse Convidado, DateTimeOffset CriadoEm);

public static class AcoesDeConvite
{
    public const string Criado = "criado";
    public const string Aceito = "aceito";
    public const string Recusado = "recusado";
    public const string Cancelado = "cancelado";
}

public static class TiposDeMudancaDeMembro
{
    public const string Entrou = "entrou";
}
