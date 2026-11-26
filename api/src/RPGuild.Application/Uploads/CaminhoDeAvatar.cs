namespace RPGuild.Application.Uploads;

/// <summary>
/// Onde fica o avatar no bucket: avatars/{userId}/{guid}.{ext}. Quem escolhe o path é a API, nunca o front.
/// </summary>
public static class CaminhoDeAvatar
{
    private const string Pasta = "avatars";

    public static string Gerar(Guid userId, string contentType) =>
        $"{Prefixo(userId)}{Guid.NewGuid()}.{TiposDeImagem.Extensao(contentType)}";

    /// <summary>
    /// Se o path está dentro da pasta do próprio usuário, sem "..", "." ou segmentos vazios que escapem dela.
    /// </summary>
    public static bool PertenceAo(Guid userId, string? path)
    {
        var prefixo = Prefixo(userId);
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(prefixo, StringComparison.Ordinal))
        {
            return false;
        }

        var resto = path[prefixo.Length..];
        return resto.Length > 0
            && !resto.Contains('\\')
            && resto.Split('/').All(segmento => segmento is not ("" or "." or ".."));
    }

    private static string Prefixo(Guid userId) => $"{Pasta}/{userId}/";
}
