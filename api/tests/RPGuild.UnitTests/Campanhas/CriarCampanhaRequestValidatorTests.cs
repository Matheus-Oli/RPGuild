using FluentValidation.TestHelper;
using RPGuild.Application.Campanhas;

namespace RPGuild.UnitTests.Campanhas;

public class CriarCampanhaRequestValidatorTests
{
    private static readonly CriarCampanhaRequest Valido = new("A Mina Perdida", "Uma aventura nas montanhas.", "dnd2024", null, null);

    private readonly CriarCampanhaRequestValidator _validator = new();

    [Fact]
    public void Validate_RequestValido_NaoTemErros()
    {
        // Act
        var resultado = _validator.TestValidate(Valido);

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_SoComNomeESistema_NaoTemErros()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Descricao = null });

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
    public void Validate_NomeCom80Caracteres_NaoTemErroEmNome()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Nome = new string('a', 80) });

        // Assert
        resultado.ShouldNotHaveValidationErrorFor(r => r.Nome);
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

    [Fact]
    public void Validate_DescricaoCom2000Caracteres_NaoTemErroEmDescricao()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Descricao = new string('a', 2000) });

        // Assert
        resultado.ShouldNotHaveValidationErrorFor(r => r.Descricao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_SistemaVazio_TemErroEmSistema(string? sistema)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Sistema = sistema! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Sistema);
    }

    [Fact]
    public void Validate_PathsComMaisDe500Caracteres_TemErroNaCapaENoBanner()
    {
        // Act
        var longo = "campaigns/" + new string('a', 491);
        var resultado = _validator.TestValidate(Valido with { CapaPath = longo, BannerPath = longo });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.CapaPath);
        resultado.ShouldHaveValidationErrorFor(r => r.BannerPath);
    }
}
