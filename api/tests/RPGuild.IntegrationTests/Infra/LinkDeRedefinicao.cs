using System.Text.RegularExpressions;
using RPGuild.Application.Notificacoes;

namespace RPGuild.IntegrationTests.Infra;

public static partial class LinkDeRedefinicao
{
    /// <summary>
    /// Link de redefinição que veio no corpo do e-mail.
    /// </summary>
    public static string Extrair(EmailMessage mensagem)
    {
        var link = Link().Match(mensagem.Texto);
        link.Success.ShouldBeTrue("O e-mail não tem o link de redefinição.");
        return link.Value;
    }

    /// <summary>
    /// Token opaco do link, pronto para o reset-password.
    /// </summary>
    public static string Token(EmailMessage mensagem) =>
        Uri.UnescapeDataString(new Uri(Extrair(mensagem)).Query.Split("token=")[1]);

    [GeneratedRegex(@"https?://\S+/redefinir-senha\?\S+")]
    private static partial Regex Link();
}
