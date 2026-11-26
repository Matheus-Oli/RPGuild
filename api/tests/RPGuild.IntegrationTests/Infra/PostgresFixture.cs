using Microsoft.EntityFrameworkCore;
using Npgsql;
using RPGuild.Infrastructure.Persistence;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace RPGuild.IntegrationTests.Infra;

public sealed class PostgresFixture : IAsyncLifetime
{
    public const string NomeContainer = "rpguild-tests-db";

    // Em dev o container sobrevive entre execuções (porta aleatória, sem brigar com o compose na 5432).
    // Em CI (CI=true) ele é sempre novo e removido no fim.
    private static readonly bool Reaproveitar = !string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    private readonly PostgreSqlContainer _container = CriarContainer();

    public string ConnectionString => _container.GetConnectionString();

    private static PostgreSqlContainer CriarContainer()
    {
        var builder = new PostgreSqlBuilder("postgres:17-alpine");

        if (Reaproveitar)
        {
            builder = builder.WithName(NomeContainer).WithReuse(true);
        }

        return builder.Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // MigrateAsync só aplica as migrations pendentes; num container reaproveitado normalmente não há nenhuma.
        await using (var context = CriarDbContext())
        {
            await context.Database.MigrateAsync();
        }

        await RemoverBancosVaziosAsync();
        await LimparAsync();
    }

    /// <summary>
    /// Apaga os dados de todas as tabelas do banco padrão, menos o histórico de migrations.
    /// Roda ao subir a fixture; chame também numa classe de teste que precise começar do banco limpo.
    /// </summary>
    public async Task LimparAsync()
    {
        await using var conexao = new NpgsqlConnection(ConnectionString);
        await conexao.OpenAsync();

        var respawner = await Respawner.CreateAsync(conexao, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            // Só o schema do EF: o "hangfire" (fila e jobs, criado pela própria lib) fica de fora.
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });
        await respawner.ResetAsync(conexao);
    }

    // Os bancos criados por CriarBancoVazioAsync se acumulariam no container reaproveitado.
    private async Task RemoverBancosVaziosAsync()
    {
        await using var conexao = new NpgsqlConnection(ConnectionString);
        await conexao.OpenAsync();

        var nomes = new List<string>();
        await using (var consulta = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datname LIKE 'vazio\\_%'", conexao))
        await using (var leitor = await consulta.ExecuteReaderAsync())
        {
            while (await leitor.ReadAsync())
            {
                nomes.Add(leitor.GetString(0));
            }
        }

        foreach (var nome in nomes)
        {
            await using var comando = new NpgsqlCommand($"DROP DATABASE \"{nome}\" WITH (FORCE)", conexao);
            await comando.ExecuteNonQueryAsync();
        }
    }

    // O Dispose de um container reaproveitado o para; deixamos rodando para a próxima execução.
    public Task DisposeAsync() => Reaproveitar ? Task.CompletedTask : _container.DisposeAsync().AsTask();

    /// <summary>
    /// Cria um <see cref="AppDbContext"/> com a mesma configuração da API, no banco padrão do container ou no informado.
    /// </summary>
    public AppDbContext CriarDbContext(string? connectionString = null)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContextOptions.Configure(builder, connectionString ?? ConnectionString);
        return new AppDbContext(builder.Options);
    }

    /// <summary>
    /// Cria um banco novo, sem nenhuma tabela, no mesmo container e devolve a connection string dele.
    /// </summary>
    public async Task<string> CriarBancoVazioAsync()
    {
        var nome = $"vazio_{Guid.NewGuid():N}";

        await using (var conexao = new NpgsqlConnection(ConnectionString))
        {
            await conexao.OpenAsync();
            await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{nome}\"", conexao);
            await comando.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = nome }.ConnectionString;
    }
}
