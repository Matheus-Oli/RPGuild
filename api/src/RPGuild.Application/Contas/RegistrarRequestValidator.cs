using FluentValidation;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public sealed class RegistrarRequestValidator : AbstractValidator<RegistrarRequest>
{
    public RegistrarRequestValidator()
    {
        RuleFor(r => r.Nome)
            .NotEmpty().WithMessage("Informe o nome.")
            .MaximumLength(RegrasDeConta.NomeTamanhoMaximo)
            .WithMessage($"O nome pode ter no máximo {RegrasDeConta.NomeTamanhoMaximo} caracteres.");

        RuleFor(r => r.Nick)
            .Must(Nick.EhValido)
            .WithMessage($"O nick deve ter de {Nick.TamanhoMinimo} a {Nick.TamanhoMaximo} caracteres, só letras, números e \"_\".");

        RuleFor(r => r.Email).Email();

        RuleFor(r => r.Senha).Senha();
    }
}
