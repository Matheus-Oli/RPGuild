using System.Text.Json;

namespace RPGuild.IntegrationTests.Campanhas;

public sealed record SistemaDisponivelResponse(string Slug, string Nome, bool Ativo);

public sealed record SistemaResponse(string Slug, string Nome);

public sealed record PessoaDaCampanhaResponse(Guid UserId, string Nome, string Nick, string? AvatarUrl);

public sealed record CardResponse(
    Guid Id,
    string Nome,
    SistemaResponse Sistema,
    string Papel,
    string? CapaUrl,
    PessoaDaCampanhaResponse Mestre,
    List<PessoaDaCampanhaResponse> Jogadores,
    JsonElement? ProximaSessao);

public sealed record EstatisticasResponse(int SessoesJogadas, int HorasDeMesa, int Documentos);

public sealed record DetalheResponse(
    Guid Id,
    string Nome,
    SistemaResponse Sistema,
    string Papel,
    string? CapaUrl,
    PessoaDaCampanhaResponse Mestre,
    List<PessoaDaCampanhaResponse> Jogadores,
    JsonElement? ProximaSessao,
    string? Descricao,
    string? BannerUrl,
    DateTimeOffset CriadaEm,
    EstatisticasResponse Estatisticas,
    JsonElement? OndeParamos,
    List<JsonElement> AtividadeRecente);

public sealed record MembroResponse(
    Guid UserId, string Nome, string Nick, string? AvatarUrl, string Papel, DateTimeOffset EntrouEm, JsonElement? Ficha);
