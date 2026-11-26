using Microsoft.EntityFrameworkCore;
using Npgsql;
using RPGuild.Infrastructure.Persistence;
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
        await using var context = CriarDbContext();
        await context.Database.MigrateAsync();
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
