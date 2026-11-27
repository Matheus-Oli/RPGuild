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

/// <summary>Nº 17, para os jogadores, com o nome depois da edição.</summary>
public sealed record CampanhaEditada(Guid CampanhaId, string CampanhaNome) : Aviso(TiposDeNotificacao.CampanhaEditada);

/// <summary>Nº 18, para os jogadores. Sem o id: a campanha não existe mais.</summary>
public sealed record CampanhaExcluida(string CampanhaNome, string MestreNome) : Aviso(TiposDeNotificacao.CampanhaExcluida);

/// <summary>Nº 9, para o convidado, com o mestre de agora.</summary>
public sealed record ConviteRecebido(Guid ConviteId, Guid CampanhaId, string CampanhaNome, string MestreNome, string MestreNick)
    : Aviso(TiposDeNotificacao.ConviteRecebido);

/// <summary>Nº 10, para o mestre, com quem aceitou.</summary>
public sealed record ConviteAceito(Guid ConviteId, Guid CampanhaId, string CampanhaNome, Guid UserId, string Nome, string Nick)
    : Aviso(TiposDeNotificacao.ConviteAceito);

/// <summary>Nº 13, para os outros jogadores (nem quem entrou, nem o mestre), com quem entrou.</summary>
public sealed record JogadorEntrou(Guid CampanhaId, string CampanhaNome, Guid UserId, string Nome, string Nick)
    : Aviso(TiposDeNotificacao.JogadorEntrou);
