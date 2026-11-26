using System.Buffers.Text;
using Microsoft.Extensions.Time.Testing;
using RPGuild.Application.Contas;

namespace RPGuild.UnitTests.Contas;

public class RefreshTokenServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 25, 20, 0, 0, TimeSpan.Zero);

    private readonly RefreshTokenRepositoryFalso _repositorio = new();
    private readonly FakeTimeProvider _relogio = new(Agora);
    private readonly RefreshTokenService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public RefreshTokenServiceTests()
    {
        _service = new RefreshTokenService(_repositorio, _relogio);
    }

    [Fact]
    public async Task EmitirAsync_Usuario_GuardaSomenteOHash()
    {
        // Act
        var emitido = await _service.EmitirAsync(_userId, CancellationToken.None);

        // Assert
        Base64Url.DecodeFromChars(emitido.Token).Length.ShouldBe(64);
        emitido.ExpiresAt.ShouldBe(Agora.AddDays(14));

        var salvo = _repositorio.Tokens.ShouldHaveSingleItem();
        salvo.TokenHash.ShouldBe(RefreshTokenService.CalcularHash(emitido.Token));
        salvo.TokenHash.ShouldNotContain(emitido.Token);
        salvo.UserId.ShouldBe(_userId);
        salvo.CreatedAt.ShouldBe(Agora);
        salvo.ExpiresAt.ShouldBe(Agora.AddDays(14));
        salvo.RevokedAt.ShouldBeNull();
        _repositorio.VezesSalvo.ShouldBe(1);
    }
}
