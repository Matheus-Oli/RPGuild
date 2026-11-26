namespace RPGuild.Infrastructure.Comum;

/// <summary>
/// "Agora" já na precisão do Postgres (microssegundos). Sem isso, a resposta de quem grava traz os 100 ns do .NET
/// e a leitura seguinte devolve o mesmo instante truncado. É o <see cref="TimeProvider"/> de toda a API.
/// </summary>
public sealed class RelogioEmMicrossegundos(TimeProvider relogio) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => Truncar(relogio.GetUtcNow());

    public override TimeZoneInfo LocalTimeZone => relogio.LocalTimeZone;

    public override long TimestampFrequency => relogio.TimestampFrequency;

    public override long GetTimestamp() => relogio.GetTimestamp();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        relogio.CreateTimer(callback, state, dueTime, period);

    public static DateTimeOffset Truncar(DateTimeOffset instante) =>
        instante.AddTicks(-(instante.Ticks % TimeSpan.TicksPerMicrosecond));
}
