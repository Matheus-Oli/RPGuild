using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Taberna;

[Collection(IntegrationCollection.Name)]
public sealed class ListarAmigosTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListarAmigos_VariosAmigos_MaisRecentesPrimeiroComCampanhasEmComumZero()
    {
        // Arrange: B virou amigo há 2 dias, C há 1 dia e D agora; os pedidos foram de lados diferentes.
        var usuarios = await _factory.CriarUsuariosLogadosAsync(4);
        var (a, b, c, d) = (usuarios[0], usuarios[1], usuarios[2], usuarios[3]);
        var agora = DateTimeOffset.UtcNow;
        await _factory.TornarAmigosAsync(a, b, agora.AddDays(-2));
        await _factory.TornarAmigosAsync(c, a, agora.AddDays(-1));
        await _factory.TornarAmigosAsync(a, d, agora);

        // Act
        var amigos = await _factory.AmigosDeAsync(a);

        // Assert
        amigos.Select(x => x.UserId).ShouldBe([d.Id, c.Id, b.Id]);
        amigos.ShouldAllBe(x => x.CampanhasEmComum == 0);
        amigos[1].ShouldBe(new AmigoResponse(c.Id, c.Dados.Nome, c.Dados.Nick, null, 0, amigos[1].AmigosDesde));
        amigos[1].AmigosDesde.ShouldBe(agora.AddDays(-1), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task ListarAmigos_AmigoComAvatar_TrazAUrlPublica()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        await _factory.TornarAmigosAsync(a, b);
        var avatar = b.NovoAvatar();
        using (var clienteDeB = _factory.CriarClienteAutenticado(b.AccessToken))
        {
            (await clienteDeB.PutAsJsonAsync("/api/me", new { b.Dados.Nome, b.Dados.Nick, avatarPath = avatar })).EnsureSuccessStatusCode();
        }

        // Act
        var amigo = (await _factory.AmigosDeAsync(a)).ShouldHaveSingleItem();

        // Assert
        amigo.AvatarUrl.ShouldBe(_factory.Storage.ObterUrlPublica(avatar));
    }

    [Fact]
    public async Task ListarAmigos_Resposta_NaoTemEmailDeNinguem()
    {
        // Arrange
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        await _factory.TornarAmigosAsync(a, b);
        using var cliente = _factory.CriarClienteAutenticado(a.AccessToken);

        // Act
        var json = await cliente.GetStringAsync(ClienteDaTaberna.Amigos);

        // Assert
        RespostaSemEmail.Conferir(json, a, b);
        JsonDocument.Parse(json).RootElement.GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task ListarAmigos_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.GetAsync(ClienteDaTaberna.Amigos);

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
