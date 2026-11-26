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
}
