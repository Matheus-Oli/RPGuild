using RPGuild.Application.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

public class MascaraDeEmailTests
{
    [Theory]
    [InlineData("matmig17@gmail.com", "ma***@gmail.com")]
    [InlineData("abc@rpguild.app", "ab***@rpguild.app")]
    [InlineData("ab@gmail.com", "***@gmail.com")]
    [InlineData("a@gmail.com", "***@gmail.com")]
    public void Mascarar_Email_MostraSoOsDoisPrimeirosCaracteresEODominio(string email, string esperado)
    {
        // Act
        var mascarado = MascaraDeEmail.Mascarar(email);

        // Assert
        mascarado.ShouldBe(esperado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("@gmail.com")]
    public void Mascarar_TextoQueNaoEhEmail_DevolveSoAMascara(string? texto)
    {
        // Act
        var mascarado = MascaraDeEmail.Mascarar(texto);

        // Assert
        mascarado.ShouldBe("***");
    }
}
