using RPGuild.Application.Taberna;

namespace RPGuild.UnitTests.Taberna;

public class AlvoDoPedidoTests
{
    [Theory]
    [InlineData("muirenn@rpguild.app", "muirenn@rpguild.app")]
    [InlineData("  Muirenn@RPGuild.app  ", "Muirenn@RPGuild.app")]
    [InlineData("a@b", "a@b")]
    public void Interpretar_EmailValido_BuscaPorEmail(string texto, string email)
    {
        // Act
        var alvo = AlvoDoPedido.Interpretar(texto);

        // Assert
        alvo.ShouldBe(new AlvoDoPedido(TipoDeAlvo.Email, email));
    }

    [Theory]
    [InlineData("muirenn")]
    [InlineData("@muirenn")]
    [InlineData("MuiRenn")]
    [InlineData("@MUIRENN")]
    [InlineData("  @Muirenn ")]
    public void Interpretar_NickComOuSemArroba_BuscaPeloNickNormalizado(string texto)
    {
        // Act
        var alvo = AlvoDoPedido.Interpretar(texto);

        // Assert
        alvo.ShouldBe(new AlvoDoPedido(TipoDeAlvo.Nick, "muirenn"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("@")]
    [InlineData("@@muirenn")]
    [InlineData("ab")]
    [InlineData("@ab")]
    [InlineData("nick com espaco")]
    [InlineData("muirenn@")]
    [InlineData("a@b@c")]
    [InlineData("nick-invalido")]
    public void Interpretar_VazioOuInvalido_RetornaNull(string? texto)
    {
        // Act
        var alvo = AlvoDoPedido.Interpretar(texto);

        // Assert
        alvo.ShouldBeNull();
    }
}
