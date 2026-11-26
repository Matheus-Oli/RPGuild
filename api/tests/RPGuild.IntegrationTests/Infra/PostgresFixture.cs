using Microsoft.EntityFrameworkCore;
using Npgsql;
using RPGuild.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RPGuild.IntegrationTests.Infra;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Aplica o schema uma vez por execução; os testes já encontram as tabelas prontas.
        await using var context = CriarDbContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

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
