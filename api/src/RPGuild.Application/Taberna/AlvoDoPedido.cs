using RPGuild.Application.Contas;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Taberna;

public enum TipoDeAlvo
{
    Email,
    Nick,
}

/// <summary>
/// Para quem vai o pedido de amizade, a partir do texto digitado ("e-mail ou nick").
/// </summary>
/// <param name="Valor">O e-mail (sem espaços nas pontas) ou o nick já normalizado.</param>
public sealed record AlvoDoPedido(TipoDeAlvo Tipo, string Valor)
{
    private const char Arroba = '@';

    /// <summary>
    /// Com "@" no meio, é e-mail (mesma regra de formato do cadastro). Senão, é nick, aceitando um "@" na frente
    /// ("@muirenn"). Devolve null para vazio ou inválido.
    /// </summary>
    public static AlvoDoPedido? Interpretar(string? texto)
    {
        var valor = texto?.Trim();
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        var arroba = valor.IndexOf(Arroba);
        if (arroba > 0 && arroba < valor.Length - 1 && arroba == valor.LastIndexOf(Arroba))
        {
            return valor.Length <= RegrasDeConta.EmailTamanhoMaximo ? new AlvoDoPedido(TipoDeAlvo.Email, valor) : null;
        }

        var nick = arroba == 0 ? valor[1..] : valor;
        return Nick.EhValido(nick) ? new AlvoDoPedido(TipoDeAlvo.Nick, Nick.Normalizar(nick)) : null;
    }
}
