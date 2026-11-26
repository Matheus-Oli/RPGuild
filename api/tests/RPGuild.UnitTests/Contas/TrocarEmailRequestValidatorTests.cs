using FluentValidation.TestHelper;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class TrocarEmailRequestValidatorTests
{
    private static readonly TrocarEmailRequest Valido = new("novo@rpguild.app", "qualquer");

    private readonly TrocarEmailRequestValidator _validator = new();

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
    [InlineData("sem-arroba")]
    [InlineData("mestre@")]
    public void Validate_NovoEmailInvalido_TemErroEmNovoEmail(string? email)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { NovoEmail = email! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.NovoEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_SenhaVazia_TemErroEmSenha(string? senha)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Senha = senha! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Senha);
    }
}
