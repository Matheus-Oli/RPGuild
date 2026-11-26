using RPGuild.Application.Comum;

namespace RPGuild.Application.Notificacoes;

/// <summary>
/// O sininho do usuário logado (/api/notifications). Só enxerga as notificações dele.
/// </summary>
public sealed class NotificacoesService(INotificacaoRepository notificacoes, TimeProvider relogio)
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

        return new PaginaDeNotificacoesResponse([.. pagina.Select(NotificacaoResponse.De)], proximo);
    }

    public async Task<ContagemDeNaoLidasResponse> ContarNaoLidasAsync(Guid userId, CancellationToken cancellationToken) =>
        new(await notificacoes.ContarNaoLidasAsync(userId, cancellationToken));

    /// <summary>
    /// Idempotente: marcar uma já lida mantém a data da primeira leitura.
    /// </summary>
    public async Task<Resultado<Vazio>> MarcarComoLidaAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var notificacao = await notificacoes.BuscarDoUsuarioAsync(id, userId, cancellationToken);
        if (notificacao is null)
        {
            return ErrosDeNotificacoes.NotificacaoNaoEncontrada;
        }

        notificacao.MarcarComoLida(relogio.GetUtcNow());
        await notificacoes.SalvarAlteracoesAsync(cancellationToken);
        return Vazio.Valor;
    }

    public async Task<Resultado<Vazio>> ApagarAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var notificacao = await notificacoes.BuscarDoUsuarioAsync(id, userId, cancellationToken);
        if (notificacao is null)
        {
            return ErrosDeNotificacoes.NotificacaoNaoEncontrada;
        }

        notificacoes.Remover(notificacao);
        await notificacoes.SalvarAlteracoesAsync(cancellationToken);
        return Vazio.Valor;
    }

    public Task MarcarTodasComoLidasAsync(Guid userId, CancellationToken cancellationToken) =>
        notificacoes.MarcarTodasComoLidasAsync(userId, relogio.GetUtcNow(), cancellationToken);

}
