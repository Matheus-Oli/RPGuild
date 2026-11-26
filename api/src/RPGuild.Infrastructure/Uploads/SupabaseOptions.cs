using Microsoft.Extensions.Configuration;

namespace RPGuild.Infrastructure.Uploads;

public sealed class SupabaseOptions
{
    public const string Secao = "Supabase";

    /// <summary>
    /// Base do projeto (ex.: https://xyz.supabase.co), sem /rest/v1 nem /storage/v1.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Secret key (sb_secret_...). Só no servidor: nunca vai para o front, para log ou para arquivo versionado.
    /// </summary>
    public string ServiceKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;

    public static SupabaseOptions Ler(IConfiguration configuration)
    {
        var options = configuration.GetSection(Secao).Get<SupabaseOptions>() ?? new SupabaseOptions();

        var faltando = new[]
            {
                (Chave: nameof(Url), Valor: options.Url),
                (Chave: nameof(ServiceKey), Valor: options.ServiceKey),
                (Chave: nameof(Bucket), Valor: options.Bucket),
            }
            .Where(c => string.IsNullOrWhiteSpace(c.Valor))
            .Select(c => $"{Secao}:{c.Chave}")
            .ToList();

        if (faltando.Count > 0)
        {
            throw new InvalidOperationException(
                $"Configuração do Supabase incompleta: {string.Join(", ", faltando)}. " +
                "Em desenvolvimento ela fica no .env da raiz (Supabase__Url, Supabase__ServiceKey, Supabase__Bucket); " +
                "em produção, nas variáveis de ambiente com os mesmos nomes.");
        }

        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"'{Secao}:{nameof(Url)}' precisa ser uma URL absoluta (ex.: https://xyz.supabase.co).");
        }

        return options;
    }
}
