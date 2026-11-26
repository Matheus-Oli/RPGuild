using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

/// <summary>
/// Chamadas da Taberna em nome de um usuário logado.
/// </summary>
public static class ClienteDaTaberna
{
    public const string Pedidos = "/api/friends/requests";
    public const string Amigos = "/api/friends";

    public static string Pedido(Guid pedidoId) => $"{Pedidos}/{pedidoId}";

    public static string Aceitar(Guid pedidoId) => $"{Pedido(pedidoId)}/accept";

    public static string Recusar(Guid pedidoId) => $"{Pedido(pedidoId)}/decline";

    public static string Amigo(Guid userId) => $"{Amigos}/{userId}";

    public static async Task<HttpResponseMessage> PedirAsync(this RPGuildApiFactory factory, UsuarioLogado de, string emailOuNick)
    {
        using var cliente = factory.CriarClienteAutenticado(de.AccessToken);
        return await cliente.PostAsJsonAsync(Pedidos, new { emailOuNick });
    }

    /// <summary>
    /// Pede amizade pelo nick e devolve o pedido criado (falha o teste se não for 201).
    /// </summary>
    public static async Task<PedidoEnviadoResponse> PedirComSucessoAsync(this RPGuildApiFactory factory, UsuarioLogado de, UsuarioLogado para)
    {
        var resposta = await factory.PedirAsync(de, para.Dados.Nick);
        resposta.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<PedidoEnviadoResponse>())!;
    }

    public static async Task<PedidosResponse> PedidosDeAsync(this RPGuildApiFactory factory, UsuarioLogado logado)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return (await cliente.GetFromJsonAsync<PedidosResponse>(Pedidos))!;
    }

    public static async Task<List<AmigoResponse>> AmigosDeAsync(this RPGuildApiFactory factory, UsuarioLogado logado)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return (await cliente.GetFromJsonAsync<List<AmigoResponse>>(Amigos))!;
    }
}
