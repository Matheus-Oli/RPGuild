using Microsoft.EntityFrameworkCore;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Persistence;

[Collection(IntegrationCollection.Name)]
public class MigrationsTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Migrate_BancoVazio_AplicaInitialCreate()
    {
        // Arrange
        await using var context = postgres.CriarDbContext(await postgres.CriarBancoVazioAsync());

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
        using var context = postgres.CriarDbContext();

        // Act
        var temMudancas = context.Database.HasPendingModelChanges();

        // Assert
        temMudancas.ShouldBeFalse();
    }
}
