using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RPGuild.Application.Contas;
using RPGuild.Infrastructure.Contas;
using RPGuild.Infrastructure.Identity;
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

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AppDbContext>(options => AppDbContextOptions.Configure(options, connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                // UserName = e-mail; o formato já é validado no request.
                options.User.AllowedUserNameCharacters = string.Empty;

                options.Password.RequiredLength = RegrasDeConta.SenhaTamanhoMinimo;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IIdentityService, IdentityService>();

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
