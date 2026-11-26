namespace RPGuild.Application.Uploads;

/// <summary>
/// Onde fica o avatar no bucket: avatars/{userId}/{guid}.{ext}. Quem escolhe o path é a API, nunca o front.
/// </summary>
public static class CaminhoDeAvatar
{
    private const string Pasta = "avatars";

    public static string Gerar(Guid userId, string contentType) =>
        $"{Pasta}/{userId}/{Guid.NewGuid()}.{TiposDeImagem.Extensao(contentType)}";
}
