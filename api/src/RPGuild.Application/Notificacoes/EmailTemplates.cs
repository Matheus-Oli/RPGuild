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

    public static EmailMessage ConfirmacaoDeNovoEmail(string nome, string novoEmail, string link) => new(
        novoEmail,
        "Confirme seu novo e-mail na RPGuild",
        $"""
        Olá, {nome}!

        Recebemos um pedido para trocar o e-mail da sua conta na RPGuild para este endereço. Para confirmar, abra o link abaixo:

        {link}

        O link vale por {Duracao(RegrasDeConta.ValidadeDoTokenDeTrocaDeEmail)} e só pode ser usado uma vez. Até lá, o e-mail da conta continua o antigo.
        Se não foi você que pediu, ignore este e-mail: nada muda.

        {Assinatura}
        """);

    // Vai para o endereço antigo: se a troca foi de um invasor, é o único jeito de o dono saber.
    public static EmailMessage EmailAlterado(string nome, string emailAntigo, string emailNovo) => new(
        emailAntigo,
        "O e-mail da sua conta na RPGuild foi alterado",
        $"""
        Olá, {nome}!

        O e-mail da sua conta na RPGuild foi alterado para {MascaraDeEmail.Mascarar(emailNovo)}. A partir de agora, o login e os avisos usam o endereço novo, e as outras sessões abertas foram encerradas.

        Se não foi você, alguém pode ter acessado sua conta: responda a este e-mail o quanto antes para recuperarmos o acesso.

        {Assinatura}
        """);

    public static EmailMessage SenhaAlterada(string nome, string email, string site) => new(
        email,
        "A senha da sua conta na RPGuild foi alterada",
        $"""
        Olá, {nome}!

        A senha da sua conta na RPGuild acabou de ser alterada, e as outras sessões abertas foram encerradas.

        Se não foi você, redefina a senha agora pelo "Esqueci minha senha" na tela de login: {site}

        {Assinatura}
        """);

    // Aviso nº 5. Leva o @nick de quem pediu, nunca o e-mail.
    public static EmailMessage PedidoDeAmizade(string nome, string email, string nomeDeQuemPediu, string nickDeQuemPediu, string link) => new(
        email,
        $"{nomeDeQuemPediu} te enviou um pedido de amizade na RPGuild",
        $"""
        Olá, {nome}!

        {nomeDeQuemPediu} (@{nickDeQuemPediu}) te enviou um pedido de amizade na RPGuild. Para aceitar ou recusar, abra a Taberna:

        {link}

        {Assinatura}
        """);

    // Aviso nº 18. Sem link: a campanha não existe mais.
    public static EmailMessage CampanhaExcluida(string nome, string email, string nomeDaCampanha, string nomeDoMestre) => new(
        email,
        $"A campanha {nomeDaCampanha} foi excluída",
        $"""
        Olá, {nome}!

        {nomeDoMestre} excluiu a campanha {nomeDaCampanha} na RPGuild. Ela não aparece mais na sua lista de campanhas.

        {Assinatura}
        """);

    private static string Duracao(TimeSpan duracao)
    {
        var horas = (int)duracao.TotalHours;
        return horas == 1 ? "1 hora" : $"{horas} horas";
    }
}
