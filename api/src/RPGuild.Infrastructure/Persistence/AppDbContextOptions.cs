using Microsoft.EntityFrameworkCore;

namespace RPGuild.Infrastructure.Persistence;

/// <summary>
/// Configuração do provider usada pelo <see cref="AppDbContext"/>; os testes reaproveitam para validar as convenções.
/// </summary>
public static class AppDbContextOptions
{
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        return builder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();
    }
}
