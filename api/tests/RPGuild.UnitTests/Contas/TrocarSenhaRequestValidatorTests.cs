using FluentValidation.TestHelper;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class TrocarSenhaRequestValidatorTests
{
    private static readonly TrocarSenhaRequest Valido = new("senhaAntiga1", "senhaNova22");

    private readonly TrocarSenhaRequestValidator _validator = new();

    [Fact]
    public void Validate_RequestValido_NaoTemErros()
    {
        // Act
        var resultado = _validator.TestValidate(Valido);

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_AtualVazia_TemErroEmAtual(string? atual)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Atual = atual! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Atual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc1234")]
    [InlineData("somenteletras")]
    [InlineData("12345678")]
    public void Validate_NovaForaDaPolitica_TemErroEmNova(string? nova)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Nova = nova! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Nova);
    }

    [Fact]
    public void Validate_NovaComLetraENumero_NaoTemErroEmNova()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Nova = "çãoçãoç1" });

        // Assert
        resultado.ShouldNotHaveValidationErrorFor(r => r.Nova);
    }
}
