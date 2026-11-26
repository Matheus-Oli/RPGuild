using FluentValidation.TestHelper;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class RedefinirSenhaRequestValidatorTests
{
    private static readonly RedefinirSenhaRequest Valido = new("dG9rZW4tb3BhY28", "novaSenha1");

    private readonly RedefinirSenhaRequestValidator _validator = new();

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
    [InlineData("   ")]
    public void Validate_TokenVazio_TemErroEmToken(string? token)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Token = token! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc1234")]
    [InlineData("somenteletras")]
    [InlineData("12345678")]
    public void Validate_NovaSenhaForaDaPolitica_TemErroEmNovaSenha(string? senha)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { NovaSenha = senha! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.NovaSenha);
    }

    [Theory]
    [InlineData("senha123")]
    [InlineData("SENHA123")]
    [InlineData("çãoçãoç1")]
    public void Validate_NovaSenhaComLetraENumero_NaoTemErroEmNovaSenha(string senha)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { NovaSenha = senha });

        // Assert
        resultado.ShouldNotHaveValidationErrorFor(r => r.NovaSenha);
    }
}
