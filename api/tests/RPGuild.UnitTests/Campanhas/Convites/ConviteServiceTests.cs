using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RPGuild.Application.Campanhas;
using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Application.Notificacoes;
using RPGuild.Application.Uploads;
using RPGuild.Domain.Campanhas;
using RPGuild.Domain.Taberna;
using RPGuild.UnitTests.Taberna;

namespace RPGuild.UnitTests.Campanhas.Convites;

public class ConviteServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 27, 18, 0, 0, TimeSpan.Zero);

    private static readonly PessoaGuardada Mestre = new(Guid.NewGuid(), "Muirenn", "muirenn", null);
    private static readonly PessoaGuardada Amigo = new(Guid.NewGuid(), "Thorin", "thorin", null);
    private static readonly PessoaGuardada OutroAmigo = new(Guid.NewGuid(), "Bilbo", "bilbo", null);
    private static readonly PessoaGuardada Jogador = new(Guid.NewGuid(), "Selene", "selene", null);
    private static readonly PessoaGuardada Estranho = new(Guid.NewGuid(), "Gollum", "gollum", null);

    private readonly CampanhaRepositoryFalso _campanhas = new();
    private readonly ConviteRepositoryFalso _convites = new();
    private readonly AmizadeRepositoryFalso _amizades = new();
    private readonly GradeDeCampanhasRepositoryFalso _grade = new();
    private readonly Campanha _campanha = Campanha.Criar(Mestre.Id, 1, "A Mina Perdida", null, null, null, Agora.AddDays(-1));
    private readonly ConviteService _service;

    public ConviteServiceTests()
    {
        var identity = Substitute.For<IIdentityService>();
        foreach (var pessoa in new[] { Mestre, Amigo, OutroAmigo, Jogador, Estranho })
        {
            _campanhas.Pessoas[pessoa.Id] = pessoa;
            identity.BuscarPerfilAsync(pessoa.Id, Arg.Any<CancellationToken>())
                .Returns(new DadosDoPerfil(pessoa.Id, pessoa.Nome, pessoa.Nick, $"{pessoa.Nick}@rpguild.app", null));
        }

        foreach (var amigo in new[] { Amigo, OutroAmigo, Jogador })
        {
            var amizade = Amizade.Pedir(Mestre.Id, amigo.Id, Agora.AddDays(-10));
            amizade.Aceitar(Agora.AddDays(-9));
            _amizades.Amizades.Add(amizade);
        }

        _campanhas.Campanhas.Add(_campanha);
        _campanhas.Membros.Add(new MembroDaCampanha(_campanha.Id, Jogador.Id, Agora.AddHours(-5)));

        var acessos = new AcessoACampanha(_campanhas);
        _service = new ConviteService(
            _convites,
            _campanhas,
            _amizades,
            acessos,
            new GradeDeCampanhas(_grade),
            new TransacaoFalsa(),
            identity,
            Substitute.For<IStorageService>(),
            Substitute.For<INotificador>(),
            new EventosDaCampanha(Substitute.For<ICampanhaTempoReal>(), NullLogger<EventosDaCampanha>.Instance),
            new FakeTimeProvider(Agora));
    }

    [Fact]
    public async Task ConvidarAsync_Amigos_CriaUmConvitePendentePorAmigo()
    {
        // Act
        var resultado = await ConvidarAsync(Mestre, Amigo, OutroAmigo);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        _convites.Convites.Select(c => (c.CampaignId, c.InvitedBy, c.UserId, c.Status, c.CreatedAt))
            .ShouldBe(
            [
                (_campanha.Id, Mestre.Id, Amigo.Id, StatusDoConvite.Pendente, Agora),
                (_campanha.Id, Mestre.Id, OutroAmigo.Id, StatusDoConvite.Pendente, Agora),
            ]);
        resultado.Valor!.Select(c => c.Convidado.UserId).ShouldBe([Amigo.Id, OutroAmigo.Id]);
    }

    [Fact]
    public async Task ConvidarAsync_NaoAmigo_Retorna400ComOMotivoENaoCriaNada()
    {
        // Act
        var resultado = await ConvidarAsync(Mestre, Amigo, Estranho);

        // Assert
        resultado.Erro!.Tipo.ShouldBe(TipoErro.Validacao);
        Convidados(resultado.Erro).ShouldBe([new ConvidadoInvalido(Estranho.Id, MotivosDeConvite.NaoAmigo)]);
        _convites.Convites.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConvidarAsync_OProprioMestre_Retorna400()
    {
        // Act
        var resultado = await ConvidarAsync(Mestre, Mestre);

        // Assert
        resultado.Erro!.Tipo.ShouldBe(TipoErro.Validacao);
        Convidados(resultado.Erro).ShouldBe([new ConvidadoInvalido(Mestre.Id, MotivosDeConvite.Mestre)]);
    }

    [Fact]
    public async Task ConvidarAsync_JaJogador_Retorna409()
    {
        // Act
        var resultado = await ConvidarAsync(Mestre, Amigo, Jogador);

        // Assert
        resultado.Erro!.Tipo.ShouldBe(TipoErro.Conflito);
        Convidados(resultado.Erro).ShouldBe([new ConvidadoInvalido(Jogador.Id, MotivosDeConvite.JaJogador)]);
        _convites.Convites.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConvidarAsync_JaTemConvitePendente_Retorna409()
    {
        // Arrange
        _convites.Convites.Add(ConviteDeCampanha.Criar(_campanha.Id, Mestre.Id, Amigo.Id, Agora.AddHours(-1)));

        // Act
        var resultado = await ConvidarAsync(Mestre, Amigo);

        // Assert
        resultado.Erro!.Tipo.ShouldBe(TipoErro.Conflito);
        Convidados(resultado.Erro).ShouldBe([new ConvidadoInvalido(Amigo.Id, MotivosDeConvite.JaConvidado)]);
        _convites.Convites.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConvidarAsync_ErrosDe400EDe409Juntos_Retorna400ComTodosOsMotivos()
    {
        // Act
        var resultado = await ConvidarAsync(Mestre, Jogador, Estranho);

        // Assert
        resultado.Erro!.Tipo.ShouldBe(TipoErro.Validacao);
        Convidados(resultado.Erro).ShouldBe(
        [
            new ConvidadoInvalido(Jogador.Id, MotivosDeConvite.JaJogador),
            new ConvidadoInvalido(Estranho.Id, MotivosDeConvite.NaoAmigo),
        ]);
    }

    [Theory]
    [InlineData(StatusDoConvite.Recusado)]
    [InlineData(StatusDoConvite.Cancelado)]
    public async Task ConvidarAsync_DepoisDeRecusadoOuCancelado_CriaUmConviteNovo(StatusDoConvite status)
    {
        // Arrange
        var antigo = await ConviteComStatusAsync(Amigo, status);

        // Act
        var resultado = await ConvidarAsync(Mestre, Amigo);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        _convites.Convites.Count.ShouldBe(2);
        antigo.Status.ShouldBe(status);
        _convites.Convites.Single(c => c.Id != antigo.Id).Status.ShouldBe(StatusDoConvite.Pendente);
    }

    [Fact]
    public async Task ConvidarAsync_Jogador_Retorna403()
    {
        // Act
        var resultado = await ConvidarAsync(Jogador, Amigo);

        // Assert
        resultado.Erro.ShouldBe(ErrosDeCampanha.SoOMestre);
    }

    [Fact]
    public async Task ConvidarAsync_QuemNaoParticipa_Retorna404()
    {
        // Act
        var resultado = await ConvidarAsync(Estranho, Amigo);

        // Assert
        resultado.Erro.ShouldBe(ErrosDeCampanha.CampanhaNaoEncontrada);
    }

    [Fact]
    public async Task AceitarAsync_Convidado_ViraJogadorComACampanhaNoInicioDaGrade()
    {
        // Arrange
        var convite = await ConvitePendenteAsync(Amigo);
        var outra = Guid.NewGuid();
        _grade.Ordens[Amigo.Id] = [outra];

        // Act
        var resultado = await _service.AceitarAsync(Amigo.Id, convite.Id, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        convite.Status.ShouldBe(StatusDoConvite.Aceito);
        convite.RespondedAt.ShouldBe(Agora);
        _campanhas.Membros.ShouldContain(m => m.CampaignId == _campanha.Id && m.UserId == Amigo.Id && m.JoinedAt == Agora);
        _grade.Ordens[Amigo.Id].ShouldBe([_campanha.Id, outra]);
    }

    [Fact]
    public async Task RecusarAsync_Convidado_FicaRecusadoENaoViraJogador()
    {
        // Arrange
        var convite = await ConvitePendenteAsync(Amigo);

        // Act
        var resultado = await _service.RecusarAsync(Amigo.Id, convite.Id, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        convite.Status.ShouldBe(StatusDoConvite.Recusado);
        convite.RespondedAt.ShouldBe(Agora);
        _campanhas.Membros.ShouldNotContain(m => m.UserId == Amigo.Id);
    }

    [Theory]
    [MemberData(nameof(QuemNaoEOConvidado))]
    public async Task AceitarERecusar_QuemNaoEOConvidado_Retorna404(Guid userId)
    {
        // Arrange
        var convite = await ConvitePendenteAsync(Amigo);

        // Act
        var aceite = await _service.AceitarAsync(userId, convite.Id, CancellationToken.None);
        var recusa = await _service.RecusarAsync(userId, convite.Id, CancellationToken.None);

        // Assert
        aceite.Erro.ShouldBe(ErrosDeConvite.ConviteNaoEncontrado);
        recusa.Erro.ShouldBe(ErrosDeConvite.ConviteNaoEncontrado);
        convite.Status.ShouldBe(StatusDoConvite.Pendente);
    }

    [Fact]
    public async Task AceitarERecusar_ConviteInexistente_Retorna404()
    {
        // Act
        var aceite = await _service.AceitarAsync(Amigo.Id, Guid.NewGuid(), CancellationToken.None);
        var recusa = await _service.RecusarAsync(Amigo.Id, Guid.NewGuid(), CancellationToken.None);

        // Assert
        aceite.Erro.ShouldBe(ErrosDeConvite.ConviteNaoEncontrado);
        recusa.Erro.ShouldBe(ErrosDeConvite.ConviteNaoEncontrado);
    }

    [Theory]
    [InlineData(StatusDoConvite.Aceito)]
    [InlineData(StatusDoConvite.Recusado)]
    [InlineData(StatusDoConvite.Cancelado)]
    public async Task AceitarRecusarECancelar_ConviteQueNaoEstaPendente_Retorna409ENaoMuda(StatusDoConvite status)
    {
        // Arrange
        var convite = await ConviteComStatusAsync(Amigo, status);
        var membros = _campanhas.Membros.Count;

        // Act
        var aceite = await _service.AceitarAsync(Amigo.Id, convite.Id, CancellationToken.None);
        var recusa = await _service.RecusarAsync(Amigo.Id, convite.Id, CancellationToken.None);
        var cancelamento = await _service.CancelarAsync(Mestre.Id, convite.Id, CancellationToken.None);

        // Assert
        aceite.Erro.ShouldBe(ErrosDeConvite.ConviteNaoPendente);
        recusa.Erro.ShouldBe(ErrosDeConvite.ConviteNaoPendente);
        cancelamento.Erro.ShouldBe(ErrosDeConvite.ConviteNaoPendente);
        convite.Status.ShouldBe(status);
        _campanhas.Membros.Count.ShouldBe(membros);
    }

    [Fact]
    public async Task CancelarAsync_Mestre_FicaCancelado()
    {
        // Arrange
        var convite = await ConvitePendenteAsync(Amigo);

        // Act
        var resultado = await _service.CancelarAsync(Mestre.Id, convite.Id, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        convite.Status.ShouldBe(StatusDoConvite.Cancelado);
        convite.RespondedAt.ShouldBe(Agora);
    }

    [Fact]
    public async Task CancelarAsync_OConvidado_Retorna403()
    {
        // Arrange
        var convite = await ConvitePendenteAsync(Amigo);

        // Act
        var resultado = await _service.CancelarAsync(Amigo.Id, convite.Id, CancellationToken.None);

        // Assert
        resultado.Erro.ShouldBe(ErrosDeCampanha.SoOMestre);
        convite.Status.ShouldBe(StatusDoConvite.Pendente);
    }

    [Fact]
    public async Task CancelarAsync_Jogador_Retorna403()
    {
        // Arrange
        var convite = await ConvitePendenteAsync(Amigo);

        // Act
        var resultado = await _service.CancelarAsync(Jogador.Id, convite.Id, CancellationToken.None);

        // Assert
        resultado.Erro.ShouldBe(ErrosDeCampanha.SoOMestre);
    }

    [Fact]
    public async Task CancelarAsync_QuemNaoParticipa_Retorna404()
    {
        // Arrange
        var convite = await ConvitePendenteAsync(Amigo);

        // Act
        var resultado = await _service.CancelarAsync(Estranho.Id, convite.Id, CancellationToken.None);

        // Assert
        resultado.Erro.ShouldBe(ErrosDeConvite.ConviteNaoEncontrado);
    }

    [Theory]
    [InlineData(StatusDoConvite.Aceito)]
    [InlineData(StatusDoConvite.Recusado)]
    [InlineData(StatusDoConvite.Cancelado)]
    public void Convite_ResponderQuandoNaoEstaPendente_LancaInvalidOperationException(StatusDoConvite status)
    {
        // Arrange
        var convite = ConviteDeCampanha.Criar(_campanha.Id, Mestre.Id, Amigo.Id, Agora);
        Mudar(convite, status);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => convite.Aceitar(Agora));
        Should.Throw<InvalidOperationException>(() => convite.Recusar(Agora));
        Should.Throw<InvalidOperationException>(() => convite.Cancelar(Agora));
    }

    public static TheoryData<Guid> QuemNaoEOConvidado() => [Mestre.Id, Jogador.Id, Estranho.Id, OutroAmigo.Id];

    private static IReadOnlyList<ConvidadoInvalido> Convidados(Erro erro) =>
        (IReadOnlyList<ConvidadoInvalido>)erro.Extensoes![ErrosDeConvite.ExtensaoConvidados];

    private static void Mudar(ConviteDeCampanha convite, StatusDoConvite status)
    {
        switch (status)
        {
            case StatusDoConvite.Aceito:
                convite.Aceitar(Agora);
                break;
            case StatusDoConvite.Recusado:
                convite.Recusar(Agora);
                break;
            case StatusDoConvite.Cancelado:
                convite.Cancelar(Agora);
                break;
        }
    }

    private Task<Resultado<ConviteDaCampanhaResponse[]>> ConvidarAsync(PessoaGuardada quem, params PessoaGuardada[] convidados) =>
        _service.ConvidarAsync(quem.Id, _campanha.Id, new ConvidarRequest([.. convidados.Select(c => c.Id)]), CancellationToken.None);

    private Task<ConviteDeCampanha> ConvitePendenteAsync(PessoaGuardada convidado) => ConviteComStatusAsync(convidado, StatusDoConvite.Pendente);

    private Task<ConviteDeCampanha> ConviteComStatusAsync(PessoaGuardada convidado, StatusDoConvite status)
    {
        var convite = ConviteDeCampanha.Criar(_campanha.Id, Mestre.Id, convidado.Id, Agora.AddHours(-1));
        Mudar(convite, status);
        _convites.Convites.Add(convite);
        return Task.FromResult(convite);
    }
}
