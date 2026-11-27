namespace RPGuild.Application.Uploads;

/// <param name="Tipo">Para que é a imagem. "avatar", "capa" ou "banner".</param>
/// <param name="ContentType">Tipo do arquivo que o front vai subir (image/png...).</param>
public sealed record AssinarUploadRequest(string Tipo, string ContentType);
