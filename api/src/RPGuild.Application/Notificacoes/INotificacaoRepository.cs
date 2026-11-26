using RPGuild.Domain.Notificacoes;

namespace RPGuild.Application.Notificacoes;

public interface INotificacaoRepository
{
    /// <summary>
    /// Notificações do usuário, das mais novas para as mais antigas (desempate pelo id), depois do cursor.
    /// </summary>
    Task<IReadOnlyList<Notificacao>> ListarAsync(
        Guid userId, bool naoLidas, CursorDeNotificacoes? apos, int quantidade, CancellationToken cancellationToken);

    Task<int> ContarNaoLidasAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// A notificação, se for deste usuário; senão, null.
    /// </summary>
    Task<Notificacao?> BuscarDoUsuarioAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task MarcarTodasComoLidasAsync(Guid userId, DateTimeOffset agora, CancellationToken cancellationToken);

    void Remover(Notificacao notificacao);

    /// <summary>
    /// Apaga, de todos os usuários, as criadas antes do instante.
    /// </summary>
    Task ApagarCriadasAntesDeAsync(DateTimeOffset limite, CancellationToken cancellationToken);

    Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
