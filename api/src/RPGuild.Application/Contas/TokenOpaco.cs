using System.Buffers.Text;
using System.Text;

namespace RPGuild.Application.Contas;

/// <summary>
/// Token que vai nos links de conta (redefinição de senha, troca de e-mail): um texto opaco com o id do usuário
/// e o token do Identity.
/// </summary>
public sealed record TokenOpaco(Guid UserId, string TokenIdentity)
{
    private const char Separador = ':';

    public string Codificar() => Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{UserId}{Separador}{TokenIdentity}"));

    /// <summary>
    /// Devolve null se o texto não for um token válido.
    /// </summary>
    public static TokenOpaco? Decodificar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || !Base64Url.IsValid(texto))
        {
            return null;
        }

        string conteudo;
        try
        {
            conteudo = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(texto));
        }
        catch (FormatException)
        {
            return null;
        }

        var separador = conteudo.IndexOf(Separador);
        if (separador < 0 || !Guid.TryParse(conteudo.AsSpan(0, separador), out var userId))
        {
            return null;
        }

        var tokenIdentity = conteudo[(separador + 1)..];
        return tokenIdentity.Length == 0 ? null : new TokenOpaco(userId, tokenIdentity);
    }
}
