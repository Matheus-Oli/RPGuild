using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RPGuild.Application.Contas;

namespace RPGuild.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
