using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Application.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

public class NotificadorTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 26, 18, 0, 0, TimeSpan.Zero);

    private static readonly DadosDoPerfil Usuario = new(Guid.NewGuid(), "Muirenn", "muirenn", "muirenn@rpguild.app", null);

    private readonly NotificacaoRepositoryFalso _repositorio = new();
    private readonly FilaDeEmailFalsa _fila = new();
    private readonly IEmailSender _envioDireto = Substitute.For<IEmailSender>();
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly Notificador _notificador;

    public NotificadorTests()
    {
        _identity.BuscarPerfilAsync(Usuario.Id, Arg.Any<CancellationToken>()).Returns(Usuario);

        _notificador = new Notificador(
            _repositorio,
            _fila,
            new EnvioDeEmail(_envioDireto, NullLogger<EnvioDeEmail>.Instance),
            _identity,
            new EventosDoSininho(Substitute.For<INotificacoesTempoReal>(), _repositorio, NullLogger<EventosDoSininho>.Instance),
            new FrontOptions { BaseUrl = "http://localhost:5173" },
            new FakeTimeProvider(Agora));
    }

    [Fact]
    public async Task NotificarAsync_TipoComSininho_GravaANotificacaoNaoLida()
    {
        // Act
        await _notificador.NotificarAsync(Usuario.Id, new BoasVindas(), CancellationToken.None);

        // Assert
        var notificacao = _repositorio.Notificacoes.ShouldHaveSingleItem();
        notificacao.UserId.ShouldBe(Usuario.Id);
        notificacao.Tipo.ShouldBe(TiposDeNotificacao.BoasVindas);
        notificacao.Dados.ShouldBe("{}");
        notificacao.CreatedAt.ShouldBe(Agora);
        notificacao.LidaEm.ShouldBeNull();
        _repositorio.VezesSalvo.ShouldBe(1);
    }

    [Fact]
    public async Task NotificarAsync_EmailComum_VaiParaAFilaComOEmailDoUsuario()
    {
        // Act
        await _notificador.NotificarAsync(Usuario.Id, new BoasVindas(), CancellationToken.None);

        // Assert
        var mensagem = _fila.Mensagens.ShouldHaveSingleItem();
        mensagem.Para.ShouldBe(Usuario.Email);
        mensagem.Assunto.ShouldBe("Boas-vindas à RPGuild");
        await _envioDireto.DidNotReceiveWithAnyArgs().EnviarAsync(default!, default);
    }

    [Theory]
    [InlineData(TiposDeNotificacao.RedefinicaoDeSenha)]
    [InlineData(TiposDeNotificacao.ConfirmacaoDeEmail)]
    public async Task NotificarAsync_EmailComToken_SaiDiretoSemPassarPelaFilaNemPeloSininho(string tipo)
    {
        // Arrange
        const string link = "http://localhost:5173/rota?token=token-secreto";
        Aviso aviso = tipo == TiposDeNotificacao.RedefinicaoDeSenha ? new RedefinicaoDeSenha(link) : new ConfirmacaoDeEmail(link);

        // Act
        await _notificador.NotificarAsync(Usuario.Id, aviso, "novo@rpguild.app", CancellationToken.None);

        // Assert
        await _envioDireto.Received(1).EnviarAsync(
            Arg.Is<EmailMessage>(m => m.Para == "novo@rpguild.app" && m.Texto.Contains(link)), Arg.Any<CancellationToken>());
        _fila.Mensagens.ShouldBeEmpty();
        _repositorio.Notificacoes.ShouldBeEmpty();
    }

    [Fact]
    public async Task NotificarAsync_EmailAlterado_VaiParaOEmailDestinoEnaoParaODoUsuario()
    {
        // Act
        await _notificador.NotificarAsync(Usuario.Id, new EmailAlterado("muirenn.nova@rpguild.app"), "antigo@rpguild.app", CancellationToken.None);

        // Assert
        var mensagem = _fila.Mensagens.ShouldHaveSingleItem();
        mensagem.Para.ShouldBe("antigo@rpguild.app");
        mensagem.Assunto.ShouldBe("O e-mail da sua conta na RPGuild foi alterado");
        _repositorio.Notificacoes.ShouldBeEmpty();
    }

    [Fact]
    public async Task NotificarAsync_SenhaAlterada_SoEmailPelaFila()
    {
        // Act
        await _notificador.NotificarAsync(Usuario.Id, new SenhaAlterada(), CancellationToken.None);

        // Assert
        _fila.Mensagens.ShouldHaveSingleItem().Assunto.ShouldBe("A senha da sua conta na RPGuild foi alterada");
        _repositorio.Notificacoes.ShouldBeEmpty();
    }

    [Fact]
    public async Task NotificarAsync_AmizadeRecebida_GravaOsDadosComOsNomesDaHoraEMandaOEmail()
    {
        // Arrange
        var pedidoId = Guid.NewGuid();
        var deUserId = Guid.NewGuid();

        // Act
        await _notificador.NotificarAsync(Usuario.Id, new AmizadeRecebida(pedidoId, deUserId, "Thorin Escudo", "thorin"), CancellationToken.None);

        // Assert: o nome e o nick ficam como eram agora, mesmo que mudem depois.
        var notificacao = _repositorio.Notificacoes.ShouldHaveSingleItem();
        notificacao.Tipo.ShouldBe(TiposDeNotificacao.AmizadeRecebida);
        var dados = JsonDocument.Parse(notificacao.Dados).RootElement;
        dados.GetProperty("pedidoId").GetGuid().ShouldBe(pedidoId);
        dados.GetProperty("deUserId").GetGuid().ShouldBe(deUserId);
        dados.GetProperty("deNome").GetString().ShouldBe("Thorin Escudo");
        dados.GetProperty("deNick").GetString().ShouldBe("thorin");
        dados.EnumerateObject().Count().ShouldBe(4);

        var mensagem = _fila.Mensagens.ShouldHaveSingleItem();
        mensagem.Para.ShouldBe(Usuario.Email);
        mensagem.Texto.ShouldContain("Thorin Escudo (@thorin)");
        mensagem.Texto.ShouldContain("http://localhost:5173/taberna");
    }

    [Theory]
    [InlineData(TiposDeNotificacao.AmizadeAceita)]
    [InlineData(TiposDeNotificacao.AmizadeRecusada)]
    public async Task NotificarAsync_RespostaDeAmizade_SoSininhoComOsDadosDeQuemRespondeu(string tipo)
    {
        // Arrange
        var quem = Guid.NewGuid();
        Aviso aviso = tipo == TiposDeNotificacao.AmizadeAceita
            ? new AmizadeAceita(quem, "Bilbo", "bilbo")
            : new AmizadeRecusada(quem, "Bilbo", "bilbo");

        // Act
        await _notificador.NotificarAsync(Usuario.Id, aviso, CancellationToken.None);

        // Assert
        var notificacao = _repositorio.Notificacoes.ShouldHaveSingleItem();
        notificacao.Tipo.ShouldBe(tipo);
        notificacao.Dados.ShouldBe($$"""{"userId":"{{quem}}","nome":"Bilbo","nick":"bilbo"}""");
        _fila.Mensagens.ShouldBeEmpty();
        await _envioDireto.DidNotReceiveWithAnyArgs().EnviarAsync(default!, default);
    }
}
