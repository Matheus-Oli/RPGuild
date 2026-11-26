using FluentValidation;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Taberna;

public sealed class EnviarPedidoRequestValidator : AbstractValidator<EnviarPedidoRequest>
{
    public EnviarPedidoRequestValidator()
    {
        RuleFor(r => r.EmailOuNick)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o e-mail ou o nick.")
            .Must(texto => AlvoDoPedido.Interpretar(texto) is not null)
            .WithMessage($"Informe um e-mail válido ou um nick de {Nick.TamanhoMinimo} a {Nick.TamanhoMaximo} caracteres (letras, números e \"_\"), com ou sem \"@\" na frente.");
    }
}
