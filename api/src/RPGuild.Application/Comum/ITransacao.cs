namespace RPGuild.Application.Comum;

/// <summary>
/// Porta para uma transação do banco: o que o trabalho grava entra todo junto, ou nada entra.
/// </summary>
public interface ITransacao
{
    /// <summary>
    /// Se o trabalho lançar, a transação é desfeita e a exceção sobe. Dentro de outra transação, entra nela.
    /// </summary>
    Task ExecutarAsync(Func<Task> trabalho, CancellationToken cancellationToken);
}
