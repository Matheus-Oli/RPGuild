using FluentValidation;

namespace RPGuild.Application.Contas;

public sealed class EsqueciSenhaRequestValidator : AbstractValidator<EsqueciSenhaRequest>
{
    public EsqueciSenhaRequestValidator()
    {
        RuleFor(r => r.Email).Email();
    }
}
