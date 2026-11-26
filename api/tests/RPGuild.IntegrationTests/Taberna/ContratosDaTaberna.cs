namespace RPGuild.IntegrationTests.Taberna;

public sealed record PessoaResponse(Guid UserId, string Nome, string Nick, string? AvatarUrl);

public sealed record PedidoEnviadoResponse(Guid Id, PessoaResponse Para, DateTimeOffset CriadoEm);

public sealed record PedidoRecebidoResponse(Guid Id, PessoaResponse De, DateTimeOffset CriadoEm);

public sealed record PedidosResponse(List<PedidoRecebidoResponse> Recebidos, List<PedidoEnviadoResponse> Enviados);

public sealed record AmigoResponse(
    Guid UserId, string Nome, string Nick, string? AvatarUrl, int CampanhasEmComum, DateTimeOffset AmigosDesde);
