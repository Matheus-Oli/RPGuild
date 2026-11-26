namespace RPGuild.Application.Uploads;

/// <summary>
/// Imagens aceitas no upload e a extensão do arquivo no storage. A mesma lista está configurada no bucket.
/// </summary>
public static class TiposDeImagem
{
    private static readonly Dictionary<string, string> Extensoes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
        ["image/gif"] = "gif",
    };

    public static IReadOnlyCollection<string> Permitidos => Extensoes.Keys;

    public static bool EhPermitido(string? contentType) => contentType is not null && Extensoes.ContainsKey(contentType);
}
