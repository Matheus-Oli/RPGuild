using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseHealthCheck = "database";

    // O /health precisa responder rápido mesmo com o banco fora; a conexão da aplicação segue com o timeout padrão.
    private static readonly TimeSpan DatabaseHealthCheckTimeout = TimeSpan.FromMilliseconds(1500);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'ConnectionStrings:Default' não foi configurada. " +
                "Em desenvolvimento ela fica no appsettings.Development.json; em produção, na variável de ambiente ConnectionStrings__Default.");
        }

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(DatabaseHealthCheck);

        services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations.Where(r => r.Name == DatabaseHealthCheck))
            {
                registration.Timeout = DatabaseHealthCheckTimeout;
            }
        });

        return services;
    }
}
