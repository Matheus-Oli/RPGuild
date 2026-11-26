namespace RPGuild.Application.Taberna;

/// <param name="EmailOuNick">E-mail, nick ou @nick de quem vai receber o pedido.</param>
public sealed record EnviarPedidoRequest(string EmailOuNick);

/// <summary>
/// Outra pessoa nas listas da Taberna. Nunca leva e-mail.
/// </summary>
public sealed record UsuarioDaTaberna(Guid UserId, string Nome, string Nick, string? AvatarUrl);

public sealed record PedidoEnviadoResponse(Guid Id, UsuarioDaTaberna Para, DateTimeOffset CriadoEm);

public sealed record PedidoRecebidoResponse(Guid Id, UsuarioDaTaberna De, DateTimeOffset CriadoEm);

/// <summary>
/// Pedidos pendentes, os mais novos primeiro. Sem paginação.
/// </summary>
public sealed record PedidosResponse(IReadOnlyList<PedidoRecebidoResponse> Recebidos, IReadOnlyList<PedidoEnviadoResponse> Enviados);

/// <param name="AmigosDesde">Quando o pedido foi aceito.</param>
public sealed record AmigoResponse(
    Guid UserId, string Nome, string Nick, string? AvatarUrl, int CampanhasEmComum, DateTimeOffset AmigosDesde);

/// <summary>
/// A outra pessoa de uma linha da lista, como está guardada (o avatar ainda é o path no storage).
/// </summary>
public sealed record PessoaDaLista(Guid Id, string Nome, string Nick, string? AvatarPath);

public sealed record AmizadeDaLista(PessoaDaLista Amigo, DateTimeOffset AmigosDesde);

/// <param name="Enviado">Se fui eu que enviei; senão, é um pedido que recebi.</param>
public sealed record PedidoPendente(Guid Id, bool Enviado, PessoaDaLista Outro, DateTimeOffset CriadoEm);
