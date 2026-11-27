namespace RPGuild.Application.Campanhas;

/// <summary>
/// O banco recusou um convite novo porque a pessoa já tem um pendente na campanha: na prática, outro convite para ela
/// entrou ao mesmo tempo.
/// </summary>
public sealed class ConvitePendenteJaExisteException(Exception inner)
    : Exception("Já existe um convite pendente para esta pessoa na campanha.", inner);

/// <summary>
/// O banco recusou o jogador novo porque ele já está na campanha: na prática, outro aceite do mesmo convite entrou ao
/// mesmo tempo.
/// </summary>
public sealed class JogadorJaNaCampanhaException(Exception inner)
    : Exception("Este usuário já é jogador da campanha.", inner);
