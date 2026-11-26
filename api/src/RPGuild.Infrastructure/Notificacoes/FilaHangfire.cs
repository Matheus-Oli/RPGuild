using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RPGuild.Application.Notificacoes;

namespace RPGuild.Infrastructure.Notificacoes;

/// <summary>
/// Enfileira o envio num job do Hangfire. A mensagem fica gravada nas tabelas do Hangfire (schema "hangfire"):
/// e-mail com token nunca passa por aqui (veja <see cref="EnvioPorEmail.Direto"/>).
/// </summary>
public sealed class HangfireFilaDeEmail(IBackgroundJobClient jobs) : IFilaDeEmail
{
    public void Enfileirar(EmailMessage mensagem) =>
        jobs.Enqueue<EnvioDeEmailJob>(job => job.EnviarAsync(mensagem, CancellationToken.None));
}

/// <summary>
/// Job que envia o e-mail. Se o envio falhar, loga como o envio direto (assunto e destinatário mascarado, sem o corpo)
/// e deixa a exceção subir para o Hangfire tentar de novo (tentativas automáticas padrão).
/// </summary>
public sealed class EnvioDeEmailJob(IEmailSender emails, ILogger<EnvioDeEmailJob> logger)
{
    public async Task EnviarAsync(EmailMessage mensagem, CancellationToken cancellationToken)
    {
        try
        {
            await emails.EnviarAsync(mensagem, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, "Falha ao enviar o e-mail \"{Assunto}\" para {Para}; o Hangfire vai tentar de novo",
                mensagem.Assunto, MascaraDeEmail.Mascarar(mensagem.Para));
            throw;
        }
    }
}

public static class FilaHangfire
{
    public const string Schema = "hangfire";

    private const string ChaveHabilitado = "Hangfire:Habilitado";

    /// <summary>
    /// Ligado por padrão, menos no ambiente Testing, onde os testes usam uma fila falsa. "Hangfire:Habilitado" força.
    /// </summary>
    public static bool EstaHabilitado(IConfiguration configuration, bool ambienteDeTestes) =>
        configuration.GetValue<bool?>(ChaveHabilitado) ?? !ambienteDeTestes;

    /// <summary>
    /// Hangfire com storage no mesmo Postgres (schema "hangfire", criado pela própria lib) e o server que roda os jobs.
    /// </summary>
    public static IServiceCollection AddFilaHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddHangfire(hangfire => hangfire
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                postgres => postgres.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions { SchemaName = Schema, PrepareSchemaIfNecessary = true }));
        services.AddHangfireServer();

        services.AddTransient<EnvioDeEmailJob>();
        services.AddSingleton<IFilaDeEmail, HangfireFilaDeEmail>();

        return services;
    }
}
