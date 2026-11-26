using FluentValidation;

namespace RPGuild.Application.Contas;

public sealed class ConfirmarTrocaDeEmailRequestValidator : AbstractValidator<ConfirmarTrocaDeEmailRequest>
{
    public ConfirmarTrocaDeEmailRequestValidator()
    {
        RuleFor(r => r.Token).NotEmpty().WithMessage("Informe o token.");
    }
}
