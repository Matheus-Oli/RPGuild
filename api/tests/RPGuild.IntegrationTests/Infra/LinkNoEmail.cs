using System.Text.RegularExpressions;
using RPGuild.Application.Notificacoes;

namespace RPGuild.IntegrationTests.Infra;

public static class LinkNoEmail
{
    public const string RedefinirSenha = "redefinir-senha";
    public const string ConfirmarEmail = "confirmar-email";

    /// <summary>
    /// Link para a rota do front (ex.: "redefinir-senha") que veio no corpo do e-mail.
    /// </summary>
    public static string Extrair(EmailMessage mensagem, string rota)
    {
        var link = Regex.Match(mensagem.Texto, $@"https?://\S+/{Regex.Escape(rota)}\?\S+");
        link.Success.ShouldBeTrue($"O e-mail não tem o link de {rota}.");
        return link.Value;
    }

    /// <summary>
    /// Token opaco do link, pronto para mandar de volta à API.
    /// </summary>
    public static string Token(EmailMessage mensagem, string rota) =>
        Uri.UnescapeDataString(new Uri(Extrair(mensagem, rota)).Query.Split("token=")[1]);
}
