using RPGuild.Application.Campanhas;

namespace RPGuild.UnitTests.Campanhas;

public class ConfirmacaoDeExclusaoTests
{
    private const string Nome = "A Mina Perdida de Phandelver";
    private const string Frase = "desejo excluir a campanha";

    [Fact]
    public void Confere_NomeEFraseIguais_RetornaTrue()
    {
        // Act & Assert
        ConfirmacaoDeExclusao.Confere(Nome, Nome, Frase).ShouldBeTrue();
    }

    [Theory]
    [InlineData("  A Mina Perdida de Phandelver  ", "desejo excluir a campanha")]
    [InlineData("A Mina Perdida de Phandelver", "   desejo excluir a campanha ")]
    [InlineData("\tA Mina Perdida de Phandelver\n", "\tdesejo excluir a campanha\n")]
    public void Confere_EspacosNasPontas_RetornaTrue(string nome, string frase)
    {
        // Act & Assert
        ConfirmacaoDeExclusao.Confere(Nome, nome, frase).ShouldBeTrue();
    }

    [Fact]
    public void Confere_NomeDaCampanhaComEspacosNasPontas_CompararPeloNomeSemEles()
    {
        // Act & Assert
        ConfirmacaoDeExclusao.Confere("  Taverna  ", "Taverna", Frase).ShouldBeTrue();
    }

    [Theory]
    [InlineData("a mina perdida de phandelver")]
    [InlineData("A MINA PERDIDA DE PHANDELVER")]
    [InlineData("A Mina Perdida de Phândelver")]
    [InlineData("A Mina  Perdida de Phandelver")]
    [InlineData("A Mina Perdida")]
    public void Confere_NomeDiferente_RetornaFalse(string nome)
    {
        // Act & Assert
        ConfirmacaoDeExclusao.Confere(Nome, nome, Frase).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Desejo excluir a campanha")]
    [InlineData("desejo excluir a campanha.")]
    [InlineData("desejo exclúir a campanha")]
    [InlineData("excluir a campanha")]
    [InlineData("")]
    public void Confere_FraseDiferente_RetornaFalse(string frase)
    {
        // Act & Assert
        ConfirmacaoDeExclusao.Confere(Nome, Nome, frase).ShouldBeFalse();
    }

    [Fact]
    public void Confere_ConfirmacoesNulas_RetornaFalse()
    {
        // Act & Assert
        ConfirmacaoDeExclusao.Confere(Nome, null, null).ShouldBeFalse();
    }
}
