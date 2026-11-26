using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Conexão no hub de notificações pelo TestServer, guardando os eventos recebidos.
/// Transporte LongPolling (o handler do TestServer atende HTTP), com o token na query "access_token",
/// como o navegador faz no WebSocket; o cliente não manda header Authorization.
/// </summary>
public sealed class ConexaoNoHub : IAsyncDisposable
{
    public const string Rota = "/hubs/notifications";

    public static readonly string[] Eventos =
        ["NotificationCreated", "UnreadCount", "NotificationRead", "NotificationsReadAll", "NotificationDeleted"];

    private static readonly TimeSpan EsperaPadrao = TimeSpan.FromSeconds(5);

    private readonly HubConnection _conexao;
    private readonly ConcurrentQueue<EventoRecebido> _recebidos = new();
    private readonly SemaphoreSlim _chegou = new(0);

    private ConexaoNoHub(HubConnection conexao)
    {
        _conexao = conexao;
        foreach (var evento in Eventos)
        {
            _conexao.On<JsonElement>(evento, payload =>
            {
                _recebidos.Enqueue(new EventoRecebido(evento, payload));
                _chegou.Release();
            });
        }
    }

    public IReadOnlyCollection<EventoRecebido> Recebidos => _recebidos;

    /// <summary>
    /// Abre a conexão. Sem token (null), a conexão vai sem credencial. Lança se o hub recusar.
    /// </summary>
    public static async Task<ConexaoNoHub> AbrirAsync(RPGuildApiFactory factory, string? accessToken)
    {
        var url = $"https://localhost{Rota}" + (accessToken is null ? string.Empty : $"?access_token={Uri.EscapeDataString(accessToken)}");
        var servidor = factory.Server;

        var conexao = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => servidor.CreateHandler();
            })
            .Build();

        var noHub = new ConexaoNoHub(conexao);
        try
        {
            await conexao.StartAsync();
        }
        catch
        {
            await noHub.DisposeAsync();
            throw;
        }

        return noHub;
    }

    /// <summary>
    /// Espera o primeiro evento com este nome (e que passe no filtro) chegar, até o timeout. Falha o teste se não chegar.
    /// </summary>
    public async Task<JsonElement> EsperarAsync(string evento, Func<JsonElement, bool>? filtro = null, TimeSpan? timeout = null)
    {
        var limite = DateTimeOffset.UtcNow + (timeout ?? EsperaPadrao);
        while (true)
        {
            var encontrado = _recebidos.FirstOrDefault(r => r.Evento == evento && (filtro?.Invoke(r.Payload) ?? true));
            if (encontrado is not null)
            {
                return encontrado.Payload;
            }

            var resta = limite - DateTimeOffset.UtcNow;
            if (resta <= TimeSpan.Zero || !await _chegou.WaitAsync(resta))
            {
                throw new ShouldAssertException(
                    $"O evento {evento} não chegou. Recebidos: {string.Join(", ", _recebidos.Select(r => r.Evento))}.");
            }
        }
    }

    /// <summary>
    /// Confere que nenhum evento com este nome chegou dentro da janela (curta: é o tempo de dar chance de chegar).
    /// </summary>
    public async Task NaoRecebeAsync(string evento, TimeSpan? janela = null)
    {
        var limite = DateTimeOffset.UtcNow + (janela ?? TimeSpan.FromMilliseconds(500));
        for (var resta = limite - DateTimeOffset.UtcNow; resta > TimeSpan.Zero; resta = limite - DateTimeOffset.UtcNow)
        {
            await _chegou.WaitAsync(resta);
        }

        _recebidos.ShouldNotContain(r => r.Evento == evento, $"O evento {evento} não devia ter chegado.");
    }

    /// <summary>
    /// Esquece os eventos já recebidos, para o próximo passo do teste só enxergar os novos.
    /// </summary>
    public void Esquecer() => _recebidos.Clear();

    public async ValueTask DisposeAsync()
    {
        await _conexao.DisposeAsync();
        _chegou.Dispose();
    }
}

public sealed record EventoRecebido(string Evento, JsonElement Payload);
