using System.Net;
using System.Net.Http.Json;
using RPGuild.Infrastructure.Comum;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas;

[Collection(IntegrationCollection.Name)]
public sealed class MembrosDaCampanhaTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Membros_Mestre_VemPrimeiroEDepoisOsJogadoresNaOrdemEmQueEntraram()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(3);
        var (mestre, primeiro, segundo) = (usuarios[0], usuarios[1], usuarios[2]);
        var criada = await _factory.CriarCampanhaAsync(mestre);
        var agora = RelogioEmMicrossegundos.Truncar(DateTimeOffset.UtcNow);
        await _factory.AdicionarJogadorAsync(criada.Id, segundo, entrouEm: agora.AddMinutes(5));
        await _factory.AdicionarJogadorAsync(criada.Id, primeiro, entrouEm: agora.AddMinutes(1));

        // Act
        var resposta = await _factory.GetAsync(mestre, CampanhasDeTeste.Membros(criada.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var membros = (await resposta.Content.ReadFromJsonAsync<List<MembroResponse>>())!;
        membros.ShouldBe(
        [
            new MembroResponse(mestre.Id, mestre.Dados.Nome, mestre.Dados.Nick, null, "mestre", criada.CriadaEm, null),
            new MembroResponse(primeiro.Id, primeiro.Dados.Nome, primeiro.Dados.Nick, null, "jogador", agora.AddMinutes(1), null),
            new MembroResponse(segundo.Id, segundo.Dados.Nome, segundo.Dados.Nick, null, "jogador", agora.AddMinutes(5), null),
        ]);
    }

    [Fact]
    public async Task Membros_Jogador_Retorna200()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.GetAsync(jogador, CampanhasDeTeste.Membros(criada.Id));

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<List<MembroResponse>>())!.Select(m => m.UserId).ShouldBe([mestre.Id, jogador.Id]);
    }

    [Fact]
    public async Task Membros_ComAvatar_TemAUrlPublica()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var avatar = mestre.NovoAvatar();
        using (var cliente = _factory.CriarClienteAutenticado(mestre.AccessToken))
        {
            (await cliente.PutAsJsonAsync("/api/me", new { nome = mestre.Dados.Nome, nick = mestre.Dados.Nick, avatarPath = avatar }))
                .StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var criada = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.GetAsync(mestre, CampanhasDeTeste.Membros(criada.Id));

        // Assert
        var membro = (await resposta.Content.ReadFromJsonAsync<List<MembroResponse>>())!.ShouldHaveSingleItem();
        membro.AvatarUrl.ShouldBe(_factory.Storage.ObterUrlPublica(avatar));
    }

    [Fact]
    public async Task Membros_QuemNaoParticipa_Retorna404()
    {
        // Arrange
        var (mestre, estranho) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.GetAsync(estranho, CampanhasDeTeste.Membros(criada.Id));

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Membros_Resposta_NaoTemEmailDeNinguem()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.GetAsync(mestre, CampanhasDeTeste.Membros(criada.Id));

        // Assert
        RespostaSemEmail.Conferir(await resposta.Content.ReadAsStringAsync(), mestre, jogador);
    }
}
