using FluentValidation.TestHelper;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class RegistrarRequestValidatorTests
{
    private static readonly RegistrarRequest Valido = new("Mestre da Mesa", "mestre_01", "mestre@rpguild.app", "senha1234");

    private readonly RegistrarRequestValidator _validator = new();

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
    public void Validate_NomeVazio_TemErroEmNome(string? nome)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Nome = nome! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Nome);
    }

    [Fact]
    public void Validate_NomeComMaisDe100Caracteres_TemErroEmNome()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Nome = new string('a', 101) });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Nome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("nick com espaco")]
    public void Validate_NickInvalido_TemErroEmNick(string? nick)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Nick = nick! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Nick);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("mestre@")]
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
    [InlineData("abc1234")]
    [InlineData("somenteletras")]
    [InlineData("12345678")]
    public void Validate_SenhaForaDaPolitica_TemErroEmSenha(string? senha)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Senha = senha! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Senha);
    }

    [Theory]
    [InlineData("senha123")]
    [InlineData("SENHA123")]
    [InlineData("çãoçãoç1")]
    public void Validate_SenhaComLetraENumero_NaoTemErroEmSenha(string senha)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Senha = senha });

        // Assert
        resultado.ShouldNotHaveValidationErrorFor(r => r.Senha);
    }
}
