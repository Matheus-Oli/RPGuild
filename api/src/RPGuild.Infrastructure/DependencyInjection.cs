using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.Infrastructure;

public static class DependencyInjection
{
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
            .AddDbContextCheck<AppDbContext>();

        return services;
    }
}
