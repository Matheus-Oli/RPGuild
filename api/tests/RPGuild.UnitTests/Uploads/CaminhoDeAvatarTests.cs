using System.Text.RegularExpressions;
using RPGuild.Application.Uploads;

namespace RPGuild.UnitTests.Uploads;

public class CaminhoDeAvatarTests
{
    private static readonly Guid UserId = Guid.Parse("0198f3a1-7c2e-7a4b-9d10-1234567890ab");

    [Fact]
    public void Gerar_ContentTypePermitido_FicaNaPastaDoUsuarioComUmGuidNoNome()
    {
        // Act
        var path = CaminhoDeAvatar.Gerar(UserId, "image/png");

        // Assert
        path.ShouldMatch($"^avatars/{UserId}/[0-9a-f]{{8}}-[0-9a-f]{{4}}-[0-9a-f]{{4}}-[0-9a-f]{{4}}-[0-9a-f]{{12}}\\.png$");
    }

    [Fact]
    public void Gerar_DuasVezes_GeraNomesDiferentes()
    {
        // Act
        var primeiro = CaminhoDeAvatar.Gerar(UserId, "image/png");
        var segundo = CaminhoDeAvatar.Gerar(UserId, "image/png");

        // Assert
        primeiro.ShouldNotBe(segundo);
    }

    [Theory]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/png", "png")]
    [InlineData("image/webp", "webp")]
    [InlineData("image/gif", "gif")]
    [InlineData("IMAGE/PNG", "png")]
    public void Gerar_ContentType_UsaAExtensaoDoTipo(string contentType, string extensao)
    {
        // Act
        var path = CaminhoDeAvatar.Gerar(UserId, contentType);

        // Assert
        Regex.Match(path, @"\.([a-z]+)$").Groups[1].Value.ShouldBe(extensao);
    }

    [Fact]
    public void Gerar_ContentTypeForaDaLista_LancaArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => CaminhoDeAvatar.Gerar(UserId, "image/svg+xml"));
    }

    [Fact]
    public void PertenceAo_PathGeradoParaOUsuario_RetornaTrue()
    {
        // Arrange
        var path = CaminhoDeAvatar.Gerar(UserId, "image/webp");

        // Act & Assert
        CaminhoDeAvatar.PertenceAo(UserId, path).ShouldBeTrue();
    }

    [Theory]
    [InlineData("avatars/{0}/foto.png")]
    [InlineData("avatars/{0}/sub/foto.png")]
    public void PertenceAo_ArquivoDentroDaPastaDoUsuario_RetornaTrue(string modelo)
    {
        // Act & Assert
        CaminhoDeAvatar.PertenceAo(UserId, string.Format(modelo, UserId)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PertenceAo_PathVazio_RetornaFalse(string? path)
    {
        // Act & Assert
        CaminhoDeAvatar.PertenceAo(UserId, path).ShouldBeFalse();
    }

    [Fact]
    public void PertenceAo_PastaDeOutroUsuario_RetornaFalse()
    {
        // Arrange
        var path = CaminhoDeAvatar.Gerar(Guid.NewGuid(), "image/png");

        // Act & Assert
        CaminhoDeAvatar.PertenceAo(UserId, path).ShouldBeFalse();
    }

    [Theory]
    [InlineData("avatars/{0}x/foto.png")]
    [InlineData("avatars/{0}")]
    [InlineData("avatars/{0}/")]
    [InlineData("/avatars/{0}/foto.png")]
    [InlineData("Avatars/{0}/foto.png")]
    [InlineData("capas/{0}/foto.png")]
    [InlineData("x/avatars/{0}/foto.png")]
    public void PertenceAo_ForaDaPastaOuPrefixoParecido_RetornaFalse(string modelo)
    {
        // Act & Assert
        CaminhoDeAvatar.PertenceAo(UserId, string.Format(modelo, UserId)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("avatars/{0}/../outro/foto.png")]
    [InlineData("avatars/{0}/..")]
    [InlineData("avatars/{0}/./foto.png")]
    [InlineData("avatars/{0}//foto.png")]
    [InlineData(@"avatars/{0}/..\foto.png")]
    [InlineData(@"avatars/{0}/sub\foto.png")]
    public void PertenceAo_ComPontoPontoOuSegmentoEstranho_RetornaFalse(string modelo)
    {
        // Act & Assert
        CaminhoDeAvatar.PertenceAo(UserId, string.Format(modelo, UserId)).ShouldBeFalse();
    }
}
