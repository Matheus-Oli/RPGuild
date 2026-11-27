using System.Net;
using System.Net.Http.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas;

[Collection(IntegrationCollection.Name)]
public sealed class CriarCampanhaTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Criar_Valida_Retorna201ComOUsuarioComoMestre()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var capa = $"campaigns/{mestre.Id}/{Guid.NewGuid()}.png";
        var banner = $"campaigns/{mestre.Id}/{Guid.NewGuid()}.webp";

        // Act
        var resposta = await _factory.CriarAsync(mestre, new
        {
            nome = "  A Mina Perdida  ",
            descricao = "Phandelver.",
            sistema = CampanhasDeTeste.SistemaAtivo,
            capaPath = capa,
            bannerPath = banner,
        });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var detalhe = (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!;
        resposta.Headers.Location!.ToString().ShouldEndWith($"{CampanhasDeTeste.Url}/{detalhe.Id}");
        detalhe.Nome.ShouldBe("A Mina Perdida");
        detalhe.Descricao.ShouldBe("Phandelver.");
        detalhe.Sistema.ShouldBe(new SistemaResponse("dnd2024", "D&D 5e (regras de 2024)"));
        detalhe.Papel.ShouldBe("mestre");
        detalhe.Mestre.ShouldBe(new PessoaDaCampanhaResponse(mestre.Id, mestre.Dados.Nome, mestre.Dados.Nick, null));
        detalhe.Jogadores.ShouldBeEmpty();
        detalhe.CapaUrl.ShouldBe(_factory.Storage.ObterUrlPublica(capa));
        detalhe.BannerUrl.ShouldBe(_factory.Storage.ObterUrlPublica(banner));
    }

    [Fact]
    public async Task Criar_UsuarioComOutrasCampanhas_PoeANovaNoInicioDaGrade()
    {
        // Arrange
        var (mestre, antigas) = await _factory.CriarUsuarioComCampanhasAsync(2);

        // Act
        var nova = await _factory.CriarCampanhaAsync(mestre);

        // Assert
        (await _factory.OrdemNoBancoAsync(mestre.Id)).ShouldBe([nova.Id, antigas[1], antigas[0]]);
    }

    [Theory]
    [InlineData("ordem2")]
    [InlineData("deadlands")]
    [InlineData("sistema-que-nao-existe")]
    public async Task Criar_SistemaInativoOuInexistente_Retorna400ENaoCria(string sistema)
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.CriarAsync(mestre, new { nome = "Campanha", sistema });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.CampanhasNoBancoAsync(mestre.Id)).ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Criar_NomeVazio_Retorna400(string? nome)
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.CriarAsync(mestre, new { nome, sistema = CampanhasDeTeste.SistemaAtivo });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.CampanhasNoBancoAsync(mestre.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Criar_NomeCom80Caracteres_Retorna201()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.CriarAsync(mestre, new { nome = new string('a', 80), sistema = CampanhasDeTeste.SistemaAtivo });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Criar_NomeCom81Caracteres_Retorna400()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.CriarAsync(mestre, new { nome = new string('a', 81), sistema = CampanhasDeTeste.SistemaAtivo });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_DescricaoCom2001Caracteres_Retorna400()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.CriarAsync(
            mestre, new { nome = "Campanha", descricao = new string('a', 2001), sistema = CampanhasDeTeste.SistemaAtivo });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("capaPath")]
    [InlineData("bannerPath")]
    public async Task Criar_ImagemNaPastaDeOutroUsuario_Retorna400ENaoCria(string campo)
    {
        // Arrange
        var (mestre, outro) = await _factory.CriarDoisUsuariosLogadosAsync();
        var path = $"campaigns/{outro.Id}/{Guid.NewGuid()}.png";
        var corpo = new Dictionary<string, object?>
        {
            ["nome"] = "Campanha",
            ["sistema"] = CampanhasDeTeste.SistemaAtivo,
            [campo] = path,
        };

        // Act
        var resposta = await _factory.CriarAsync(mestre, corpo);

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        (await _factory.CampanhasNoBancoAsync(mestre.Id)).ShouldBe(0);
    }

    [Theory]
    [InlineData("avatars/{0}/capa.png")]
    [InlineData("campaigns/{0}/../outro/capa.png")]
    [InlineData("campaigns/{0}/")]
    public async Task Criar_ImagemForaDaPastaDeCampanhasDoUsuario_Retorna400(string formato)
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.CriarAsync(mestre, new
        {
            nome = "Campanha",
            sistema = CampanhasDeTeste.SistemaAtivo,
            capaPath = string.Format(System.Globalization.CultureInfo.InvariantCulture, formato, mestre.Id),
        });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PostAsJsonAsync(CampanhasDeTeste.Url, new { nome = "Campanha", sistema = CampanhasDeTeste.SistemaAtivo });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
