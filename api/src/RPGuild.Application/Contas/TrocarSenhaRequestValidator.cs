using FluentValidation;

namespace RPGuild.Application.Contas;

public sealed class TrocarSenhaRequestValidator : AbstractValidator<TrocarSenhaRequest>
{
    public TrocarSenhaRequestValidator()
    {
        RuleFor(r => r.Atual).NotEmpty().WithMessage("Informe a senha atual.");

        RuleFor(r => r.Nova).Senha();
    }
}
