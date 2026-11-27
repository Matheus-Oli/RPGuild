using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

/// <summary>
/// Conexões no hub de campanha nos testes.
/// </summary>
public static class TempoRealDeTeste
{
    public static Task<ConexaoNoHub> AbrirNoHubDaCampanhaAsync(this RPGuildApiFactory factory, UsuarioLogado logado) =>
        ConexaoNoHub.AbrirAsync(factory, logado.AccessToken, ConexaoNoHub.RotaDaCampanha);

    /// <summary>
    /// Abre a conexão no hub de campanha e entra no grupo da campanha (lança se o hub recusar).
    /// </summary>
    public static async Task<ConexaoNoHub> EntrarNaCampanhaAsync(this RPGuildApiFactory factory, UsuarioLogado logado, Guid campanhaId)
    {
        var conexao = await factory.AbrirNoHubDaCampanhaAsync(logado);
        try
        {
            await conexao.EntrarNaCampanhaAsync(campanhaId);
        }
        catch
        {
            await conexao.DisposeAsync();
            throw;
        }

        return conexao;
    }
}
