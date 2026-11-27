using RPGuild.IntegrationTests.Infra;
using RPGuild.IntegrationTests.Taberna;

namespace RPGuild.IntegrationTests.Campanhas.Convites;

[Collection(IntegrationCollection.Name)]
public sealed class CampanhasEmComumTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListarAmigos_SemCampanhaJuntos_Da0()
    {
        // Arrange
        var (a, b) = await AmigosAsync();
        await _factory.CriarCampanhaAsync(a);
        await _factory.CriarCampanhaAsync(b);

        // Act & Assert
        await EmComumDeveSerAsync(a, b, 0);
    }

    [Fact]
    public async Task ListarAmigos_NumaCampanhaJuntos_Da1ParaOsDois()
    {
        // Arrange
        var (a, b) = await AmigosAsync();
        var campanha = await _factory.CriarCampanhaAsync(a);
        await _factory.AdicionarJogadorAsync(campanha.Id, b);

        // Act & Assert
        await EmComumDeveSerAsync(a, b, 1);
        await EmComumDeveSerAsync(b, a, 1);
    }

    [Fact]
    public async Task ListarAmigos_EmDuasCampanhasUmMestreDeCada_Da2()
    {
        // Arrange
        var (a, b) = await AmigosAsync();
        var deA = await _factory.CriarCampanhaAsync(a);
        var deB = await _factory.CriarCampanhaAsync(b);
        await _factory.AdicionarJogadorAsync(deA.Id, b);
        await _factory.AdicionarJogadorAsync(deB.Id, a);

        // Act & Assert
        await EmComumDeveSerAsync(a, b, 2);
        await EmComumDeveSerAsync(b, a, 2);
    }

    [Fact]
    public async Task ListarAmigos_OsDoisJogadoresNaCampanhaDeOutraPessoa_Conta()
    {
        // Arrange
        var (a, b) = await AmigosAsync();
        var outro = await _factory.CriarUsuarioELogarAsync();
        var campanha = await _factory.CriarCampanhaAsync(outro);
        await _factory.AdicionarJogadorAsync(campanha.Id, a);
        await _factory.AdicionarJogadorAsync(campanha.Id, b);

        // Act & Assert
        await EmComumDeveSerAsync(a, b, 1);
    }

    [Fact]
    public async Task ListarAmigos_ConvitePendente_NaoConta()
    {
        // Arrange
        var (a, b) = await AmigosAsync();
        var campanha = await _factory.CriarCampanhaAsync(a);
        await _factory.ConvidarComSucessoAsync(a, campanha.Id, b);

        // Act & Assert
        await EmComumDeveSerAsync(a, b, 0);
    }

    [Fact]
    public async Task ListarAmigos_VariosAmigos_ContaCadaUmSeparado()
    {
        // Arrange
        var (eu, amigos) = await _factory.CriarMestreComAmigosAsync(2);
        var campanha = await _factory.CriarCampanhaAsync(eu);
        await _factory.AdicionarJogadorAsync(campanha.Id, amigos[0]);

        // Act
        var lista = await _factory.AmigosDeAsync(eu);

        // Assert
        lista.Single(x => x.UserId == amigos[0].Id).CampanhasEmComum.ShouldBe(1);
        lista.Single(x => x.UserId == amigos[1].Id).CampanhasEmComum.ShouldBe(0);
    }

    private async Task<(UsuarioLogado A, UsuarioLogado B)> AmigosAsync()
    {
        var (a, b) = await _factory.CriarDoisUsuariosLogadosAsync();
        await _factory.TornarAmigosAsync(a, b);
        return (a, b);
    }

    private async Task EmComumDeveSerAsync(UsuarioLogado quem, UsuarioLogado amigo, int esperado) =>
        (await _factory.AmigosDeAsync(quem)).ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.UserId.ShouldBe(amigo.Id),
            x => x.CampanhasEmComum.ShouldBe(esperado));
}
