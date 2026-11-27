using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas;

[Collection(IntegrationCollection.Name)]
public sealed class SistemasTests(PostgresFixture postgres) : IDisposable
{
    private const string Url = "/api/systems";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListarSistemas_Logado_DevolveOsCincoComSoODnd2024Ativo()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        using var cliente = _factory.CriarClienteAutenticado(logado.AccessToken);

        // Act
        var sistemas = (await cliente.GetFromJsonAsync<List<SistemaDisponivelResponse>>(Url))!;

        // Assert
        sistemas.ShouldBe(
        [
            new SistemaDisponivelResponse("dnd2024", "D&D 5e (regras de 2024)", true),
            new SistemaDisponivelResponse("ordem2", "Ordem Paranormal 2", false),
            new SistemaDisponivelResponse("t20", "Tormenta 20", false),
            new SistemaDisponivelResponse("coc", "Call of Cthulhu", false),
            new SistemaDisponivelResponse("deadlands", "Deadlands", false),
        ]);
    }

    [Fact]
    public async Task ListarSistemas_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(Url);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
