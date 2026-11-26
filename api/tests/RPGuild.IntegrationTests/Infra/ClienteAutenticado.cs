namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Chamadas sem corpo em nome de um usuário logado (ações como aceitar, ler ou apagar).
/// </summary>
public static class ClienteAutenticado
{
    public static async Task<HttpResponseMessage> PostAsync(this RPGuildApiFactory factory, UsuarioLogado logado, string url)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return await cliente.PostAsync(url, content: null);
    }

    public static async Task<HttpResponseMessage> DeleteAsync(this RPGuildApiFactory factory, UsuarioLogado logado, string url)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return await cliente.DeleteAsync(url);
    }
}
