using System.Text.Json.Serialization;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// O que aconteceu, com o que cada canal precisa. Nos tipos com sininho, as propriedades viram os `dados`
/// (JSON em camelCase) gravados na notificação, com os nomes de agora. Nos tipos só de e-mail, servem para montar
/// a mensagem e não são gravadas em lugar nenhum.
/// </summary>
public abstract record Aviso([property: JsonIgnore] string Tipo);

/// <summary>Nº 1.</summary>
public sealed record BoasVindas() : Aviso(TiposDeNotificacao.BoasVindas);

/// <summary>Nº 2. Leva o token no link: sai direto, sem fila.</summary>
public sealed record RedefinicaoDeSenha(string Link) : Aviso(TiposDeNotificacao.RedefinicaoDeSenha);

/// <summary>Nº 3.</summary>
public sealed record SenhaAlterada() : Aviso(TiposDeNotificacao.SenhaAlterada);

/// <summary>Nº 4. Vai para o e-mail antigo (emailDestino).</summary>
public sealed record EmailAlterado(string EmailNovo) : Aviso(TiposDeNotificacao.EmailAlterado);

/// <summary>Confirmação do e-mail novo (emailDestino). Leva o token no link: sai direto, sem fila.</summary>
public sealed record ConfirmacaoDeEmail(string Link) : Aviso(TiposDeNotificacao.ConfirmacaoDeEmail);

/// <summary>Nº 5, para o destinatário do pedido.</summary>
public sealed record AmizadeRecebida(Guid PedidoId, Guid DeUserId, string DeNome, string DeNick)
    : Aviso(TiposDeNotificacao.AmizadeRecebida);

/// <summary>Nº 6, para quem enviou o pedido, com quem aceitou.</summary>
public sealed record AmizadeAceita(Guid UserId, string Nome, string Nick) : Aviso(TiposDeNotificacao.AmizadeAceita);

/// <summary>Nº 7, para quem enviou o pedido, com quem recusou.</summary>
public sealed record AmizadeRecusada(Guid UserId, string Nome, string Nick) : Aviso(TiposDeNotificacao.AmizadeRecusada);
