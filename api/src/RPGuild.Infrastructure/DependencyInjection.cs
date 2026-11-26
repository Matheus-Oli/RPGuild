using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Application.Notificacoes;
using RPGuild.Application.Taberna;
using RPGuild.Application.Uploads;
using RPGuild.Infrastructure.Comum;
using RPGuild.Infrastructure.Contas;
using RPGuild.Infrastructure.Identity;
using RPGuild.Infrastructure.Notificacoes;
using RPGuild.Infrastructure.Persistence;
using RPGuild.Infrastructure.Taberna;
using RPGuild.Infrastructure.Uploads;

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

        // Agora na precisão do Postgres: o instante devolvido por quem grava é o mesmo que uma leitura devolve depois.
        services.TryAddSingleton<TimeProvider>(new RelogioEmMicrossegundos(TimeProvider.System));

        services.AddDbContext<AppDbContext>(options => AppDbContextOptions.Configure(options, connectionString));

        // Chaves no banco: os tokens (ex.: redefinição de senha) continuam válidos entre reinícios e deploys.
        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .SetApplicationName("RPGuild");

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
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = RegrasDeConta.ValidadeDoTokenDeRedefinicao);

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAmizadeRepository, AmizadeRepository>();
        services.AddScoped<INotificacaoRepository, NotificacaoRepository>();

        var jwt = JwtOptions.Ler(configuration);
        services.AddSingleton(jwt);
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddSingleton(LerFrontOptions(configuration));
        services.AddSingleton(EmailOptions.Ler(configuration));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        var supabase = SupabaseOptions.Ler(configuration);
        services.AddSingleton(supabase);
        services.AddHttpClient<IStorageService, SupabaseStorageService>(http => SupabaseStorageService.Configurar(http, supabase))
            // A secret key vai num header: nada de header no log, nem em Trace.
            .RedactLoggedHeaders(_ => true);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Mantém os nomes do JWT (sub, email, nick, name) em vez dos URIs do .NET.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = JwtTokenService.ChaveDeAssinatura(jwt),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    // O padrão aceita 5 min além da validade; o access token já é curto e o refresh cobre a renovação.
                    ClockSkew = TimeSpan.Zero,
                };
            });

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

    private static FrontOptions LerFrontOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection(FrontOptions.Secao).Get<FrontOptions>() ?? new FrontOptions();
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                "'Front:BaseUrl' precisa ser uma URL absoluta (ex.: http://localhost:5173). " +
                "Em desenvolvimento ela fica no appsettings.Development.json; em produção, na variável de ambiente Front__BaseUrl.");
        }

        return options;
    }
}
