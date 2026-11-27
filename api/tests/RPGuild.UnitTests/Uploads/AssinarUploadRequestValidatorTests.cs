using FluentValidation.TestHelper;
using RPGuild.Application.Uploads;

namespace RPGuild.UnitTests.Uploads;

public class AssinarUploadRequestValidatorTests
{
    private static readonly AssinarUploadRequest Valido = new("avatar", "image/png");

    private readonly AssinarUploadRequestValidator _validator = new();

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void Validate_AvatarComContentTypePermitido_NaoTemErros(string contentType)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { ContentType = contentType });

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("capa")]
    [InlineData("banner")]
    public void Validate_CapaOuBannerComContentTypePermitido_NaoTemErros(string tipo)
    {
        // Act
        var resultado = _validator.TestValidate(new AssinarUploadRequest(tipo, "image/webp"));

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("retrato")]
    [InlineData("Capa")]
    [InlineData("qualquer")]
    public void Validate_TipoNaoSuportado_TemErroEmTipoDizendoQueAindaNaoESuportado(string tipo)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Tipo = tipo });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Tipo).WithErrorMessage($"O tipo \"{tipo}\" ainda não é suportado.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_TipoVazio_TemErroEmTipo(string? tipo)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { Tipo = tipo! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Tipo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("image/svg+xml")]
    [InlineData("image/bmp")]
    [InlineData("application/pdf")]
    [InlineData("text/html")]
    public void Validate_ContentTypeForaDaLista_TemErroEmContentType(string? contentType)
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { ContentType = contentType! });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.ContentType);
    }
}
