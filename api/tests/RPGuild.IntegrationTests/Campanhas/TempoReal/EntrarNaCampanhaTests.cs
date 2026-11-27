using System.Net;
using Microsoft.AspNetCore.SignalR;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class EntrarNaCampanhaTests(PostgresFixture postgres) : IDisposable
{
    private const string NaoEncontrada = "HubException: Campanha não encontrada";

    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Conectar_TokenNaQuery_Conecta()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        await using var conexao = await _factory.AbrirNoHubDaCampanhaAsync(logado);

        // Assert: sem exceção ao abrir.
        conexao.Recebidos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Conectar_SemToken_ERecusado()
    {
        // Act & Assert
        var erro = await Should.ThrowAsync<HttpRequestException>(
            () => ConexaoNoHub.AbrirAsync(_factory, accessToken: null, ConexaoNoHub.RotaDaCampanha));
        erro.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JoinCampaign_Mestre_Entra()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await using var conexao = await _factory.AbrirNoHubDaCampanhaAsync(mestre);

        // Act & Assert
        await Should.NotThrowAsync(() => conexao.EntrarNaCampanhaAsync(campanha.Id));
    }

    [Fact]
    public async Task JoinCampaign_Jogador_Entra()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(campanha.Id, jogador);
        await using var conexao = await _factory.AbrirNoHubDaCampanhaAsync(jogador);

        // Act & Assert
        await Should.NotThrowAsync(() => conexao.EntrarNaCampanhaAsync(campanha.Id));
    }

    [Fact]
    public async Task JoinCampaign_QuemNaoParticipa_RecebeCampanhaNaoEncontrada()
    {
        // Arrange
        var (mestre, outro) = await _factory.CriarDoisUsuariosLogadosAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await using var conexao = await _factory.AbrirNoHubDaCampanhaAsync(outro);

        // Act
        var erro = await Should.ThrowAsync<HubException>(() => conexao.EntrarNaCampanhaAsync(campanha.Id));

        // Assert
        erro.Message.ShouldEndWith(NaoEncontrada);
    }

    [Fact]
    public async Task JoinCampaign_IdInexistente_RecebeCampanhaNaoEncontrada()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();
        await using var conexao = await _factory.AbrirNoHubDaCampanhaAsync(logado);

        // Act
        var erro = await Should.ThrowAsync<HubException>(() => conexao.EntrarNaCampanhaAsync(Guid.NewGuid()));

        // Assert
        erro.Message.ShouldEndWith(NaoEncontrada);
    }

    [Fact]
    public async Task LeaveCampaign_SemTerEntrado_NaoDaErro()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(mestre);
        await using var conexao = await _factory.AbrirNoHubDaCampanhaAsync(mestre);

        // Act & Assert
        await Should.NotThrowAsync(() => conexao.SairDaCampanhaAsync(campanha.Id));
    }
}
