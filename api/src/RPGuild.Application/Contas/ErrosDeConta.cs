using RPGuild.Application.Comum;

namespace RPGuild.Application.Contas;

public static class ErrosDeConta
{
    public static readonly Erro EmailEmUso = new(
        TipoErro.Conflito, "E-mail em uso", "Já existe uma conta com este e-mail.");

    public static readonly Erro NickEmUso = new(
        TipoErro.Conflito, "Nick em uso", "Este nick já está em uso.");

    public static readonly Erro CredenciaisInvalidas = new(
        TipoErro.NaoAutorizado, "Credenciais inválidas", "E-mail ou senha incorretos.");

    public static readonly Erro ContaBloqueada = new(
        TipoErro.Bloqueado, "Conta bloqueada", "Muitas tentativas de login. Tente de novo em 15 minutos.");

    public static readonly Erro EmailNaoCadastrado = new(
        TipoErro.NaoEncontrado, "E-mail não cadastrado", "Não existe conta com este e-mail.");

    public static readonly Erro TokenDeRedefinicaoInvalido = new(
        TipoErro.Validacao, "Token inválido", "O link de redefinição é inválido, já foi usado ou expirou. Peça um novo.");

    public static readonly Erro SessaoInvalida = new(
        TipoErro.NaoAutorizado, "Sessão inválida", "Sua sessão expirou ou não é válida. Entre novamente.");
}
