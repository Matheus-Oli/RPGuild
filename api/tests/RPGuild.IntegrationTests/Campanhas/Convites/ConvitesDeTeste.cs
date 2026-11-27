using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using RPGuild.Infrastructure.Comum;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

/// <summary>
/// Preparação e chamadas de convites de campanha nos testes.
/// </summary>
public static class ConvitesDeTeste
{
    public const string MeusConvites = "/api/campaign-invites";

    public static string ConvitesDaCampanha(Guid campanhaId) => $"{CampanhasDeTeste.Campanha(campanhaId)}/invites";

    public static string Convite(Guid conviteId) => $"{MeusConvites}/{conviteId}";

    public static string Aceitar(Guid conviteId) => $"{Convite(conviteId)}/accept";

    public static string Recusar(Guid conviteId) => $"{Convite(conviteId)}/decline";

    /// <summary>
    /// Cadastra um mestre e N usuários, todos amigos dele (direto no banco).
    /// </summary>
    public static async Task<(UsuarioLogado Mestre, UsuarioLogado[] Amigos)> CriarMestreComAmigosAsync(
        this RPGuildApiFactory factory, int quantidade)
    {
        var usuarios = await factory.CriarUsuariosLogadosAsync(quantidade + 1);
        foreach (var amigo in usuarios[1..])
        {
            await factory.TornarAmigosAsync(usuarios[0], amigo);
        }

        return (usuarios[0], usuarios[1..]);
    }

    /// <summary>
    /// Grava um convite direto no banco, com o status e a data controláveis, e devolve o id.
    /// </summary>
    public static async Task<Guid> ConvidarNoBancoAsync(
        this RPGuildApiFactory factory,
        Guid campanhaId,
        UsuarioLogado mestre,
        UsuarioLogado convidado,
        string status = "pendente",
        DateTimeOffset? criadoEm = null)
    {
        var quando = RelogioEmMicrossegundos.Truncar(criadoEm ?? DateTimeOffset.UtcNow);
        var id = Guid.CreateVersion7(quando);

        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO campaign_invites (id, campaign_id, invited_by, user_id, status, created_at, responded_at)
            VALUES (@id, @campanha, @mestre, @convidado, @status, @criado, @respondido)
            """,
            conexao);
        comando.Parameters.AddWithValue("id", id);
        comando.Parameters.AddWithValue("campanha", campanhaId);
        comando.Parameters.AddWithValue("mestre", mestre.Id);
        comando.Parameters.AddWithValue("convidado", convidado.Id);
        comando.Parameters.AddWithValue("status", status);
        comando.Parameters.AddWithValue("criado", quando);
        comando.Parameters.AddWithValue("respondido", status == "pendente" ? DBNull.Value : quando);
        await comando.ExecuteNonQueryAsync();

        return id;
    }

    /// <summary>
    /// O status do convite como está no banco, ou null se a linha não existe.
    /// </summary>
    public static async Task<string?> StatusNoBancoAsync(this RPGuildApiFactory factory, Guid conviteId)
    {
        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT status FROM campaign_invites WHERE id = @id", conexao);
        comando.Parameters.AddWithValue("id", conviteId);
        return (string?)await comando.ExecuteScalarAsync();
    }

    /// <summary>
    /// Quantos convites a campanha tem no banco, em qualquer status.
    /// </summary>
    public static async Task<long> ConvitesNoBancoAsync(this RPGuildApiFactory factory, Guid campanhaId)
    {
        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM campaign_invites WHERE campaign_id = @campanha", conexao);
        comando.Parameters.AddWithValue("campanha", campanhaId);
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    public static async Task<HttpResponseMessage> ConvidarAsync(
        this RPGuildApiFactory factory, UsuarioLogado mestre, Guid campanhaId, params Guid[] userIds)
    {
        using var cliente = factory.CriarClienteAutenticado(mestre.AccessToken);
        return await cliente.PostAsJsonAsync(ConvitesDaCampanha(campanhaId), new { userIds });
    }

    /// <summary>
    /// Convida pela API e devolve os convites criados (falha o teste se não for 201).
    /// </summary>
    public static async Task<List<ConviteDaCampanhaResponse>> ConvidarComSucessoAsync(
        this RPGuildApiFactory factory, UsuarioLogado mestre, Guid campanhaId, params UsuarioLogado[] convidados)
    {
        var resposta = await factory.ConvidarAsync(mestre, campanhaId, [.. convidados.Select(c => c.Id)]);
        resposta.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<List<ConviteDaCampanhaResponse>>())!;
    }

    /// <summary>
    /// Os convidados recusados que vieram no ProblemDetails (extensão "convidados").
    /// </summary>
    public static async Task<List<ConvidadoInvalidoResponse>> ConvidadosInvalidosAsync(this HttpResponseMessage resposta)
    {
        var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        return problema.GetProperty("convidados").Deserialize<List<ConvidadoInvalidoResponse>>(JsonSerializerOptions.Web)!;
    }

    public static Task<HttpResponseMessage> AceitarAsync(this RPGuildApiFactory factory, UsuarioLogado convidado, Guid conviteId) =>
        factory.PostAsync(convidado, Aceitar(conviteId));

    public static Task<HttpResponseMessage> RecusarAsync(this RPGuildApiFactory factory, UsuarioLogado convidado, Guid conviteId) =>
        factory.PostAsync(convidado, Recusar(conviteId));

    public static Task<HttpResponseMessage> CancelarAsync(this RPGuildApiFactory factory, UsuarioLogado mestre, Guid conviteId) =>
        factory.DeleteAsync(mestre, Convite(conviteId));
}
