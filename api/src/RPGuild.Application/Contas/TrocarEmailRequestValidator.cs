using FluentValidation;

namespace RPGuild.Application.Contas;

public sealed class TrocarEmailRequestValidator : AbstractValidator<TrocarEmailRequest>
{
    public TrocarEmailRequestValidator()
    {
        RuleFor(r => r.NovoEmail).Email();

        RuleFor(r => r.Senha).NotEmpty().WithMessage("Informe a senha.");
    }
}
