using Npgsql;

namespace RPGuild.IntegrationTests.Infra;

public static class TabernaDeTeste
{
    /// <summary>
    /// Cadastra e loga vários usuários novos (dados únicos).
    /// </summary>
    public static async Task<UsuarioLogado[]> CriarUsuariosLogadosAsync(this RPGuildApiFactory factory, int quantidade)
    {
        var logados = new UsuarioLogado[quantidade];
        for (var i = 0; i < quantidade; i++)
        {
            logados[i] = await factory.CriarUsuarioELogarAsync();
        }

        return logados;
    }

    public static async Task<(UsuarioLogado A, UsuarioLogado B)> CriarDoisUsuariosLogadosAsync(this RPGuildApiFactory factory)
    {
        var usuarios = await factory.CriarUsuariosLogadosAsync(2);
        return (usuarios[0], usuarios[1]);
    }

    /// <summary>
    /// Deixa os dois amigos direto no banco (pedido de <paramref name="a"/> aceito por <paramref name="b"/>),
    /// sem passar pelos endpoints. <paramref name="desde"/> é o "amigos desde" (responded_at do aceite).
    /// </summary>
    public static async Task TornarAmigosAsync(
        this RPGuildApiFactory factory, UsuarioLogado a, UsuarioLogado b, DateTimeOffset? desde = null)
    {
        var aceiteEm = desde ?? DateTimeOffset.UtcNow;

        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO friendships (id, requester_id, addressee_id, status, created_at, responded_at)
            VALUES (@id, @requester, @addressee, 'aceita', @criado, @aceite)
            """,
            conexao);
        comando.Parameters.AddWithValue("id", Guid.CreateVersion7());
        comando.Parameters.AddWithValue("requester", a.Id);
        comando.Parameters.AddWithValue("addressee", b.Id);
        comando.Parameters.AddWithValue("criado", aceiteEm.AddMinutes(-1));
        comando.Parameters.AddWithValue("aceite", aceiteEm);
        await comando.ExecuteNonQueryAsync();
    }
}
