using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Notificacoes.TempoReal;

[Collection(IntegrationCollection.Name)]
public sealed class SincronizacaoEntreAbasTests(PostgresFixture postgres) : IDisposable
{
    private readonly RPGuildApiFactory _factory = new(postgres);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Read_NasDuasAbas_ChegaNotificationReadComOId()
    {
        // Arrange
        var (b, abas) = await UsuarioComDuasAbasAsync();
        var id = await _factory.CriarNotificacaoAsync(b.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        await using var _ = abas;

        // Act
        (await _factory.PostAsync(b, ClienteDeNotificacoes.Ler(id))).EnsureSuccessStatusCode();

        // Assert
        foreach (var aba in abas.Todas)
        {
            (await aba.EsperarAsync("NotificationRead")).GetProperty("id").GetGuid().ShouldBe(id);
        }
    }

    [Fact]
    public async Task Read_DeUmaJaLida_NaoMandaNotificationReadDeNovo()
    {
        // Arrange
        var (b, abas) = await UsuarioComDuasAbasAsync();
        var id = await _factory.CriarNotificacaoAsync(b.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        await using var _ = abas;
        (await _factory.PostAsync(b, ClienteDeNotificacoes.Ler(id))).EnsureSuccessStatusCode();
        await abas.Um.EsperarAsync("NotificationRead");
        abas.Um.Esquecer();

        // Act
        (await _factory.PostAsync(b, ClienteDeNotificacoes.Ler(id))).EnsureSuccessStatusCode();

        // Assert
        await abas.Um.NaoRecebeAsync("NotificationRead");
    }

    [Fact]
    public async Task ReadAll_NasDuasAbas_ChegaNotificationsReadAll()
    {
        // Arrange
        var (b, abas) = await UsuarioComDuasAbasAsync();
        await using var _ = abas;

        // Act
        (await _factory.PostAsync(b, ClienteDeNotificacoes.LerTodas)).EnsureSuccessStatusCode();

        // Assert
        foreach (var aba in abas.Todas)
        {
            (await aba.EsperarAsync("NotificationsReadAll")).EnumerateObject().ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Delete_NasDuasAbas_ChegaNotificationDeletedComOId()
    {
        // Arrange
        var (b, abas) = await UsuarioComDuasAbasAsync();
        var id = await _factory.CriarNotificacaoAsync(b.Id, DateTimeOffset.UtcNow.AddMinutes(-1));
        await using var _ = abas;

        // Act
        (await _factory.DeleteAsync(b, ClienteDeNotificacoes.Notificacao(id))).EnsureSuccessStatusCode();

        // Assert
        foreach (var aba in abas.Todas)
        {
            (await aba.EsperarAsync("NotificationDeleted")).GetProperty("id").GetGuid().ShouldBe(id);
        }
    }

    [Fact]
    public async Task ReadOuDelete_NaDeOutroUsuario_NaoMandaNadaParaNinguem()
    {
        // Arrange
        var (b, abasDeB) = await UsuarioComDuasAbasAsync();
        var intruso = await _factory.CriarUsuarioELogarAsync();
        await using var _ = abasDeB;
        await using var hubDoIntruso = await ConexaoNoHub.AbrirAsync(_factory, intruso.AccessToken);
        var deB = await _factory.CriarNotificacaoAsync(b.Id, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        (await _factory.PostAsync(intruso, ClienteDeNotificacoes.Ler(deB))).DeveSerProblema(System.Net.HttpStatusCode.NotFound);
        (await _factory.DeleteAsync(intruso, ClienteDeNotificacoes.Notificacao(deB))).DeveSerProblema(System.Net.HttpStatusCode.NotFound);

        // Assert
        await hubDoIntruso.NaoRecebeAsync("NotificationRead");
        foreach (var conexao in abasDeB.Todas.Append(hubDoIntruso))
        {
            conexao.Recebidos.ShouldBeEmpty();
        }
    }

    /// <summary>
    /// Um usuário com duas sessões e uma conexão no hub em cada (duas abas).
    /// </summary>
    private async Task<(UsuarioLogado Usuario, DuasAbas Abas)> UsuarioComDuasAbasAsync()
    {
        var usuario = await _factory.CriarUsuarioELogarAsync();
        var outraSessao = await _factory.LogarAsync(usuario.Dados);
        var um = await ConexaoNoHub.AbrirAsync(_factory, usuario.AccessToken);
        var dois = await ConexaoNoHub.AbrirAsync(_factory, outraSessao.AccessToken);
        return (usuario, new DuasAbas(um, dois));
    }

    private sealed record DuasAbas(ConexaoNoHub Um, ConexaoNoHub Dois) : IAsyncDisposable
    {
        public IEnumerable<ConexaoNoHub> Todas => [Um, Dois];

        public async ValueTask DisposeAsync()
        {
            await Um.DisposeAsync();
            await Dois.DisposeAsync();
        }
    }
}
