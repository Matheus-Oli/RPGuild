using RPGuild.Application.Contas;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Todos os textos de e-mail da RPGuild, em texto simples.
/// </summary>
public static class EmailTemplates
{
    private const string Assinatura = "Nos vemos na mesa,\nRPGuild";

    public static EmailMessage BoasVindas(string nome, string email) => new(
        email,
        "Boas-vindas à RPGuild",
        $"""
        Olá, {nome}!

        Sua conta na RPGuild foi criada. Agora é só entrar, chamar os amigos para a Taberna e montar a próxima campanha.

        {Assinatura}
        """);

    public static EmailMessage RedefinicaoDeSenha(string nome, string email, string link) => new(
        email,
        "Redefinição de senha da RPGuild",
        $"""
        Olá, {nome}!

        Recebemos um pedido para redefinir a senha da sua conta. Para escolher uma senha nova, abra o link abaixo:

        {link}

        O link vale por {Duracao(RegrasDeConta.ValidadeDoTokenDeRedefinicao)} e só pode ser usado uma vez.
        Se não foi você que pediu, ignore este e-mail: sua senha continua a mesma.

        {Assinatura}
        """);

    private static string Duracao(TimeSpan duracao)
    {
        var horas = (int)duracao.TotalHours;
        return horas == 1 ? "1 hora" : $"{horas} horas";
    }
}
