namespace RPGuild.Application.Comum;

public enum TipoErro
{
    Validacao,
    NaoAutorizado,
    Conflito,
    Bloqueado,
    NaoEncontrado,
    Proibido,
}

/// <summary>
/// Falha esperada de um caso de uso. A Api traduz o <see cref="Tipo"/> para o status HTTP.
/// </summary>
public sealed record Erro(TipoErro Tipo, string Titulo, string Detalhe);

/// <summary>
/// Valor de sucesso de um caso de uso que não devolve nada.
/// </summary>
public sealed record Vazio
{
    public static readonly Vazio Valor = new();

    private Vazio()
    {
    }
}

public sealed class Resultado<T>
{
    private Resultado(T? valor, Erro? erro)
    {
        Valor = valor;
        Erro = erro;
    }

    public bool Sucesso => Erro is null;

    public T? Valor { get; }

    public Erro? Erro { get; }

    public static implicit operator Resultado<T>(T valor) => new(valor, null);

    public static implicit operator Resultado<T>(Erro erro) => new(default, erro);
}
