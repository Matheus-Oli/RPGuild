using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Campanhas;

[Collection(IntegrationCollection.Name)]
public sealed class EditarCampanhaTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Editar_Mestre_Retorna200ComODetalheAtualizado()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);
        var capa = mestre.NovaImagemDeCampanha();
        var banner = mestre.NovaImagemDeCampanha();

        // Act
        var resposta = await _factory.EditarAsync(
            mestre, criada.Id, new { nome = "  Novo nome  ", descricao = "Nova descrição.", capaPath = capa, bannerPath = banner });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var detalhe = (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!;
        detalhe.Nome.ShouldBe("Novo nome");
        detalhe.Descricao.ShouldBe("Nova descrição.");
        detalhe.CapaUrl.ShouldBe(_factory.Storage.ObterUrlPublica(capa));
        detalhe.BannerUrl.ShouldBe(_factory.Storage.ObterUrlPublica(banner));
        detalhe.Papel.ShouldBe("mestre");
        (await (await _factory.GetAsync(mestre, CampanhasDeTeste.Campanha(criada.Id))).Content.ReadAsStringAsync())
            .ShouldBe(await resposta.Content.ReadAsStringAsync(), "o GET devolve o mesmo que o PUT");
    }

    [Fact]
    public async Task Editar_ComSistemaNoCorpo_NaoMudaOSistema()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.EditarAsync(mestre, criada.Id, new { nome = "Outro nome", sistema = "ordem2" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!.Sistema.Slug.ShouldBe("dnd2024");
    }

    [Fact]
    public async Task Editar_Jogador_Retorna403ENaoMuda()
    {
        // Arrange
        var (mestre, jogador) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre, nome: "Original");
        await _factory.AdicionarJogadorAsync(criada.Id, jogador);

        // Act
        var resposta = await _factory.EditarAsync(jogador, criada.Id, new { nome = "Tomada" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.Forbidden);
        (await _factory.CampanhasDeAsync(mestre)).ShouldHaveSingleItem().Nome.ShouldBe("Original");
    }

    [Fact]
    public async Task Editar_QuemNaoParticipa_Retorna404()
    {
        // Arrange
        var (mestre, estranho) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre, nome: "Original");

        // Act
        var resposta = await _factory.EditarAsync(estranho, criada.Id, new { nome = "Tomada" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
        (await _factory.CampanhasDeAsync(mestre)).ShouldHaveSingleItem().Nome.ShouldBe("Original");
    }

    [Fact]
    public async Task Editar_CampanhaInexistente_Retorna404()
    {
        // Arrange
        var logado = await _factory.CriarUsuarioELogarAsync();

        // Act
        var resposta = await _factory.EditarAsync(logado, Guid.NewGuid(), new { nome = "Nome" });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Editar_TrocandoACapa_ApagaAAntigaDoStorage()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var antiga = mestre.NovaImagemDeCampanha();
        var banner = mestre.NovaImagemDeCampanha();
        var criada = await _factory.CriarCampanhaAsync(mestre, capaPath: antiga, bannerPath: banner);

        // Act
        var resposta = await _factory.EditarAsync(
            mestre, criada.Id, new { nome = criada.Nome, capaPath = mestre.NovaImagemDeCampanha(), bannerPath = banner });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        _factory.Storage.Apagados.ShouldBe([antiga]);
    }

    [Fact]
    public async Task Editar_RemovendoOBanner_ApagaODoStorage()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var banner = mestre.NovaImagemDeCampanha();
        var criada = await _factory.CriarCampanhaAsync(mestre, bannerPath: banner);

        // Act
        var resposta = await _factory.EditarAsync(mestre, criada.Id, new { nome = criada.Nome, bannerPath = (string?)null });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<DetalheResponse>())!.BannerUrl.ShouldBeNull();
        _factory.Storage.Apagados.ShouldBe([banner]);
    }

    [Fact]
    public async Task Editar_MantendoAsImagens_NaoApagaNada()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var (capa, banner) = (mestre.NovaImagemDeCampanha(), mestre.NovaImagemDeCampanha());
        var criada = await _factory.CriarCampanhaAsync(mestre, capaPath: capa, bannerPath: banner);

        // Act
        var resposta = await _factory.EditarAsync(mestre, criada.Id, new { nome = "Outro nome", capaPath = capa, bannerPath = banner });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        _factory.Storage.Apagados.ShouldBeEmpty();
    }

    [Fact]
    public async Task Editar_FalhaAoApagarACapaAntiga_Retorna200ComACapaNova()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre, capaPath: mestre.NovaImagemDeCampanha());
        _factory.Storage.FalharAoApagar = true;
        var nova = mestre.NovaImagemDeCampanha();

        // Act
        var resposta = await _factory.EditarAsync(mestre, criada.Id, new { nome = criada.Nome, capaPath = nova });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _factory.CampanhasDeAsync(mestre)).ShouldHaveSingleItem().CapaUrl.ShouldBe(_factory.Storage.ObterUrlPublica(nova));
    }

    [Theory]
    [InlineData("capaPath")]
    [InlineData("bannerPath")]
    public async Task Editar_ImagemNaPastaDeOutroUsuario_Retorna400(string campo)
    {
        // Arrange
        var (mestre, outro) = await _factory.CriarDoisUsuariosLogadosAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.EditarAsync(
            mestre, criada.Id, new Dictionary<string, object?> { ["nome"] = criada.Nome, [campo] = outro.NovaImagemDeCampanha() });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
        _factory.Storage.Apagados.ShouldBeEmpty();
    }

    [Fact]
    public async Task Editar_NomeVazio_Retorna400()
    {
        // Arrange
        var mestre = await _factory.CriarUsuarioELogarAsync();
        var criada = await _factory.CriarCampanhaAsync(mestre);

        // Act
        var resposta = await _factory.EditarAsync(mestre, criada.Id, new { nome = " " });

        // Assert
        resposta.DeveSerProblema(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Editar_Mestre_AvisaOsJogadoresNoSininhoENaoAvisaOMestre()
    {
        // Arrange
        var usuarios = await _factory.CriarUsuariosLogadosAsync(3);
        var (mestre, jogadores) = (usuarios[0], usuarios[1..]);
        var criada = await _factory.CriarCampanhaAsync(mestre);
        foreach (var jogador in jogadores)
        {
            await _factory.AdicionarJogadorAsync(criada.Id, jogador);
        }

        var emailsAntes = jogadores.Select(j => _factory.EmailsPara(j)).ToList();

        // Act
        var resposta = await _factory.EditarAsync(mestre, criada.Id, new { nome = "Nome novo" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        foreach (var jogador in jogadores)
        {
            var aviso = (await _factory.NotificacoesNoBancoAsync(jogador.Id)).Where(n => n.Tipo == "campanha_editada").ShouldHaveSingleItem();
            var dados = JsonDocument.Parse(aviso.Dados).RootElement;
            dados.GetProperty("campanhaId").GetGuid().ShouldBe(criada.Id);
            dados.GetProperty("campanhaNome").GetString().ShouldBe("Nome novo");
        }

        jogadores.Select(j => _factory.EmailsPara(j)).ShouldBe(emailsAntes, "o aviso nº 17 é só no sininho");
        (await _factory.NotificacoesNoBancoAsync(mestre.Id)).ShouldNotContain(n => n.Tipo == "campanha_editada");
    }

    [Fact]
    public async Task Editar_SemToken_Retorna401()
    {
        // Arrange
        using var cliente = _factory.CriarCliente();

        // Act
        var resposta = await cliente.PutAsJsonAsync(CampanhasDeTeste.Campanha(Guid.NewGuid()), new { nome = "Nome" });

        // Assert
        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
