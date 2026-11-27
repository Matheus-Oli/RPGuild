namespace RPGuild.Application.Uploads;

public static class TiposDeUpload
{
    public const string Avatar = "avatar";
    public const string Capa = "capa";
    public const string Banner = "banner";

    // TODO(fichas): retrato.
    public static bool EhSuportado(string? tipo) => tipo is Avatar or Capa or Banner;

    /// <summary>
    /// O path de um upload novo, na pasta do usuário: avatars/ para o avatar, campaigns/ para capa e banner.
    /// </summary>
    public static string GerarPath(string tipo, Guid userId, string contentType) => tipo switch
    {
        Avatar => CaminhoDeAvatar.Gerar(userId, contentType),
        Capa or Banner => CaminhoDeImagemDeCampanha.Gerar(userId, contentType),
        _ => throw new ArgumentException($"Tipo de upload não suportado: {tipo}.", nameof(tipo)),
    };
}
