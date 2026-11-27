using RPGuild.Application.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

public class EmailTemplatesTests
{
    private const string Nome = "Gandalf Cinzento";
    private const string Email = "gandalf@teste.rpguild";

    [Fact]
    public void BoasVindas_Usuario_TemONomeEAssuntoEmPortugues()
    {
        // Act
        var mensagem = EmailTemplates.BoasVindas(Nome, Email);

        // Assert
        mensagem.Para.ShouldBe(Email);
        mensagem.Assunto.ShouldBe("Boas-vindas à RPGuild");
        mensagem.Texto.ShouldContain(Nome);
    }

    [Fact]
    public void RedefinicaoDeSenha_Link_TemOLinkCompletoEAValidadeDeUmaHora()
    {
        // Arrange
        const string link = "http://localhost:5173/redefinir-senha?token=abc_123-XYZ";

        // Act
        var mensagem = EmailTemplates.RedefinicaoDeSenha(Nome, Email, link);

        // Assert
        mensagem.Para.ShouldBe(Email);
        mensagem.Assunto.ShouldBe("Redefinição de senha da RPGuild");
        mensagem.Texto.ShouldContain(Nome);
        mensagem.Texto.ShouldContain(link);
        mensagem.Texto.ShouldContain("1 hora");
    }

    [Fact]
    public void ConfirmacaoDeNovoEmail_Link_VaiParaONovoEnderecoComOLinkEAValidadeDeUmaHora()
    {
        // Arrange
        const string novoEmail = "gandalf.branco@teste.rpguild";
        const string link = "http://localhost:5173/confirmar-email?token=abc_123-XYZ";

        // Act
        var mensagem = EmailTemplates.ConfirmacaoDeNovoEmail(Nome, novoEmail, link);

        // Assert
        mensagem.Para.ShouldBe(novoEmail);
        mensagem.Assunto.ShouldBe("Confirme seu novo e-mail na RPGuild");
        mensagem.Texto.ShouldContain(Nome);
        mensagem.Texto.ShouldContain(link);
        mensagem.Texto.ShouldContain("1 hora");
    }

    [Fact]
    public void EmailAlterado_EmailNovo_VaiParaOAntigoComONovoMascaradoEOrientaARecuperarAConta()
    {
        // Arrange
        const string novoEmail = "gandalf.branco@teste.rpguild";

        // Act
        var mensagem = EmailTemplates.EmailAlterado(Nome, Email, novoEmail);

        // Assert
        mensagem.Para.ShouldBe(Email);
        mensagem.Assunto.ShouldBe("O e-mail da sua conta na RPGuild foi alterado");
        mensagem.Texto.ShouldContain(Nome);
        mensagem.Texto.ShouldContain("ga***@teste.rpguild");
        mensagem.Texto.ShouldNotContain(novoEmail);
        mensagem.Texto.ShouldContain("Se não foi você");
        mensagem.Texto.ShouldContain("responda a este e-mail");
    }

    [Fact]
    public void SenhaAlterada_Usuario_AvisaEOrientaARedefinirASenhaSeNaoFoiAPessoa()
    {
        // Arrange
        const string site = "http://localhost:5173";

        // Act
        var mensagem = EmailTemplates.SenhaAlterada(Nome, Email, site);

        // Assert
        mensagem.Para.ShouldBe(Email);
        mensagem.Assunto.ShouldBe("A senha da sua conta na RPGuild foi alterada");
        mensagem.Texto.ShouldContain(Nome);
        mensagem.Texto.ShouldContain("Se não foi você");
        mensagem.Texto.ShouldContain("Esqueci minha senha");
        mensagem.Texto.ShouldContain(site);
    }

    [Fact]
    public void PedidoDeAmizade_QuemPediu_TemNomeArrobaNickEOLinkDaTaberna()
    {
        // Arrange
        const string link = "http://localhost:5173/taberna";

        // Act
        var mensagem = EmailTemplates.PedidoDeAmizade(Nome, Email, "Muirenn Folha-Alta", "muirenn", link);

        // Assert
        mensagem.Para.ShouldBe(Email);
        mensagem.Assunto.ShouldBe("Muirenn Folha-Alta te enviou um pedido de amizade na RPGuild");
        mensagem.Texto.ShouldContain(Nome);
        mensagem.Texto.ShouldContain("Muirenn Folha-Alta (@muirenn)");
        mensagem.Texto.ShouldContain(link);
    }

    [Fact]
    public void CampanhaExcluida_Mestre_TemONomeDaCampanhaEQuemExcluiu()
    {
        // Act
        var mensagem = EmailTemplates.CampanhaExcluida(Nome, Email, "A Mina Perdida", "Muirenn Folha-Alta");

        // Assert
        mensagem.Para.ShouldBe(Email);
        mensagem.Assunto.ShouldBe("A campanha A Mina Perdida foi excluída");
        mensagem.Texto.ShouldContain(Nome);
        mensagem.Texto.ShouldContain("Muirenn Folha-Alta excluiu a campanha A Mina Perdida");
    }

    [Fact]
    public void ConviteDeCampanha_Mestre_TemOAssuntoONomeArrobaNickEOLinkDoConvite()
    {
        // Arrange
        const string link = "http://localhost:5173/convites/0193";

        // Act
        var mensagem = EmailTemplates.ConviteDeCampanha(Nome, Email, "A Mina Perdida", "Muirenn Folha-Alta", "muirenn", link);

        // Assert
        mensagem.Para.ShouldBe(Email);
        mensagem.Assunto.ShouldBe("Muirenn Folha-Alta te convidou para a campanha A Mina Perdida");
        mensagem.Texto.ShouldContain(Nome);
        mensagem.Texto.ShouldContain("Muirenn Folha-Alta (@muirenn) te convidou para jogar a campanha A Mina Perdida na RPGuild.");
        mensagem.Texto.ShouldContain(link);
    }

    [Fact]
    public void JogadorEntrou_Jogador_TemOAssuntoONomeArrobaNickEOLinkDaCampanha()
    {
        // Arrange
        const string link = "http://localhost:5173/campanhas/0193";

        // Act
        var mensagem = EmailTemplates.JogadorEntrou(Nome, Email, "A Mina Perdida", "Thorin Escudo", "thorin", link);

        // Assert
        mensagem.Para.ShouldBe(Email);
        mensagem.Assunto.ShouldBe("Thorin Escudo entrou na campanha A Mina Perdida");
        mensagem.Texto.ShouldContain(Nome);
        mensagem.Texto.ShouldContain("Thorin Escudo (@thorin) entrou na campanha A Mina Perdida.");
        mensagem.Texto.ShouldContain(link);
    }
}
