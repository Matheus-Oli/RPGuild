using Npgsql;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes;

/// <summary>
/// Com o Hangfire de verdade (storage no schema "hangfire" do container e server), no lugar da fila falsa.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class FilaHangfireTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres, usarHangfire: true);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_EmailComum_EnfileiraUmJobDeEnvio()
    {
        // Act
        var (usuario, _) = await _factory.RegistrarUsuarioAsync();

        // Assert
        var jobs = await JobsQueCitamAsync(usuario.Email);
        var job = jobs.ShouldHaveSingleItem();
        job.ShouldContain("EnvioDeEmailJob");
        job.ShouldContain("Boas-vindas");
    }

    [Fact]
    public async Task ForgotPassword_EmailComToken_NaoCriaJobENenhumJobTemOToken()
    {
        // Arrange: as boas-vindas do register são o único job desse usuário.
        var (usuario, _) = await _factory.RegistrarUsuarioAsync();

        // Act
        var token = await _factory.PedirRedefinicaoAsync(usuario.Email);

        // Assert
        var jobDoUsuario = (await JobsQueCitamAsync(usuario.Email)).ShouldHaveSingleItem();
        jobDoUsuario.ShouldContain("Boas-vindas");
        jobDoUsuario.ShouldNotContain("redefinir-senha");
        (await JobsQueCitamAsync(token)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Startup_ComHangfire_AgendaALimpezaDiariaDasNotificacoes()
    {
        // Arrange: o container é reaproveitado entre execuções, então tira o agendamento de antes (se houver).
        await using var conexao = new NpgsqlConnection(postgres.ConnectionString);
        await conexao.OpenAsync();
        await using (var limpar = new NpgsqlCommand(
            """
            DO $$
            BEGIN
                IF to_regclass('hangfire.set') IS NOT NULL THEN
                    DELETE FROM hangfire.set WHERE key = 'recurring-jobs' AND value = 'limpeza-de-notificacoes';
                    DELETE FROM hangfire.hash WHERE key = 'recurring-job:limpeza-de-notificacoes';
                END IF;
            END $$;
            """,
            conexao))
        {
            await limpar.ExecuteNonQueryAsync();
        }

        // Act: subir a API registra os jobs recorrentes.
        _ = _factory.Services;

        // Assert
        await using var comando = new NpgsqlCommand(
            "SELECT count(*) FROM hangfire.set WHERE key = 'recurring-jobs' AND value = 'limpeza-de-notificacoes'", conexao);
        ((long)(await comando.ExecuteScalarAsync())!).ShouldBe(1);
    }

    /// <summary>
    /// Invocação e argumentos dos jobs que contêm o texto (o Hangfire grava os argumentos em JSON).
    /// </summary>
    private async Task<List<string>> JobsQueCitamAsync(string texto)
    {
        await using var conexao = new NpgsqlConnection(postgres.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT invocationdata::text || ' ' || arguments::text
            FROM hangfire.job
            WHERE strpos(invocationdata::text || ' ' || arguments::text, @texto) > 0
            """,
            conexao);
        comando.Parameters.AddWithValue("texto", texto);

        var jobs = new List<string>();
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync())
        {
            jobs.Add(leitor.GetString(0));
        }

        return jobs;
    }
}
