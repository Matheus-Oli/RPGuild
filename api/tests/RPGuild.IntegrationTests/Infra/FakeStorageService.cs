using System.Collections.Concurrent;
using RPGuild.Application.Uploads;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Guarda em memória os paths com URL de upload criada e os apagados, no lugar do Supabase Storage.
/// </summary>
public sealed class FakeStorageService : IStorageService
{
    public const string UrlBase = "https://storage.rpguild.invalid";

    private readonly ConcurrentQueue<string> _criados = new();
    private readonly ConcurrentQueue<string> _apagados = new();

    public IReadOnlyCollection<string> Criados => _criados;

    public IReadOnlyCollection<string> Apagados => _apagados;

    /// <summary>
    /// Simula o Storage fora do ar na exclusão.
    /// </summary>
    public bool FalharAoApagar { get; set; }

    public Task<string> CriarUrlDeUploadAsync(string path, CancellationToken ct)
    {
        _criados.Enqueue(path);
        return Task.FromResult($"{UrlBase}/upload/sign/{path}?token=assinado");
    }

    public string ObterUrlPublica(string path) => $"{UrlBase}/public/{path}";

    public Task ApagarAsync(string path, CancellationToken ct)
    {
        if (FalharAoApagar)
        {
            throw new HttpRequestException("Storage indisponível (simulado nos testes).");
        }

        _apagados.Enqueue(path);
        return Task.CompletedTask;
    }
}
