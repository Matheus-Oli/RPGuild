using FluentValidation;

namespace RPGuild.Application.Contas;

public static class RegrasDeConta
{
    public const int NomeTamanhoMaximo = 100;
    public const int EmailTamanhoMaximo = 256;
    public const int SenhaTamanhoMinimo = 8;

    public static readonly TimeSpan ValidadeDoTokenDeRedefinicao = TimeSpan.FromHours(1);

    /// <summary>
    /// O token de troca de e-mail sai do mesmo provider do Identity que o de redefinição, então a validade é a mesma.
    /// </summary>
    public static readonly TimeSpan ValidadeDoTokenDeTrocaDeEmail = ValidadeDoTokenDeRedefinicao;

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("Informe o e-mail.")
        .MaximumLength(EmailTamanhoMaximo).WithMessage($"O e-mail pode ter no máximo {EmailTamanhoMaximo} caracteres.")
        .EmailAddress().WithMessage("E-mail inválido.");

    public static IRuleBuilderOptions<T, string> Senha<T>(this IRuleBuilderInitial<T, string> regra) => regra
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("Informe a senha.")
        .MinimumLength(SenhaTamanhoMinimo).WithMessage($"A senha precisa de pelo menos {SenhaTamanhoMinimo} caracteres.")
        .Must(senha => senha.Any(char.IsLetter)).WithMessage("A senha precisa de pelo menos uma letra.")
        .Must(senha => senha.Any(char.IsDigit)).WithMessage("A senha precisa de pelo menos um número.");
}
