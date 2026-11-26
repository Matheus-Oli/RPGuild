namespace RPGuild.Application.Taberna;

/// <param name="EmailOuNick">E-mail, nick ou @nick de quem vai receber o pedido.</param>
public sealed record EnviarPedidoRequest(string EmailOuNick);

/// <summary>
/// Outra pessoa nas listas da Taberna. Nunca leva e-mail.
/// </summary>
public sealed record UsuarioDaTaberna(Guid UserId, string Nome, string Nick, string? AvatarUrl);

public sealed record PedidoEnviadoResponse(Guid Id, UsuarioDaTaberna Para, DateTimeOffset CriadoEm);
