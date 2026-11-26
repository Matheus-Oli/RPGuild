using System.Buffers.Text;
using System.Text;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class TokenDeRedefinicaoTests
{
    // Formato típico do token do Identity: Base64 comum, com "+", "/" e "=".
    private const string TokenIdentity = "CfDJ8Ab+cD/eF==";

    [Fact]
    public void Decodificar_TextoCodificado_DevolveOMesmoUserIdEToken()
    {
        // Arrange
        var original = new TokenDeRedefinicao(Guid.NewGuid(), TokenIdentity);

        // Act
        var decodificado = TokenDeRedefinicao.Decodificar(original.Codificar());

        // Assert
        decodificado.ShouldBe(original);
    }

    [Fact]
    public void Codificar_TokenComCaracteresDeBase64_GeraTextoSeguroParaUrl()
    {
        // Arrange
        var token = new TokenDeRedefinicao(Guid.NewGuid(), TokenIdentity);

        // Act
        var texto = token.Codificar();

        // Assert
        Uri.EscapeDataString(texto).ShouldBe(texto);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("não é base64!")]
    [InlineData("YWJj$")]
    public void Decodificar_TextoInvalido_DevolveNull(string? texto)
    {
        // Act
        var resultado = TokenDeRedefinicao.Decodificar(texto);

        // Assert
        resultado.ShouldBeNull();
    }

    [Theory]
    [InlineData("sem-dois-pontos")]
    [InlineData("nao-e-guid:token")]
    [InlineData("00000000-0000-0000-0000-000000000000:")]
    [InlineData(":token")]
    public void Decodificar_ConteudoForaDoFormato_DevolveNull(string conteudo)
    {
        // Arrange
        var texto = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(conteudo));

        // Act
        var resultado = TokenDeRedefinicao.Decodificar(texto);

        // Assert
        resultado.ShouldBeNull();
    }
}
