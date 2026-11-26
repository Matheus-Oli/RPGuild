using FluentValidation;

namespace RPGuild.Application.Contas;

public sealed class RegistrarRequestValidator : AbstractValidator<RegistrarRequest>
{
    public RegistrarRequestValidator()
    {
        RuleFor(r => r.Nome).Nome();

        RuleFor(r => r.Nick).Nick();

        RuleFor(r => r.Email).Email();

        RuleFor(r => r.Senha).Senha();
    }
}
