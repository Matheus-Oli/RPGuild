namespace RPGuild.Application.Taberna;

/// <summary>
/// O que a Taberna precisa de Campanhas quando uma amizade é desfeita: os convites pendentes entre os dois deixam de valer.
/// </summary>
public interface IConvitesEntreAmigos
{
    /// <summary>
    /// Cancela e salva os convites pendentes entre os dois (campanhas em que um é mestre e o outro é o convidado). Devolve
    /// o que mandar depois de confirmar a transação (os avisos nº 12 e o InvitesChanged), para quem chamou executar.
    /// </summary>
    Task<Func<CancellationToken, Task>> CancelarPendentesEntreAsync(Guid usuarioA, Guid usuarioB, CancellationToken cancellationToken);
}
