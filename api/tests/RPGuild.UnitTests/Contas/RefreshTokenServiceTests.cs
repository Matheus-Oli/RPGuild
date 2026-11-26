using System.Buffers.Text;
using Microsoft.Extensions.Time.Testing;
using RPGuild.Application.Comum;
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

    [Fact]
    public async Task RotacionarAsync_TokenValido_RevogaOAntigoECriaUmNovoNaMesmaFamilia()
    {
        // Arrange
        var emitido = await _service.EmitirAsync(_userId, CancellationToken.None);
        _relogio.Advance(TimeSpan.FromHours(1));

        // Act
        var resultado = await _service.RotacionarAsync(emitido.Token, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeTrue();
        var novo = resultado.Valor!;
        novo.Token.ShouldNotBe(emitido.Token);
        novo.UserId.ShouldBe(_userId);
        novo.ExpiresAt.ShouldBe(_relogio.GetUtcNow().AddDays(14));

        var antigo = _repositorio.Tokens.Single(t => t.TokenHash == RefreshTokenService.CalcularHash(emitido.Token));
        var atual = _repositorio.Tokens.Single(t => t.TokenHash == RefreshTokenService.CalcularHash(novo.Token));
        antigo.RevokedAt.ShouldBe(_relogio.GetUtcNow());
        antigo.ReplacedById.ShouldBe(atual.Id);
        atual.FamilyId.ShouldBe(antigo.FamilyId);
        atual.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task RotacionarAsync_TokenExpirado_RetornaNaoAutorizado()
    {
        // Arrange
        var emitido = await _service.EmitirAsync(_userId, CancellationToken.None);
        _relogio.Advance(RefreshTokenService.Validade);

        // Act
        var resultado = await _service.RotacionarAsync(emitido.Token, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeFalse();
        resultado.Erro!.Tipo.ShouldBe(TipoErro.NaoAutorizado);
        _repositorio.Tokens.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RotacionarAsync_TokenRevogadoReutilizado_RevogaAFamiliaInteira()
    {
        // Arrange
        var original = await _service.EmitirAsync(_userId, CancellationToken.None);
        var rotacionado = (await _service.RotacionarAsync(original.Token, CancellationToken.None)).Valor!;
        var outraSessao = await _service.EmitirAsync(_userId, CancellationToken.None);
        _relogio.Advance(TimeSpan.FromMinutes(5));

        // Act
        var resultado = await _service.RotacionarAsync(original.Token, CancellationToken.None);

        // Assert
        resultado.Sucesso.ShouldBeFalse();
        resultado.Erro!.Tipo.ShouldBe(TipoErro.NaoAutorizado);

        var familia = _repositorio.Tokens.Where(t => t.FamilyId == Buscar(original.Token).FamilyId).ToList();
        familia.Count.ShouldBe(2);
        familia.ShouldAllBe(t => t.RevokedAt != null);
        Buscar(rotacionado.Token).RevokedAt.ShouldBe(_relogio.GetUtcNow());
        Buscar(outraSessao.Token).RevokedAt.ShouldBeNull();
    }

    private Domain.Contas.RefreshToken Buscar(string token) =>
        _repositorio.Tokens.Single(t => t.TokenHash == RefreshTokenService.CalcularHash(token));
}
