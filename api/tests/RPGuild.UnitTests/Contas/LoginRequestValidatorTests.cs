using FluentValidation.TestHelper;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class LoginRequestValidatorTests
{
    private static readonly LoginRequest Valido = new("mestre@rpguild.app", "qualquer");

    private readonly LoginRequestValidator _validator = new();

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
    public void Validate_EmailInvalido_TemErroEmEmail(string? email)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Email = email! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Email);
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
