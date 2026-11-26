using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes;

public sealed record NotificacaoResponse(Guid Id, string Tipo, JsonElement Dados, bool Lida, DateTimeOffset CriadoEm);

public sealed record PaginaDeNotificacoes(List<NotificacaoResponse> Itens, string? ProximoCursor);

public sealed record ContagemResponse(int Total);

/// <summary>
/// Chamadas de /api/notifications em nome de um usuário logado.
/// </summary>
public static class ClienteDeNotificacoes
{
    public const string Url = "/api/notifications";

    public static async Task<HttpResponseMessage> ListarAsync(this RPGuildApiFactory factory, UsuarioLogado logado, string query = "")
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return await cliente.GetAsync($"{Url}{query}");
    }

    public static async Task<PaginaDeNotificacoes> PaginaAsync(this RPGuildApiFactory factory, UsuarioLogado logado, string query = "")
    {
        var resposta = await factory.ListarAsync(logado, query);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<PaginaDeNotificacoes>())!;
    }

    public static async Task<int> NaoLidasAsync(this RPGuildApiFactory factory, UsuarioLogado logado)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return (await cliente.GetFromJsonAsync<ContagemResponse>($"{Url}/unread-count"))!.Total;
    }

    public static string Ler(Guid id) => $"{Url}/{id}/read";

    public const string LerTodas = Url + "/read-all";

    public static string Notificacao(Guid id) => $"{Url}/{id}";
}
