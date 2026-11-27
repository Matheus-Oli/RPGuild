using RPGuild.Domain.Campanhas;

namespace RPGuild.Application.Campanhas;

public sealed record SistemaDaCampanhaResponse(string Slug, string Nome);

/// <summary>
/// Mestre ou jogador nas respostas de campanha. Nunca leva e-mail.
/// </summary>
public sealed record PessoaDaCampanhaResponse(Guid UserId, string Nome, string Nick, string? AvatarUrl);

/// <param name="Papel">"mestre" ou "jogador": o papel de quem pediu.</param>
/// <param name="ProximaSessao">Sempre null por enquanto (módulo de Sessões).</param>
public sealed record CampanhaCardResponse(
    Guid Id,
    string Nome,
    SistemaDaCampanhaResponse Sistema,
    string Papel,
    string? CapaUrl,
    PessoaDaCampanhaResponse Mestre,
    IReadOnlyList<PessoaDaCampanhaResponse> Jogadores,
    object? ProximaSessao);

public sealed record EstatisticasDaCampanhaResponse(int SessoesJogadas, int HorasDeMesa, int Documentos);

/// <summary>
/// Os campos do card, mais os da visão geral. "Onde paramos", a atividade e as estatísticas vêm vazios até os módulos
/// de Sessões e Documentos.
/// </summary>
public sealed record CampanhaDetalheResponse(
    Guid Id,
    string Nome,
    SistemaDaCampanhaResponse Sistema,
    string Papel,
    string? CapaUrl,
    PessoaDaCampanhaResponse Mestre,
    IReadOnlyList<PessoaDaCampanhaResponse> Jogadores,
    object? ProximaSessao,
    string? Descricao,
    string? BannerUrl,
    DateTimeOffset CriadaEm,
    EstatisticasDaCampanhaResponse Estatisticas,
    object? OndeParamos,
    IReadOnlyList<object> AtividadeRecente);

/// <param name="EntrouEm">Para o mestre, a criação da campanha.</param>
/// <param name="Ficha">Sempre null por enquanto (módulo de Fichas).</param>
public sealed record MembroDaCampanhaResponse(
    Guid UserId, string Nome, string Nick, string? AvatarUrl, string Papel, DateTimeOffset EntrouEm, object? Ficha);

public static class PapeisNaCampanha
{
    public const string Mestre = "mestre";
    public const string Jogador = "jogador";
}

/// <summary>
/// Uma pessoa como está guardada (o avatar ainda é o path no storage).
/// </summary>
public sealed record PessoaGuardada(Guid Id, string Nome, string Nick, string? AvatarPath);

public sealed record JogadorGuardado(PessoaGuardada Pessoa, DateTimeOffset EntrouEm);

/// <summary>
/// A campanha com o sistema, o mestre e os jogadores (do que entrou primeiro para o último).
/// </summary>
public sealed record CampanhaGuardada(Campanha Campanha, SistemaDeJogo Sistema, PessoaGuardada Mestre, IReadOnlyList<JogadorGuardado> Jogadores);
