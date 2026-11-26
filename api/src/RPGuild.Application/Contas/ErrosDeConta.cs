using RPGuild.Application.Comum;

namespace RPGuild.Application.Contas;

public static class ErrosDeConta
{
    public static readonly Erro EmailEmUso = new(
        TipoErro.Conflito, "E-mail em uso", "Já existe uma conta com este e-mail.");

    public static readonly Erro NickEmUso = new(
        TipoErro.Conflito, "Nick em uso", "Este nick já está em uso.");

    public static readonly Erro SessaoInvalida = new(
        TipoErro.NaoAutorizado, "Sessão inválida", "Sua sessão expirou ou não é válida. Entre novamente.");
}
