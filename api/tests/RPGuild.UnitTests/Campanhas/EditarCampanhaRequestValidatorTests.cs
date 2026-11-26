using FluentValidation.TestHelper;
using RPGuild.Application.Campanhas;

namespace RPGuild.UnitTests.Campanhas;

public class EditarCampanhaRequestValidatorTests
{
    private static readonly EditarCampanhaRequest Valido = new("A Mina Perdida", null, null, null);

    private readonly EditarCampanhaRequestValidator _validator = new();

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
    public void Validate_NomeCom81Caracteres_TemErroEmNome()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Nome = new string('a', 81) });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Nome);
    }

    [Fact]
    public void Validate_DescricaoCom2001Caracteres_TemErroEmDescricao()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Descricao = new string('a', 2001) });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Descricao);
    }
}
