namespace RPGuild.IntegrationTests.Campanhas.Convites;

public sealed record ConviteDaCampanhaResponse(Guid Id, PessoaDaCampanhaResponse Convidado, DateTimeOffset CriadoEm);

public sealed record CampanhaDoConviteResponse(Guid Id, string Nome, SistemaResponse Sistema, string? CapaUrl);

public sealed record MeuConviteResponse(Guid Id, CampanhaDoConviteResponse Campanha, PessoaDaCampanhaResponse Mestre, DateTimeOffset CriadoEm);

public sealed record CampanhaDoCardResponse(
    Guid Id, string Nome, string? Descricao, SistemaResponse Sistema, string? CapaUrl, string? BannerUrl);

public sealed record QuemConvidouResponse(string Nome, string Nick);

public sealed record ParaQuemResponse(string Nick);

/// <summary>
/// O card do convite. Para quem não é o convidado nem o mestre, só vêm o id, o podeResponder e o para.
/// </summary>
public sealed record CardDoConviteResponse(
    Guid Id,
    string? Status,
    bool PodeResponder,
    CampanhaDoCardResponse? Campanha,
    PessoaDaCampanhaResponse? Mestre,
    List<PessoaDaCampanhaResponse>? NaMesa,
    QuemConvidouResponse? ConvidadoPor,
    ParaQuemResponse Para,
    DateTimeOffset? CriadoEm);

/// <param name="Motivo">nao_amigo, mestre, ja_jogador ou ja_convidado.</param>
public sealed record ConvidadoInvalidoResponse(Guid UserId, string Motivo);
