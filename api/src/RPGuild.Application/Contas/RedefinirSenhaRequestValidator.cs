using FluentValidation;

namespace RPGuild.Application.Contas;

public sealed class RedefinirSenhaRequestValidator : AbstractValidator<RedefinirSenhaRequest>
{
    public RedefinirSenhaRequestValidator()
    {
        RuleFor(r => r.Token).NotEmpty().WithMessage("Informe o token.");

        RuleFor(r => r.NovaSenha).Senha();
    }
}
