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
}
