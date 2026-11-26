using FluentValidation;

namespace RPGuild.Application.Contas;

public static class RegrasDeConta
{
    public const int NomeTamanhoMaximo = 100;
    public const int EmailTamanhoMaximo = 256;
    public const int SenhaTamanhoMinimo = 8;

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("Informe o e-mail.")
        .MaximumLength(EmailTamanhoMaximo).WithMessage($"O e-mail pode ter no máximo {EmailTamanhoMaximo} caracteres.")
        .EmailAddress().WithMessage("E-mail inválido.");
}
