using Microsoft.Extensions.Time.Testing;
using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Application.Notificacoes;
using RPGuild.Application.Taberna;
using RPGuild.Application.Uploads;
using RPGuild.Domain.Taberna;
using RPGuild.UnitTests.Campanhas.Convites;

namespace RPGuild.UnitTests.Taberna;

public class AmizadeServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 26, 18, 0, 0, TimeSpan.Zero);

    private static readonly DadosDoPerfil Eu = new(Guid.NewGuid(), "Muirenn", "muirenn", "muirenn@rpguild.app", null);
    private static readonly DadosDoPerfil Outro = new(Guid.NewGuid(), "Thorin", "thorin", "thorin@rpguild.app", null);
    private static readonly DadosDoPerfil Terceiro = new(Guid.NewGuid(), "Bilbo", "bilbo", "bilbo@rpguild.app", null);

    private readonly AmizadeRepositoryFalso _repositorio = new();
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly FakeTimeProvider _relogio = new(Agora);
    private readonly AmizadeService _service;

    public AmizadeServiceTests()
    {
        foreach (var perfil in new[] { Eu, Outro, Terceiro })
        {
            _identity.BuscarPerfilAsync(perfil.Id, Arg.Any<CancellationToken>()).Returns(perfil);
            _identity.BuscarPorNickAsync(perfil.Nick, Arg.Any<CancellationToken>()).Returns(perfil);
        }

        _service = new AmizadeService(
            _repositorio,
            _identity,
            Substitute.For<IStorageService>(),
            Substitute.For<INotificador>(),
            Substitute.For<IConvitesEntreAmigos>(),
            Substitute.For<ICampanhasEmComum>(),
            new TransacaoFalsa(),
            _relogio);
    }

    [Fact]
    public async Task EnviarAsync_SemPedidoEntreOsDois_CriaPedidoPendente()
    {
        // Act
        var resultado = await EnviarAsync(Eu, Outro);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        var pedido = _repositorio.Amizades.ShouldHaveSingleItem();
        pedido.RequesterId.ShouldBe(Eu.Id);
        pedido.AddresseeId.ShouldBe(Outro.Id);
        pedido.Status.ShouldBe(StatusDaAmizade.Pendente);
        pedido.CreatedAt.ShouldBe(Agora);
        pedido.RespondedAt.ShouldBeNull();
        resultado.Valor!.Id.ShouldBe(pedido.Id);
        resultado.Valor.Para.UserId.ShouldBe(Outro.Id);
    }

    [Fact]
    public async Task EnviarAsync_ParaSiMesmo_RetornaErroSemCriarPedido()
    {
        // Act
        var resultado = await EnviarAsync(Eu, Eu);

        // Assert
        resultado.Erro.ShouldBe(ErrosDaTaberna.PedidoParaSiMesmo);
        _repositorio.Amizades.ShouldBeEmpty();
    }

    [Fact]
    public async Task EnviarAsync_UsuarioNaoEncontrado_RetornaErroSemCriarPedido()
    {
        // Act
        var resultado = await _service.EnviarAsync(Eu.Id, new EnviarPedidoRequest("ninguem_aqui"), CancellationToken.None);

        // Assert
        resultado.Erro.ShouldBe(ErrosDaTaberna.UsuarioNaoEncontrado);
        _repositorio.Amizades.ShouldBeEmpty();
    }

    [Fact]
    public async Task EnviarAsync_JaSaoAmigos_RetornaConflito()
    {
        // Arrange
        await AmizadeAceitaAsync(Outro, Eu);

        // Act
        var resultado = await EnviarAsync(Eu, Outro);

        // Assert
        resultado.Erro.ShouldBe(ErrosDaTaberna.JaSaoAmigos);
    }

    [Fact]
    public async Task EnviarAsync_JaTemPedidoPendenteMeu_RetornaConflito()
    {
        // Arrange
        await EnviarAsync(Eu, Outro);

        // Act
        var resultado = await EnviarAsync(Eu, Outro);

        // Assert
        resultado.Erro.ShouldBe(ErrosDaTaberna.PedidoJaEnviado);
        _repositorio.Amizades.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task EnviarAsync_JaTemPedidoPendenteDele_RetornaConflitoSemAceitar()
    {
        // Arrange
        await EnviarAsync(Outro, Eu);

        // Act
        var resultado = await EnviarAsync(Eu, Outro);

        // Assert
        resultado.Erro.ShouldBe(ErrosDaTaberna.PedidoRecebidoPendente);
        resultado.Erro!.Detalhe.ShouldBe("Essa pessoa já te enviou um pedido. Aceite na sua lista.");
        _repositorio.Amizades.ShouldHaveSingleItem().Status.ShouldBe(StatusDaAmizade.Pendente);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EnviarAsync_PedidoRecusadoAntes_SubstituiALinhaPorUmPedidoPendente(bool quemPediuAntesFuiEu)
    {
        // Arrange: o pedido recusado pode ter sido de qualquer um dos dois.
        var (antes, recusou) = quemPediuAntesFuiEu ? (Eu, Outro) : (Outro, Eu);
        var recusado = (await EnviarAsync(antes, recusou)).Valor!;
        (await _service.RecusarAsync(recusou.Id, recusado.Id, CancellationToken.None)).Sucesso.ShouldBeTrue();
        _relogio.Advance(TimeSpan.FromMinutes(5));

        // Act
        var resultado = await EnviarAsync(Eu, Outro);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        var pedido = _repositorio.Amizades.ShouldHaveSingleItem();
        pedido.RequesterId.ShouldBe(Eu.Id);
        pedido.AddresseeId.ShouldBe(Outro.Id);
        pedido.Status.ShouldBe(StatusDaAmizade.Pendente);
        pedido.CreatedAt.ShouldBe(Agora.AddMinutes(5));
        pedido.RespondedAt.ShouldBeNull();
    }

    [Fact]
    public async Task AceitarAsync_DestinatarioDePedidoPendente_AceitaComDataDaResposta()
    {
        // Arrange
        var pedido = (await EnviarAsync(Outro, Eu)).Valor!;
        _relogio.Advance(TimeSpan.FromHours(1));

        // Act
        var resultado = await _service.AceitarAsync(Eu.Id, pedido.Id, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        var amizade = _repositorio.Amizades.ShouldHaveSingleItem();
        amizade.Status.ShouldBe(StatusDaAmizade.Aceita);
        amizade.RespondedAt.ShouldBe(Agora.AddHours(1));
    }

    [Fact]
    public async Task RecusarAsync_DestinatarioDePedidoPendente_RecusaComDataDaResposta()
    {
        // Arrange
        var pedido = (await EnviarAsync(Outro, Eu)).Valor!;

        // Act
        var resultado = await _service.RecusarAsync(Eu.Id, pedido.Id, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        var amizade = _repositorio.Amizades.ShouldHaveSingleItem();
        amizade.Status.ShouldBe(StatusDaAmizade.Recusada);
        amizade.RespondedAt.ShouldBe(Agora);
    }

    [Theory]
    [InlineData(nameof(AmizadeService.AceitarAsync))]
    [InlineData(nameof(AmizadeService.RecusarAsync))]
    public async Task Responder_QuemNaoEODestinatario_RetornaPedidoNaoEncontrado(string acao)
    {
        // Arrange
        var pedido = (await EnviarAsync(Outro, Eu)).Valor!;

        // Act: quem enviou e um terceiro tentam responder.
        var peloRemetente = await ResponderAsync(acao, Outro.Id, pedido.Id);
        var porTerceiro = await ResponderAsync(acao, Terceiro.Id, pedido.Id);

        // Assert
        peloRemetente.Erro.ShouldBe(ErrosDaTaberna.PedidoNaoEncontrado);
        porTerceiro.Erro.ShouldBe(ErrosDaTaberna.PedidoNaoEncontrado);
        _repositorio.Amizades.ShouldHaveSingleItem().Status.ShouldBe(StatusDaAmizade.Pendente);
    }

    [Theory]
    [InlineData(nameof(AmizadeService.AceitarAsync))]
    [InlineData(nameof(AmizadeService.RecusarAsync))]
    [InlineData(nameof(AmizadeService.CancelarAsync))]
    public async Task Responder_PedidoInexistente_RetornaPedidoNaoEncontrado(string acao)
    {
        // Act
        var resultado = await ResponderAsync(acao, Eu.Id, Guid.NewGuid());

        // Assert
        resultado.Erro.ShouldBe(ErrosDaTaberna.PedidoNaoEncontrado);
    }

    [Theory]
    [InlineData(nameof(AmizadeService.AceitarAsync), StatusDaAmizade.Aceita)]
    [InlineData(nameof(AmizadeService.AceitarAsync), StatusDaAmizade.Recusada)]
    [InlineData(nameof(AmizadeService.RecusarAsync), StatusDaAmizade.Aceita)]
    [InlineData(nameof(AmizadeService.RecusarAsync), StatusDaAmizade.Recusada)]
    public async Task Responder_PedidoQueNaoEstaPendente_RetornaConflito(string acao, StatusDaAmizade status)
    {
        // Arrange
        var pedido = await PedidoRespondidoAsync(Outro, Eu, status);

        // Act
        var resultado = await ResponderAsync(acao, Eu.Id, pedido);

        // Assert
        resultado.Erro.ShouldBe(ErrosDaTaberna.PedidoNaoPendente);
    }

    [Fact]
    public async Task CancelarAsync_QuemEnviouPedidoPendente_ApagaOPedido()
    {
        // Arrange
        var pedido = (await EnviarAsync(Eu, Outro)).Valor!;

        // Act
        var resultado = await _service.CancelarAsync(Eu.Id, pedido.Id, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        _repositorio.Amizades.ShouldBeEmpty();
    }

    [Fact]
    public async Task CancelarAsync_QuemNaoEnviou_RetornaPedidoNaoEncontrado()
    {
        // Arrange
        var pedido = (await EnviarAsync(Eu, Outro)).Valor!;

        // Act: o destinatário e um terceiro tentam cancelar.
        var peloDestinatario = await _service.CancelarAsync(Outro.Id, pedido.Id, CancellationToken.None);
        var porTerceiro = await _service.CancelarAsync(Terceiro.Id, pedido.Id, CancellationToken.None);

        // Assert
        peloDestinatario.Erro.ShouldBe(ErrosDaTaberna.PedidoNaoEncontrado);
        porTerceiro.Erro.ShouldBe(ErrosDaTaberna.PedidoNaoEncontrado);
        _repositorio.Amizades.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(StatusDaAmizade.Aceita)]
    [InlineData(StatusDaAmizade.Recusada)]
    public async Task CancelarAsync_PedidoQueNaoEstaPendente_RetornaConflito(StatusDaAmizade status)
    {
        // Arrange
        var pedido = await PedidoRespondidoAsync(Eu, Outro, status);

        // Act
        var resultado = await _service.CancelarAsync(Eu.Id, pedido, CancellationToken.None);

        // Assert
        resultado.Erro.ShouldBe(ErrosDaTaberna.PedidoNaoPendente);
        _repositorio.Amizades.ShouldHaveSingleItem();
    }

    private Task<Resultado<PedidoEnviadoResponse>> EnviarAsync(DadosDoPerfil de, DadosDoPerfil para) =>
        _service.EnviarAsync(de.Id, new EnviarPedidoRequest(para.Nick), CancellationToken.None);

    private async Task AmizadeAceitaAsync(DadosDoPerfil de, DadosDoPerfil para) =>
        await PedidoRespondidoAsync(de, para, StatusDaAmizade.Aceita);

    private async Task<Guid> PedidoRespondidoAsync(DadosDoPerfil de, DadosDoPerfil para, StatusDaAmizade status)
    {
        var pedido = (await EnviarAsync(de, para)).Valor!;
        var resposta = status == StatusDaAmizade.Aceita
            ? await _service.AceitarAsync(para.Id, pedido.Id, CancellationToken.None)
            : await _service.RecusarAsync(para.Id, pedido.Id, CancellationToken.None);
        resposta.Sucesso.ShouldBeTrue();
        return pedido.Id;
    }

    private Task<Resultado<Vazio>> ResponderAsync(string acao, Guid userId, Guid pedidoId) => acao switch
    {
        nameof(AmizadeService.AceitarAsync) => _service.AceitarAsync(userId, pedidoId, CancellationToken.None),
        nameof(AmizadeService.RecusarAsync) => _service.RecusarAsync(userId, pedidoId, CancellationToken.None),
        _ => _service.CancelarAsync(userId, pedidoId, CancellationToken.None),
    };
}
