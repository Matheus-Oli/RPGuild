using Microsoft.EntityFrameworkCore;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.IntegrationTests.Persistence;

public class ConvencaoNomesTests
{
    // Só monta o modelo; nenhuma conexão é aberta.
    private const string ConnectionStringQualquer = "Host=localhost;Database=sonda;Username=sonda;Password=sonda";

    [Fact]
    public void Modelo_EntidadeSonda_TabelaEmSnakeCase()
    {
        // Arrange
        using var context = CriarContext();

        // Act
        var tabela = context.Model.FindEntityType(typeof(EntidadeSonda))!.GetTableName();

        // Assert
        tabela.ShouldBe("entidade_sondas");
    }

    [Theory]
    [InlineData(nameof(EntidadeSonda.Id), "id")]
    [InlineData(nameof(EntidadeSonda.NomeCompleto), "nome_completo")]
    [InlineData(nameof(EntidadeSonda.CriadoEm), "criado_em")]
    public void Modelo_PropriedadeDaEntidadeSonda_ColunaEmSnakeCase(string propriedade, string colunaEsperada)
    {
        // Arrange
        using var context = CriarContext();

        // Act
        var coluna = context.Model.FindEntityType(typeof(EntidadeSonda))!.FindProperty(propriedade)!.GetColumnName();

        // Assert
        coluna.ShouldBe(colunaEsperada);
    }

    private static SondaDbContext CriarContext()
    {
        var builder = new DbContextOptionsBuilder<SondaDbContext>();
        AppDbContextOptions.Configure(builder, ConnectionStringQualquer);
        return new SondaDbContext(builder.Options);
    }

    private sealed class SondaDbContext(DbContextOptions<SondaDbContext> options) : DbContext(options)
    {
        public DbSet<EntidadeSonda> EntidadeSondas => Set<EntidadeSonda>();
    }
}

public class EntidadeSonda
{
    public Guid Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public DateTimeOffset CriadoEm { get; set; }
}
