namespace RPGuild.Application.Taberna;

/// <summary>
/// O banco recusou um pedido novo porque o par já tem uma linha: na prática, a outra pessoa pediu ao mesmo tempo.
/// </summary>
public sealed class ParDeAmizadeJaExisteException(Exception inner)
    : Exception("Já existe uma linha de amizade para este par de usuários.", inner);
