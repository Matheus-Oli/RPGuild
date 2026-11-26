using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Provider de log que guarda as linhas formatadas (com a exceção, se houver), respeitando os níveis da configuração da API.
/// </summary>
public sealed class LogsCapturados : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _linhas = new();

    public IReadOnlyCollection<string> Linhas => _linhas;

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _linhas);

    public void Dispose()
    {
    }

    private sealed class Logger(string categoria, ConcurrentQueue<string> linhas) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            linhas.Enqueue($"{logLevel} {categoria}: {formatter(state, exception)} {exception}");
    }
}
