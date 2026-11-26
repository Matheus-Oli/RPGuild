namespace RPGuild.Application.Campanhas;

/// <summary>
/// A exclusão pede duas confirmações: o nome da campanha e a frase. As duas precisam ser exatas (maiúsculas e acentos
/// contam); só os espaços nas pontas são ignorados.
/// </summary>
public static class ConfirmacaoDeExclusao
{
    public const string Frase = "desejo excluir a campanha";

    public static bool Confere(string nomeDaCampanha, string? confirmacaoNome, string? confirmacaoFrase) =>
        string.Equals(confirmacaoNome?.Trim(), nomeDaCampanha.Trim(), StringComparison.Ordinal)
        && string.Equals(confirmacaoFrase?.Trim(), Frase, StringComparison.Ordinal);
}
