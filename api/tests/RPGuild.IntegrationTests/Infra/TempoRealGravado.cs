using System.Collections.Concurrent;
using RPGuild.Application.Campanhas;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// No lugar do hub de campanha: guarda os eventos que a API mandaria, para os casos em que ninguém pode estar no grupo.
/// </summary>
public sealed class TempoRealGravado : ICampanhaTempoReal
{
    private readonly ConcurrentQueue<ConvitesMudaramEvento> _convitesMudaram = new();

    public IReadOnlyCollection<ConvitesMudaramEvento> ConvitesMudaram => _convitesMudaram;

    public Task MembroMudouAsync(MembroMudouEvento evento, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task CampanhaAtualizadaAsync(CampanhaAtualizadaEvento evento, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task CampanhaExcluidaAsync(CampanhaExcluidaEvento evento, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ConvitesMudaramAsync(ConvitesMudaramEvento evento, CancellationToken cancellationToken)
    {
        _convitesMudaram.Enqueue(evento);
        return Task.CompletedTask;
    }
}
