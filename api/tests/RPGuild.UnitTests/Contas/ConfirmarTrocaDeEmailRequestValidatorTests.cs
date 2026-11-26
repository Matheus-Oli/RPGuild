using FluentValidation.TestHelper;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class ConfirmarTrocaDeEmailRequestValidatorTests
{
    private readonly ConfirmarTrocaDeEmailRequestValidator _validator = new();

    [Fact]
    public void Validate_TokenPreenchido_NaoTemErros()
    {
        // Act
        var resultado = _validator.TestValidate(new ConfirmarTrocaDeEmailRequest("dG9rZW4tb3BhY28"));

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_TokenVazio_TemErroEmToken(string? token)
    {
        // Act
        var resultado = _validator.TestValidate(new ConfirmarTrocaDeEmailRequest(token!));

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Token);
    }
}
