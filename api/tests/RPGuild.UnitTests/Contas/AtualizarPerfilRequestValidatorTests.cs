using FluentValidation.TestHelper;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class AtualizarPerfilRequestValidatorTests
{
    private static readonly AtualizarPerfilRequest Valido = new("Mestre da Mesa", "mestre_01", null);

    private readonly AtualizarPerfilRequestValidator _validator = new();

    [Fact]
    public void Validate_RequestValidoSemAvatar_NaoTemErros()
    {
        // Act
        var resultado = _validator.TestValidate(Valido);

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_RequestValidoComAvatar_NaoTemErros()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { AvatarPath = $"avatars/{Guid.NewGuid()}/{Guid.NewGuid()}.png" });

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

    [Fact]
    public void Validate_AvatarPathComMaisDe500Caracteres_TemErroEmAvatarPath()
    {
        // Act
        var resultado = _validator.TestValidate(Valido with { AvatarPath = "avatars/" + new string('a', 493) });

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.AvatarPath);
    }
}
