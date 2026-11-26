using FluentValidation;

namespace RPGuild.Application.Contas;

/// <summary>
/// No login só se exige o preenchimento: a política de senha vale no cadastro.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).Email();

        RuleFor(r => r.Senha).NotEmpty().WithMessage("Informe a senha.");
    }
}
