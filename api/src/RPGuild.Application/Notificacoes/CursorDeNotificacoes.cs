using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// Posição na lista de notificações (a última devolvida), em texto opaco para a URL. Paginação por chave
/// (created_at, id): é estável mesmo com notificações novas chegando no topo.
/// </summary>
public sealed record CursorDeNotificacoes(DateTimeOffset CriadoEm, Guid Id)
{
    private const char Separador = ':';

    public string Codificar() =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{CriadoEm.UtcTicks.ToString(CultureInfo.InvariantCulture)}{Separador}{Id}"));

    /// <summary>
    /// Devolve null se o texto não for um cursor válido.
    /// </summary>
    public static CursorDeNotificacoes? Decodificar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || !Base64Url.IsValid(texto))
        {
            return null;
        }

        string conteudo;
        try
        {
            conteudo = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(texto));
        }
        catch (FormatException)
        {
            return null;
        }

        var partes = conteudo.Split(Separador);
        if (partes.Length != 2
            || !long.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks > DateTimeOffset.MaxValue.UtcTicks
            || !Guid.TryParse(partes[1], out var id))
        {
            return null;
        }

        return new CursorDeNotificacoes(new DateTimeOffset(ticks, TimeSpan.Zero), id);
    }
}
