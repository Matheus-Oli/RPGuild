using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RPGuild.Application.Campanhas;
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
        services.AddScoped<SistemasService>();
        services.AddScoped<GradeDeCampanhas>();
        services.AddScoped<AcessoACampanha>();
        services.AddScoped<CampanhaService>();
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<EnvioDeEmail>();
        services.AddScoped<INotificador, Notificador>();
        services.AddScoped<EventosDoSininho>();
        services.AddScoped<NotificacoesService>();
        services.AddScoped<LimpezaDeNotificacoes>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
