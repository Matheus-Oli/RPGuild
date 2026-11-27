namespace RPGuild.Application.Taberna;

/// <summary>
/// O que a Taberna precisa de Campanhas para a lista de amigos.
/// </summary>
public interface ICampanhasEmComum
{
    /// <summary>
    /// Para cada amigo, em quantas campanhas os dois participam (como mestre ou jogador). Amigo sem nenhuma fica de fora.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> ContarAsync(Guid userId, IReadOnlyCollection<Guid> amigos, CancellationToken cancellationToken);
}
