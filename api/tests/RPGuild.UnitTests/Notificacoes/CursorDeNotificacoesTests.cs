using System.Buffers.Text;
using System.Text;
using RPGuild.Application.Notificacoes;

namespace RPGuild.UnitTests.Notificacoes;

public class CursorDeNotificacoesTests
{
    private static readonly CursorDeNotificacoes Cursor = new(
        new DateTimeOffset(2026, 11, 26, 18, 0, 0, TimeSpan.Zero).AddTicks(1_234_560), Guid.NewGuid());

    [Fact]
    public void Codificar_EDecodificar_VoltaOMesmoCursor()
    {
        // Act
        var texto = Cursor.Codificar();

        // Assert
        CursorDeNotificacoes.Decodificar(texto).ShouldBe(Cursor);
    }

    [Fact]
    public void Codificar_Cursor_EhSeguroParaUrlEOpaco()
    {
        // Act
        var texto = Cursor.Codificar();

        // Assert
        texto.ShouldMatch("^[A-Za-z0-9_-]+$");
        texto.ShouldNotContain(Cursor.Id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao é base64!")]
    [InlineData("lixo")]
    public void Decodificar_TextoInvalido_RetornaNullSemExcecao(string? texto)
    {
        // Act & Assert
        CursorDeNotificacoes.Decodificar(texto).ShouldBeNull();
    }

    [Theory]
    [InlineData("sem-separador")]
    [InlineData(":")]
    [InlineData("abc:00000000-0000-0000-0000-000000000000")]
    [InlineData("638999999999999999:nao-e-guid")]
    [InlineData("-1:00000000-0000-0000-0000-000000000000")]
    [InlineData("99999999999999999999999:00000000-0000-0000-0000-000000000000")]
    [InlineData("3155378976000000000:00000000-0000-0000-0000-000000000000")]
    public void Decodificar_ConteudoAdulterado_RetornaNullSemExcecao(string conteudo)
    {
        // Arrange
        var texto = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(conteudo));

        // Act & Assert
        CursorDeNotificacoes.Decodificar(texto).ShouldBeNull();
    }
}
