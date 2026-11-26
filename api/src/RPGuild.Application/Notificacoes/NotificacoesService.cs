using System.Text.Json;
using RPGuild.Domain.Notificacoes;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// O sininho do usuário logado (/api/notifications). Só enxerga as notificações dele.
/// </summary>
public sealed class NotificacoesService(INotificacaoRepository notificacoes)
{
    /// <summary>
    /// As mais novas primeiro, paginadas por cursor. O request já chega validado (cursor decodificável, limite ≥ 1).
    /// </summary>
    public async Task<PaginaDeNotificacoesResponse> ListarAsync(
        Guid userId, ListarNotificacoesRequest request, CancellationToken cancellationToken)
    {
        var limite = request.LimiteEfetivo;
        var apos = string.IsNullOrEmpty(request.Cursor) ? null : CursorDeNotificacoes.Decodificar(request.Cursor);

        // Uma a mais para saber se há próxima página.
        var encontradas = await notificacoes.ListarAsync(userId, request.NaoLidas, apos, limite + 1, cancellationToken);
        var pagina = encontradas.Take(limite).ToList();
        var proximo = encontradas.Count > limite ? new CursorDeNotificacoes(pagina[^1].CreatedAt, pagina[^1].Id).Codificar() : null;

        return new PaginaDeNotificacoesResponse([.. pagina.Select(Resposta)], proximo);
    }

    private static NotificacaoResponse Resposta(Notificacao notificacao)
    {
        using var dados = JsonDocument.Parse(notificacao.Dados);
        return new NotificacaoResponse(notificacao.Id, notificacao.Tipo, dados.RootElement.Clone(), notificacao.Lida, notificacao.CreatedAt);
    }
}
