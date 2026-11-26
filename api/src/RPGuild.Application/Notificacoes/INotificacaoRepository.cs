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

    Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
