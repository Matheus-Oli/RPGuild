using FluentValidation;

namespace RPGuild.Application.Contas;

public sealed class RedefinirSenhaRequestValidator : AbstractValidator<RedefinirSenhaRequest>
{
    public RedefinirSenhaRequestValidator()
    {
        RuleFor(r => r.Token).NotEmpty().WithMessage("Informe o token.");

        RuleFor(r => r.NovaSenha)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(RegrasDeConta.SenhaTamanhoMinimo)
            .WithMessage($"A senha precisa de pelo menos {RegrasDeConta.SenhaTamanhoMinimo} caracteres.")
            .Must(senha => senha.Any(char.IsLetter)).WithMessage("A senha precisa de pelo menos uma letra.")
            .Must(senha => senha.Any(char.IsDigit)).WithMessage("A senha precisa de pelo menos um número.");
    }
}
