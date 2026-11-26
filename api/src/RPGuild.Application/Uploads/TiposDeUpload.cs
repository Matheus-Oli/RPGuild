namespace RPGuild.Application.Uploads;

public static class TiposDeUpload
{
    public const string Avatar = "avatar";

    // TODO(campanhas, fichas): capa, banner e retrato.
    public static bool EhSuportado(string? tipo) => tipo == Avatar;
}
