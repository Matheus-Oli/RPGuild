using FluentValidation.TestHelper;
using RPGuild.Application.Campanhas;

namespace RPGuild.UnitTests.Campanhas;

public class OrdenarCampanhasRequestValidatorTests
{
    private readonly OrdenarCampanhasRequestValidator _validator = new();

    [Fact]
    public void Validate_IdsDiferentes_NaoTemErros()
    {
        // Act
        var resultado = _validator.TestValidate(new OrdenarCampanhasRequest([Guid.NewGuid(), Guid.NewGuid()]));

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ListaVazia_TemErroEmCampaignIds()
    {
        // Act
        var resultado = _validator.TestValidate(new OrdenarCampanhasRequest([]));

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.CampaignIds);
    }

    [Fact]
    public void Validate_ListaNula_TemErroEmCampaignIds()
    {
        // Act
        var resultado = _validator.TestValidate(new OrdenarCampanhasRequest(null!));

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.CampaignIds);
    }

    [Fact]
    public void Validate_IdRepetido_TemErroEmCampaignIds()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var resultado = _validator.TestValidate(new OrdenarCampanhasRequest([id, Guid.NewGuid(), id]));

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.CampaignIds);
    }
}
