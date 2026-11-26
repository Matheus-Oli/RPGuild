using FluentValidation.TestHelper;
using RPGuild.Application.Campanhas;

namespace RPGuild.UnitTests.Campanhas;

public class ExcluirCampanhaRequestValidatorTests
{
    private static readonly ExcluirCampanhaRequest Valido = new("A Mina Perdida", "desejo excluir a campanha");

    private readonly ExcluirCampanhaRequestValidator _validator = new();

    [Fact]
    public void Validate_RequestPreenchido_NaoTemErros()
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
    public void Validate_ConfirmacaoNomeVazia_TemErroEmConfirmacaoNome(string? nome)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { ConfirmacaoNome = nome! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.ConfirmacaoNome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ConfirmacaoFraseVazia_TemErroEmConfirmacaoFrase(string? frase)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { ConfirmacaoFrase = frase! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.ConfirmacaoFrase);
    }
}
