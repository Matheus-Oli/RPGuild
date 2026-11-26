using System.Text.RegularExpressions;

namespace RPGuild.Domain.Contas;

/// <summary>
/// Apelido público do usuário: de 3 a 20 caracteres, só letras sem acento, números e "_".
/// É único sem diferenciar maiúsculas de minúsculas (comparar sempre por <see cref="Normalizado"/>).
/// </summary>
public sealed partial record Nick
{
    public const int TamanhoMinimo = 3;
    public const int TamanhoMaximo = 20;

    private Nick(string valor)
    {
        Valor = valor;
        Normalizado = Normalizar(valor);
    }

    public string Valor { get; }

    public string Normalizado { get; }

    public static bool EhValido(string? valor) => valor is not null && Formato().IsMatch(valor);

    public static Nick Criar(string valor)
    {
        if (!EhValido(valor))
        {
            throw new ArgumentException(
                $"O nick deve ter de {TamanhoMinimo} a {TamanhoMaximo} caracteres, só letras, números e \"_\".",
                nameof(valor));
        }

        return new Nick(valor);
    }

    public static string Normalizar(string valor) => valor.ToLowerInvariant();

    public override string ToString() => Valor;

    [GeneratedRegex("^[A-Za-z0-9_]{3,20}$")]
    private static partial Regex Formato();
}
