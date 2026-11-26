using RPGuild.Domain.Contas;

namespace RPGuild.UnitTests.Contas;

public class NickTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("Mestre_01")]
    [InlineData("___")]
    [InlineData("a2345678901234567890")]
    public void Criar_ValorValido_GuardaOValor(string valor)
    {
        // Act
        var nick = Nick.Criar(valor);

        // Assert
        nick.Valor.ShouldBe(valor);
        Nick.EhValido(valor).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("a23456789012345678901")]
    [InlineData("com espaco")]
    [InlineData("mestre!")]
    [InlineData("mestre-01")]
    [InlineData("joão")]
    public void Criar_ValorInvalido_LancaArgumentException(string? valor)
    {
        // Act
        var criar = () => Nick.Criar(valor!);

        // Assert
        criar.ShouldThrow<ArgumentException>();
        Nick.EhValido(valor).ShouldBeFalse();
    }

    [Fact]
    public void Normalizado_ValorComMaiusculas_RetornaMinusculo()
    {
        // Arrange
        var nick = Nick.Criar("Mestre_DA_Mesa");

        // Act
        var normalizado = nick.Normalizado;

        // Assert
        normalizado.ShouldBe("mestre_da_mesa");
    }
}
