using Npgsql;

namespace RPGuild.IntegrationTests.Infra;

[Collection(IntegrationCollection.Name)]
public class PostgresFixtureTests(PostgresFixture postgres)
{
    [Fact]
    public async Task ConnectionString_SelectUm_RetornaUm()
    {
        // Arrange
        await using var conexao = new NpgsqlConnection(postgres.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT 1", conexao);

        // Act
        var resultado = await comando.ExecuteScalarAsync();

        // Assert
        resultado.ShouldBe(1);
    }
}
