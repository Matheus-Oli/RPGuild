namespace RPGuild.Application.Uploads;

/// <summary>
/// Onde fica o avatar no bucket: avatars/{userId}/{guid}.{ext}. Quem escolhe o path é a API, nunca o front.
/// </summary>
public static class CaminhoDeAvatar
{
    private static readonly PastaDoUsuario Pasta = new("avatars");

    public static string Gerar(Guid userId, string contentType) => Pasta.Gerar(userId, contentType);

    /// <summary>
    /// Se o path está dentro da pasta do próprio usuário, sem "..", "." ou segmentos vazios que escapem dela.
    /// </summary>
    public static bool PertenceAo(Guid userId, string? path) => Pasta.PertenceAo(userId, path);
}

/// <summary>
/// Onde ficam a capa e o banner de campanha no bucket: campaigns/{userId}/{guid}.{ext}. A pasta é de quem sobe,
/// porque a campanha ainda não existe na hora do upload.
/// </summary>
public static class CaminhoDeImagemDeCampanha
{
    private static readonly PastaDoUsuario Pasta = new("campaigns");

    public static string Gerar(Guid userId, string contentType) => Pasta.Gerar(userId, contentType);

    public static bool PertenceAo(Guid userId, string? path) => Pasta.PertenceAo(userId, path);
}

/// <summary>
/// Uma pasta por usuário dentro de uma pasta do bucket ({pasta}/{userId}/{guid}.{ext}).
/// </summary>
internal sealed class PastaDoUsuario(string pasta)
{
    public string Gerar(Guid userId, string contentType) =>
        $"{Prefixo(userId)}{Guid.NewGuid()}.{TiposDeImagem.Extensao(contentType)}";

    public bool PertenceAo(Guid userId, string? path)
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

    private string Prefixo(Guid userId) => $"{pasta}/{userId}/";
}
