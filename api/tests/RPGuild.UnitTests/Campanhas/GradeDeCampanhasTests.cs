using RPGuild.Application.Campanhas;

namespace RPGuild.UnitTests.Campanhas;

public class GradeDeCampanhasTests
{
    private static readonly Guid Usuario = Guid.NewGuid();
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid C = Guid.NewGuid();

    private readonly GradeDeCampanhasRepositoryFalso _repositorio = new();
    private readonly GradeDeCampanhas _grade;

    public GradeDeCampanhasTests() => _grade = new GradeDeCampanhas(_repositorio);

    [Fact]
    public async Task ColocarNoInicioAsync_GradeComCampanhas_ANovaEntraNoInicioEORestoDesce()
    {
        // Arrange
        _repositorio.Ordens[Usuario] = [A, B];

        // Act
        await _grade.ColocarNoInicioAsync(Usuario, C, CancellationToken.None);

        // Assert
        _repositorio.Ordens[Usuario].ShouldBe([C, A, B]);
    }

    [Fact]
    public async Task ColocarNoInicioAsync_GradeVazia_FicaSoANova()
    {
        // Act
        await _grade.ColocarNoInicioAsync(Usuario, A, CancellationToken.None);

        // Assert
        _repositorio.Ordens[Usuario].ShouldBe([A]);
    }

    [Fact]
    public async Task ReordenarAsync_ExatamenteAsCampanhasDoUsuario_GravaANovaOrdem()
    {
        // Arrange
        DarCampanhas(A, B, C);

        // Act
        var resultado = await _grade.ReordenarAsync(Usuario, [C, A, B], CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        _repositorio.Ordens[Usuario].ShouldBe([C, A, B]);
    }

    [Fact]
    public async Task ReordenarAsync_CampanhaAMais_RetornaErroSemMudarAOrdem()
    {
        // Arrange
        DarCampanhas(A, B);

        // Act
        var resultado = await _grade.ReordenarAsync(Usuario, [B, A, C], CancellationToken.None);

        // Assert
        resultado.Erro.ShouldBe(ErrosDeCampanha.OrdemInvalida);
        _repositorio.Ordens[Usuario].ShouldBe([A, B]);
    }

    [Fact]
    public async Task ReordenarAsync_CampanhaAMenos_RetornaErroSemMudarAOrdem()
    {
        // Arrange
        DarCampanhas(A, B, C);

        // Act
        var resultado = await _grade.ReordenarAsync(Usuario, [C, A], CancellationToken.None);

        // Assert
        resultado.Erro.ShouldBe(ErrosDeCampanha.OrdemInvalida);
        _repositorio.Ordens[Usuario].ShouldBe([A, B, C]);
    }

    [Fact]
    public async Task ReordenarAsync_CampanhaRepetida_RetornaErroSemMudarAOrdem()
    {
        // Arrange
        DarCampanhas(A, B);

        // Act
        var resultado = await _grade.ReordenarAsync(Usuario, [A, B, A], CancellationToken.None);

        // Assert
        resultado.Erro.ShouldBe(ErrosDeCampanha.OrdemInvalida);
        _repositorio.Ordens[Usuario].ShouldBe([A, B]);
    }

    private void DarCampanhas(params Guid[] campanhas)
    {
        _repositorio.Campanhas[Usuario] = [.. campanhas];
        _repositorio.Ordens[Usuario] = [.. campanhas];
    }
}
