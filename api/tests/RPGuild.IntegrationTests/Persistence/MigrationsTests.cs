using Microsoft.EntityFrameworkCore;
using RPGuild.Infrastructure.Persistence;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Persistence;

[Collection(IntegrationCollection.Name)]
public class MigrationsTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Migrate_BancoVazio_AplicaInitialCreate()
    {
        // Arrange
        await using var context = CriarContext(await postgres.CriarBancoVazioAsync());

        // Act
        await context.Database.MigrateAsync();

        // Assert
        var aplicadas = await context.Database.GetAppliedMigrationsAsync();
        aplicadas.ShouldContain(migration => migration.EndsWith("_InitialCreate"));
    }

    [Fact]
    public void Modelo_SemMudancasPendentes_NaoPrecisaDeNovaMigration()
    {
        // Arrange
        using var context = CriarContext(postgres.ConnectionString);

        // Act
        var temMudancas = context.Database.HasPendingModelChanges();

        // Assert
        temMudancas.ShouldBeFalse();
    }

    private static AppDbContext CriarContext(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContextOptions.Configure(builder, connectionString);
        return new AppDbContext(builder.Options);
    }
}
