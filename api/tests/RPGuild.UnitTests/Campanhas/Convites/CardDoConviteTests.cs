using RPGuild.Application.Campanhas;
using RPGuild.Domain.Campanhas;

namespace RPGuild.UnitTests.Campanhas.Convites;

public class CardDoConviteTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 27, 18, 0, 0, TimeSpan.Zero);

    private static readonly PessoaGuardada Mestre = new(Guid.NewGuid(), "Muirenn", "muirenn", "avatars/m/a.png");
    private static readonly PessoaGuardada Jogador = new(Guid.NewGuid(), "Selene", "selene", null);
    private static readonly PessoaGuardada Convidado = new(Guid.NewGuid(), "Thorin", "thorin", null);
    private static readonly PessoaGuardada Estranho = new(Guid.NewGuid(), "Gollum", "gollum", null);

    private static readonly Campanha Campanha = Campanha.Criar(
        Mestre.Id, 1, "A Mina Perdida", "Phandelver.", "campaigns/m/capa.png", "campaigns/m/banner.png", Agora.AddDays(-1));

    private static readonly CampanhaGuardada Guardada = new(
        Campanha,
        new SistemaDeJogo(1, "dnd2024", "D&D 5e (regras de 2024)", ativo: true),
        Mestre,
        [new JogadorGuardado(Jogador, Agora.AddHours(-5))]);

    [Fact]
    public void Montar_ConvidadoComConvitePendente_VeTudoEPodeResponder()
    {
        // Arrange
        var convite = ConviteDeCampanha.Criar(Campanha.Id, Mestre.Id, Convidado.Id, Agora);

        // Act
        var card = Montar(convite, Convidado).ShouldBeOfType<CardDoConviteResponse>();

        // Assert
        card.Id.ShouldBe(convite.Id);
        card.Status.ShouldBe("pendente");
        card.PodeResponder.ShouldBeTrue();
        card.Campanha.ShouldBe(new CampanhaDoCardResponse(
            Campanha.Id, "A Mina Perdida", "Phandelver.", new SistemaDaCampanhaResponse("dnd2024", "D&D 5e (regras de 2024)"),
            "url:campaigns/m/capa.png", "url:campaigns/m/banner.png"));
        card.Mestre.ShouldBe(new PessoaDaCampanhaResponse(Mestre.Id, "Muirenn", "muirenn", "url:avatars/m/a.png"));
        card.NaMesa.ShouldBe(
        [
            new PessoaDaCampanhaResponse(Mestre.Id, "Muirenn", "muirenn", "url:avatars/m/a.png"),
            new PessoaDaCampanhaResponse(Jogador.Id, "Selene", "selene", null),
        ]);
        card.ConvidadoPor.ShouldBe(new QuemConvidouResponse("Muirenn", "muirenn"));
        card.Para.ShouldBe(new ParaQuemResponse("thorin"));
        card.CriadoEm.ShouldBe(Agora);
    }

    [Theory]
    [InlineData("aceito")]
    [InlineData("recusado")]
    [InlineData("cancelado")]
    public void Montar_ConvidadoComConviteRespondido_VeOStatusENaoPodeResponder(string status)
    {
        // Arrange
        var convite = ConviteComStatus(status);

        // Act
        var card = Montar(convite, Convidado).ShouldBeOfType<CardDoConviteResponse>();

        // Assert
        card.Status.ShouldBe(status);
        card.PodeResponder.ShouldBeFalse();
    }

    [Theory]
    [InlineData("pendente")]
    [InlineData("aceito")]
    [InlineData("cancelado")]
    public void Montar_Mestre_VeTudoENuncaPodeResponder(string status)
    {
        // Arrange
        var convite = ConviteComStatus(status);

        // Act
        var card = Montar(convite, Mestre).ShouldBeOfType<CardDoConviteResponse>();

        // Assert
        card.Status.ShouldBe(status);
        card.PodeResponder.ShouldBeFalse();
        card.Campanha.Id.ShouldBe(Campanha.Id);
    }

    [Theory]
    [MemberData(nameof(Outros))]
    public void Montar_OutroUsuario_VeSoParaQuemEONaoPodeResponder(Guid quem)
    {
        // Arrange
        var convite = ConviteDeCampanha.Criar(Campanha.Id, Mestre.Id, Convidado.Id, Agora);

        // Act
        var card = CardDoConvite.Montar(convite, Guardada, Mestre, Convidado, quem, Url);

        // Assert
        card.ShouldBe(new CardDoConviteParaOutroResponse(convite.Id, PodeResponder: false, new ParaQuemResponse("thorin")));
    }

    public static TheoryData<Guid> Outros() => [Jogador.Id, Estranho.Id];

    private static string? Url(string? path) => path is null ? null : $"url:{path}";

    private static object Montar(ConviteDeCampanha convite, PessoaGuardada quem) =>
        CardDoConvite.Montar(convite, Guardada, Mestre, Convidado, quem.Id, Url);

    private static ConviteDeCampanha ConviteComStatus(string status)
    {
        var convite = ConviteDeCampanha.Criar(Campanha.Id, Mestre.Id, Convidado.Id, Agora);
        switch (status)
        {
            case "aceito":
                convite.Aceitar(Agora);
                break;
            case "recusado":
                convite.Recusar(Agora);
                break;
            case "cancelado":
                convite.Cancelar(Agora);
                break;
        }

        return convite;
    }
}
