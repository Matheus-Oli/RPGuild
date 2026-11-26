namespace RPGuild.Application.Comum;

/// <summary>
/// Endereço do front, para os links que vão nos e-mails.
/// </summary>
public sealed class FrontOptions
{
    public const string Secao = "Front";

    public string BaseUrl { get; set; } = string.Empty;
}
