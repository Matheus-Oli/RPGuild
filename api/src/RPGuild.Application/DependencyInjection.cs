using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RPGuild.Application.Contas;
using RPGuild.Application.Notificacoes;
using RPGuild.Application.Taberna;
using RPGuild.Application.Uploads;

namespace RPGuild.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<UploadService>();
        services.AddScoped<AmizadeService>();
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<EnvioDeEmail>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
