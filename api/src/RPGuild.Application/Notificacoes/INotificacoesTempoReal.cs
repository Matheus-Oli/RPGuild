namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Porta para empurrar os eventos do sininho às conexões abertas do usuário (SignalR, na Api).
/// Use pelo <see cref="EventosDoSininho"/>, que só chama depois de gravar e não deixa uma falha quebrar a requisição.
/// </summary>
public interface INotificacoesTempoReal
{
    Task NotificacaoCriadaAsync(Guid userId, NotificacaoResponse notificacao, CancellationToken cancellationToken);

    Task NaoLidasAsync(Guid userId, ContagemDeNaoLidasResponse contagem, CancellationToken cancellationToken);

    Task NotificacaoLidaAsync(Guid userId, Guid notificacaoId, CancellationToken cancellationToken);

    Task TodasLidasAsync(Guid userId, CancellationToken cancellationToken);

    Task NotificacaoApagadaAsync(Guid userId, Guid notificacaoId, CancellationToken cancellationToken);
}
