using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas;

[Collection(IntegrationCollection.Name)]
public sealed class OrdenarCampanhasTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Ordenar_OrdemValida_Retorna204EOGetRespeita()
    {
        // Arrange
        var (mestre, c) = await _factory.CriarUsuarioComCampanhasAsync(3);
        Guid[] nova = [c[1], c[0], c[2]];

        // Act
        var resposta = await _factory.OrdenarAsync(mestre, nova);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.CampanhasDeAsync(mestre)).Select(card => card.Id).ShouldBe(nova);
    }

    [Fact]
    public async Task Ordenar_JogadorReordena_NaoMudaAOrdemDoMestre()
    {
        // Arrange
        var (mestre, c) = await _factory.CriarUsuarioComCampanhasAsync(2);
        var jogador = await _factory.CriarUsuarioELogarAsync();
        await _factory.AdicionarJogadorAsync(c[0], jogador);
        await _factory.AdicionarJogadorAsync(c[1], jogador);

        // Act
        var resposta = await _factory.OrdenarAsync(jogador, [c[0], c[1]]);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _factory.CampanhasDeAsync(jogador)).Select(card => card.Id).ShouldBe([c[0], c[1]]);
        (await _factory.CampanhasDeAsync(mestre)).Select(card => card.Id).ShouldBe([c[1], c[0]]);
    }

    [Fact]
    public async Task Ordenar_ListaIncompleta_Retorna400ENaoMuda()
    {
        // Arrange
        var (mestre, c) = await _factory.CriarUsuarioComCampanhasAsync(3);

        // Act
        var resposta = await _factory.OrdenarAsync(mestre, [c[0], c[1]]);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.OrdemNoBancoAsync(mestre.Id)).ShouldBe([c[2], c[1], c[0]]);
    }

    [Fact]
    public async Task Ordenar_ComCampanhaDeOutraPessoa_Retorna400ENaoMuda()
    {
        // Arrange
        var (mestre, c) = await _factory.CriarUsuarioComCampanhasAsync(2);
        var (_, deOutro) = await _factory.CriarUsuarioComCampanhasAsync(1);

        // Act
        var resposta = await _factory.OrdenarAsync(mestre, [c[0], c[1], deOutro[0]]);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.OrdemNoBancoAsync(mestre.Id)).ShouldBe([c[1], c[0]]);
    }

    [Fact]
    public async Task Ordenar_TrocandoUmaPorOutraDeOutraPessoa_Retorna400()
    {
        // Arrange
        var (mestre, c) = await _factory.CriarUsuarioComCampanhasAsync(2);
        var (_, deOutro) = await _factory.CriarUsuarioComCampanhasAsync(1);

        // Act
        var resposta = await _factory.OrdenarAsync(mestre, [c[0], deOutro[0]]);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ordenar_ComCampanhaInexistente_Retorna400()
    {
        // Arrange
        var (mestre, c) = await _factory.CriarUsuarioComCampanhasAsync(1);

        // Act
        var resposta = await _factory.OrdenarAsync(mestre, [c[0], Guid.NewGuid()]);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ordenar_ComRepetida_Retorna400ENaoMuda()
    {
        // Arrange
        var (mestre, c) = await _factory.CriarUsuarioComCampanhasAsync(2);

        // Act
        var resposta = await _factory.OrdenarAsync(mestre, [c[0], c[1], c[0]]);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.OrdemNoBancoAsync(mestre.Id)).ShouldBe([c[1], c[0]]);
    }

    [Fact]
    public async Task Ordenar_ListaVazia_Retorna400()
    {
        // Arrange
        var (mestre, _) = await _factory.CriarUsuarioComCampanhasAsync(1);

        // Act
        var resposta = await _factory.OrdenarAsync(mestre, []);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_DepoisDeReordenar_PoeANovaNoInicioEMantemOResto()
    {
        // Arrange
        var (mestre, c) = await _factory.CriarUsuarioComCampanhasAsync(3);
        (await _factory.OrdenarAsync(mestre, [c[1], c[0], c[2]])).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var nova = await _factory.CriarCampanhaAsync(mestre);

        // Assert
        (await _factory.CampanhasDeAsync(mestre)).Select(card => card.Id).ShouldBe([nova.Id, c[1], c[0], c[2]]);
    }

    [Fact]
    public async Task Ordenar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PutAsJsonAsync($"{CampanhasDeTeste.Url}/order", new { campaignIds = new[] { Guid.NewGuid() } });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
