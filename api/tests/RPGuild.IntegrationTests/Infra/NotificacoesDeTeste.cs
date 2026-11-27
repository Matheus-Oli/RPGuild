using Npgsql;
using NpgsqlTypes;
using RPGuild.Infrastructure.Comum;

namespace RPGuild.IntegrationTests.Infra;

public static class NotificacoesDeTeste
{
    /// <summary>
    /// Grava uma notificação direto no banco, com o created_at controlável, e devolve o id.
    /// </summary>
    public static async Task<Guid> CriarNotificacaoAsync(
        this RPGuildApiFactory factory,
        Guid userId,
        DateTimeOffset? criadaEm = null,
        string tipo = "boas_vindas",
        string dados = "{}",
        bool lida = false)
    {
        // Na precisão do Postgres, como o relógio da API.
        var quando = RelogioEmMicrossegundos.Truncar(criadaEm ?? DateTimeOffset.UtcNow);
        var id = Guid.CreateVersion7(quando);

        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO notifications (id, user_id, tipo, dados, lida_em, created_at)
            VALUES (@id, @user, @tipo, @dados, @lida, @criada)
            """,
            conexao);
        comando.Parameters.AddWithValue("id", id);
        comando.Parameters.AddWithValue("user", userId);
        comando.Parameters.AddWithValue("tipo", tipo);
        comando.Parameters.AddWithValue("dados", NpgsqlDbType.Jsonb, dados);
        comando.Parameters.AddWithValue("lida", lida ? quando : DBNull.Value);
        comando.Parameters.AddWithValue("criada", quando);
        await comando.ExecuteNonQueryAsync();

        return id;
    }

    /// <summary>
    /// Quantos e-mails o usuário recebeu até agora, pela fila ou direto.
    /// </summary>
    public static int EmailsPara(this RPGuildApiFactory factory, UsuarioLogado usuario) =>
        factory.Fila.Enfileiradas.Count(m => m.Para == usuario.Dados.Email)
        + factory.Emails.Enviadas.Count(m => m.Para == usuario.Dados.Email);

    /// <summary>
    /// As notificações do usuário como estão no banco, das mais novas para as mais antigas.
    /// </summary>
    public static async Task<List<NotificacaoNoBanco>> NotificacoesNoBancoAsync(this RPGuildApiFactory factory, Guid userId)
    {
        await using var conexao = new NpgsqlConnection(factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT id, tipo, dados::text, lida_em IS NOT NULL FROM notifications WHERE user_id = @user ORDER BY created_at DESC, id",
            conexao);
        comando.Parameters.AddWithValue("user", userId);

        var notificacoes = new List<NotificacaoNoBanco>();
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            notificacoes.Add(new NotificacaoNoBanco(leitor.GetGuid(0), leitor.GetString(1), leitor.GetString(2), leitor.GetBoolean(3)));
        }

        return notificacoes;
    }
}

public sealed record NotificacaoNoBanco(Guid Id, string Tipo, string Dados, bool Lida);
