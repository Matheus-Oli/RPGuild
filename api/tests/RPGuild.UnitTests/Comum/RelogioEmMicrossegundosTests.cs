using Microsoft.Extensions.Time.Testing;
using RPGuild.Infrastructure.Comum;

namespace RPGuild.UnitTests.Comum;

public class RelogioEmMicrossegundosTests
{
    // 18:00:00.1234567: os 7 dígitos dos ticks do .NET, um a mais que o microssegundo do Postgres.
    private static readonly DateTimeOffset ComTicks = new DateTimeOffset(2026, 11, 26, 18, 0, 0, TimeSpan.Zero).AddTicks(1_234_567);

    [Fact]
    public void GetUtcNow_RelogioComTicks_TruncaParaMicrossegundos()
    {
        // Arrange
        var relogio = new RelogioEmMicrossegundos(new FakeTimeProvider(ComTicks));

        // Act
        var agora = relogio.GetUtcNow();

        // Assert
        agora.ShouldBe(new DateTimeOffset(2026, 11, 26, 18, 0, 0, TimeSpan.Zero).AddTicks(1_234_560));
        agora.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void GetUtcNow_RelogioQueAvanca_AcompanhaORelogioDeBaixo()
    {
        // Arrange
        var relogioDeBaixo = new FakeTimeProvider(ComTicks);
        var relogio = new RelogioEmMicrossegundos(relogioDeBaixo);

        // Act
        relogioDeBaixo.Advance(TimeSpan.FromMinutes(5));

        // Assert
        relogio.GetUtcNow().ShouldBe(RelogioEmMicrossegundos.Truncar(ComTicks.AddMinutes(5)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 10)]
    [InlineData(19, 10)]
    public void Truncar_Ticks_DescartaOQueSobraDoMicrossegundo(long ticks, long esperado)
    {
        // Arrange
        var inicio = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act & Assert
        RelogioEmMicrossegundos.Truncar(inicio.AddTicks(ticks)).ShouldBe(inicio.AddTicks(esperado));
    }
}
