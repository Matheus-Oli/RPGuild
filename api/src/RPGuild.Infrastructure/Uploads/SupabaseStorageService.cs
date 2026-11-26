using System.Net.Http.Json;
using RPGuild.Application.Uploads;

namespace RPGuild.Infrastructure.Uploads;

/// <summary>
/// Supabase Storage pela API REST, sem SDK. O HttpClient já vem com a base do Storage e a secret key no header
/// "apikey" (veja <see cref="Configurar"/>). Sem teste automatizado: validado manualmente contra o projeto real.
/// </summary>
public sealed class SupabaseStorageService(HttpClient http, SupabaseOptions options) : IStorageService
{
    // As chaves novas (sb_secret_...) não são JWT: vão só no "apikey". No "Authorization: Bearer" são rejeitadas.
    private const string HeaderApiKey = "apikey";

    public static void Configurar(HttpClient http, SupabaseOptions options)
    {
        http.BaseAddress = new Uri($"{BaseDoStorage(options)}/");
        http.DefaultRequestHeaders.Add(HeaderApiKey, options.ServiceKey);
    }

    public async Task<string> CriarUrlDeUploadAsync(string path, CancellationToken ct)
    {
        using var resposta = await http.PostAsJsonAsync($"object/upload/sign/{CaminhoNoBucket(path)}", new { }, ct);
        await GarantirSucessoAsync(resposta, "criar a URL de upload", ct);

        // A url vem relativa à base do Storage: /object/upload/sign/{bucket}/{path}?token=...
        var assinada = await resposta.Content.ReadFromJsonAsync<UrlAssinada>(ct);
        if (string.IsNullOrWhiteSpace(assinada?.Url))
        {
            throw new HttpRequestException("O Supabase Storage não devolveu a URL de upload.");
        }

        return $"{BaseDoStorage(options)}{assinada.Url}";
    }

    public string ObterUrlPublica(string path) => $"{BaseDoStorage(options)}/object/public/{CaminhoNoBucket(path)}";

    public async Task ApagarAsync(string path, CancellationToken ct)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Delete, $"object/{Uri.EscapeDataString(options.Bucket)}")
        {
            Content = JsonContent.Create(new { prefixes = new[] { path } }),
        };
        using var resposta = await http.SendAsync(requisicao, ct);
        await GarantirSucessoAsync(resposta, "apagar o arquivo", ct);
    }

    private static string BaseDoStorage(SupabaseOptions options) => $"{options.Url.TrimEnd('/')}/storage/v1";

    private string CaminhoNoBucket(string path) =>
        string.Join('/', new[] { options.Bucket }.Concat(path.Split('/')).Select(Uri.EscapeDataString));

    private static async Task GarantirSucessoAsync(HttpResponseMessage resposta, string acao, CancellationToken ct)
    {
        if (!resposta.IsSuccessStatusCode)
        {
            // O corpo de erro do Storage só tem a mensagem (sem a chave): ajuda a diagnosticar no log.
            var corpo = await resposta.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Falha ao {acao} no Supabase Storage: {(int)resposta.StatusCode} {corpo}", null, resposta.StatusCode);
        }
    }

    private sealed record UrlAssinada(string? Url);
}
