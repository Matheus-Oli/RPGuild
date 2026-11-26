using FluentValidation;

namespace RPGuild.Application.Contas;

/// <summary>
/// Formato dos campos. Se o avatar está na pasta do usuário, só dá para saber no caso de uso, que conhece o usuário.
/// </summary>
public sealed class AtualizarPerfilRequestValidator : AbstractValidator<AtualizarPerfilRequest>
{
    public AtualizarPerfilRequestValidator()
    {
        RuleFor(r => r.Nome).Nome();

        RuleFor(r => r.Nick).Nick();

        RuleFor(r => r.AvatarPath)
            .MaximumLength(RegrasDeConta.AvatarPathTamanhoMaximo)
            .WithMessage($"O caminho do avatar pode ter no máximo {RegrasDeConta.AvatarPathTamanhoMaximo} caracteres.");
    }
}
