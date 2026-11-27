using FluentValidation.TestHelper;
using RPGuild.Application.Campanhas;

namespace RPGuild.UnitTests.Campanhas.Convites;

public class ConvidarRequestValidatorTests
{
    private readonly ConvidarRequestValidator _validator = new();
    private readonly CriarCampanhaRequestValidator _criar = new();

    [Fact]
    public void Validate_UmOuMaisUsuarios_NaoTemErros()
    {
        // Act
        var resultado = _validator.TestValidate(new ConvidarRequest([Guid.NewGuid(), Guid.NewGuid()]));

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ListaVazia_TemErroEmUserIds()
    {
        // Act
        var resultado = _validator.TestValidate(new ConvidarRequest([]));

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.UserIds);
    }

    [Fact]
    public void Validate_UsuarioRepetido_TemErroEmUserIds()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var resultado = _validator.TestValidate(new ConvidarRequest([id, Guid.NewGuid(), id]));

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.UserIds).WithErrorMessage("A lista tem usuário repetido.");
    }

    [Theory]
    [MemberData(nameof(ConvidadosValidos))]
    public void ValidateCriar_ConvidadosAusentesVaziosOuDistintos_NaoTemErros(Guid[]? convidados)
    {
        // Act
        var resultado = _criar.TestValidate(new CriarCampanhaRequest("Campanha", null, "dnd2024", null, null, convidados));

        // Assert
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidateCriar_ConvidadoRepetido_TemErroEmConvidados()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var resultado = _criar.TestValidate(new CriarCampanhaRequest("Campanha", null, "dnd2024", null, null, [id, id]));

        // Assert
        resultado.ShouldHaveValidationErrorFor(r => r.Convidados).WithErrorMessage("A lista tem usuário repetido.");
    }

    public static TheoryData<Guid[]?> ConvidadosValidos() => [null, [], [Guid.NewGuid(), Guid.NewGuid()]];
}
