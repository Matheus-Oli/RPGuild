using System.Net;
using System.Net.Http.Json;
using Npgsql;
using RPGuild.Infrastructure.Comum;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas;

/// <summary>
/// Preparação de campanhas nos testes.
/// </summary>
public static class CampanhasDeTeste
{
    public const string Url = "/api/campaigns";
    public const string SistemaAtivo = "dnd2024";

    public static async Task<HttpResponseMessage> CriarAsync(this RPGuildApiFactory factory, UsuarioLogado mestre, object corpo)
    {
        using var cliente = factory.CriarClienteAutenticado(mestre.AccessToken);
        return await cliente.PostAsJsonAsync(Url, corpo);
    }

    /// <summary>
    /// Cria uma campanha pela API (D&amp;D 2024) e devolve o detalhe (falha o teste se não for 201).
    /// </summary>
    public static async Task<DetalheResponse> CriarCampanhaAsync(
        this RPGuildApiFactory factory, UsuarioLogado mestre, string? nome = null, string? capaPath = null, string? bannerPath = null)
    {
        var resposta = await factory.CriarAsync(mestre, new
        {
            nome = nome ?? $"Campanha {Guid.NewGuid():N}"[..20],
            descricao = "Uma aventura de teste.",
            sistema = SistemaAtivo,
            capaPath,
            bannerPath,
        });
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!;
    }

    /// <summary>
    /// Cadastra um usuário e cria N campanhas para ele, nesta ordem. Devolve os ids na ordem de criação.
    /// </summary>
    public static async Task<(UsuarioLogado Mestre, List<Guid> Campanhas)> CriarUsuarioComCampanhasAsync(
        this RPGuildApiFactory factory, int quantidade)
    {
        var mestre = await factory.CriarUsuarioELogarAsync();
        var campanhas = new List<Guid>();
        for (var i = 0; i < quantidade; i++)
        {
            campanhas.Add((await factory.CriarCampanhaAsync(mestre)).Id);
        }

        return (mestre, campanhas);
    }

    /// <summary>
    /// Põe o usuário como jogador direto no banco (o convite só nasce na parte 2), com a campanha no início da
    /// grade dele, como o aceite do convite vai fazer.
    /// </summary>
    public static async Task AdicionarJogadorAsync(
        this RPGuildApiFactory factory, Guid campanhaId, UsuarioLogado jogador, DateTimeOffset? entrouEm = null)
    {
        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var transacao = await conexao.BeginTransactionAsync();

        await using (var membro = new NpgsqlCommand(
            "INSERT INTO campaign_members (campaign_id, user_id, joined_at) VALUES (@campanha, @user, @entrou)", conexao, transacao))
        {
            membro.Parameters.AddWithValue("campanha", campanhaId);
            membro.Parameters.AddWithValue("user", jogador.Id);
            membro.Parameters.AddWithValue("entrou", RelogioEmMicrossegundos.Truncar(entrouEm ?? DateTimeOffset.UtcNow));
            await membro.ExecuteNonQueryAsync();
        }

        await using (var ordem = new NpgsqlCommand(
            """
            UPDATE campaign_user_order SET posicao = posicao + 1 WHERE user_id = @user;
            INSERT INTO campaign_user_order (user_id, campaign_id, posicao) VALUES (@user, @campanha, 0);
            """,
            conexao,
            transacao))
        {
            ordem.Parameters.AddWithValue("campanha", campanhaId);
            ordem.Parameters.AddWithValue("user", jogador.Id);
            await ordem.ExecuteNonQueryAsync();
        }

        await transacao.CommitAsync();
    }

    /// <summary>
    /// A grade do usuário como está no banco (campaign_user_order), da primeira posição para a última.
    /// </summary>
    public static async Task<List<Guid>> OrdemNoBancoAsync(this RPGuildApiFactory factory, Guid userId)
    {
        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT campaign_id FROM campaign_user_order WHERE user_id = @user ORDER BY posicao", conexao);
        comando.Parameters.AddWithValue("user", userId);

        var ordem = new List<Guid>();
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            ordem.Add(leitor.GetGuid(0));
        }

        return ordem;
    }

    /// <summary>
    /// Quantas campanhas existem no banco com este mestre.
    /// </summary>
    public static async Task<long> CampanhasNoBancoAsync(this RPGuildApiFactory factory, Guid mestreId)
    {
        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM campaigns WHERE owner_id = @mestre", conexao);
        comando.Parameters.AddWithValue("mestre", mestreId);
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    public static string Campanha(Guid campanhaId) => $"{Url}/{campanhaId}";

    public static string Membros(Guid campanhaId) => $"{Campanha(campanhaId)}/members";

    public static async Task<HttpResponseMessage> GetAsync(this RPGuildApiFactory factory, UsuarioLogado logado, string url)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return await cliente.GetAsync(url);
    }

    public static async Task<HttpResponseMessage> EditarAsync(this RPGuildApiFactory factory, UsuarioLogado logado, Guid campanhaId, object corpo)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return await cliente.PutAsJsonAsync(Campanha(campanhaId), corpo);
    }

    /// <summary>
    /// Path de capa ou banner novo na pasta do usuário, como o que o POST /api/uploads/sign gera.
    /// </summary>
    public static string NovaImagemDeCampanha(this UsuarioLogado logado) => $"campaigns/{logado.Id}/{Guid.NewGuid()}.png";

    public static async Task<HttpResponseMessage> OrdenarAsync(this RPGuildApiFactory factory, UsuarioLogado logado, IEnumerable<Guid> campaignIds)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return await cliente.PutAsJsonAsync($"{Url}/order", new { campaignIds });
    }

    /// <summary>
    /// Quantos e-mails o usuário recebeu até agora, pela fila ou direto.
    /// </summary>
    public static int EmailsPara(this RPGuildApiFactory factory, UsuarioLogado usuario) =>
        factory.Fila.Enfileiradas.Count(m => m.Para == usuario.Dados.Email)
        + factory.Emails.Enviadas.Count(m => m.Para == usuario.Dados.Email);

    public static async Task<List<CardResponse>> CampanhasDeAsync(this RPGuildApiFactory factory, UsuarioLogado logado)
    {
        using var cliente = factory.CriarClienteAutenticado(logado.AccessToken);
        return (await cliente.GetFromJsonAsync<List<CardResponse>>(Url))!;
    }
}
