using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Application.Uploads;
using RPGuild.Domain.Taberna;

namespace RPGuild.Application.Taberna;

public sealed class AmizadeService(
    IAmizadeRepository amizades,
    IIdentityService identity,
    IStorageService storage,
    TimeProvider relogio)
{
    /// <summary>
    /// O request já chega validado: <see cref="EnviarPedidoRequest.EmailOuNick"/> é um e-mail ou nick válido.
    /// </summary>
    public async Task<Resultado<PedidoEnviadoResponse>> EnviarAsync(
        Guid userId, EnviarPedidoRequest request, CancellationToken cancellationToken)
    {
        var alvo = AlvoDoPedido.Interpretar(request.EmailOuNick)
            ?? throw new ArgumentException("E-mail ou nick inválido.", nameof(request));

        var destinatario = alvo.Tipo == TipoDeAlvo.Email
            ? await identity.BuscarPorEmailAsync(alvo.Valor, cancellationToken)
            : await identity.BuscarPorNickAsync(alvo.Valor, cancellationToken);
        if (destinatario is null)
        {
            return ErrosDaTaberna.UsuarioNaoEncontrado;
        }

        if (destinatario.Id == userId)
        {
            return ErrosDaTaberna.PedidoParaSiMesmo;
        }

        var agora = relogio.GetUtcNow();
        var existente = await amizades.BuscarDoParAsync(userId, destinatario.Id, cancellationToken);
        Amizade pedido;
        switch (existente?.Status)
        {
            case null:
                pedido = Amizade.Pedir(userId, destinatario.Id, agora);
                await amizades.AdicionarAsync(pedido, cancellationToken);
                break;
            case StatusDaAmizade.Aceita:
                return ErrosDaTaberna.JaSaoAmigos;
            case StatusDaAmizade.Pendente:
                // Sem aceite automático: quem recebeu o pedido aceita pela lista.
                return existente.RequesterId == userId ? ErrosDaTaberna.PedidoJaEnviado : ErrosDaTaberna.PedidoRecebidoPendente;
            default:
                existente.Reenviar(userId, destinatario.Id, agora);
                pedido = existente;
                break;
        }

        await amizades.SalvarAlteracoesAsync(cancellationToken);

        return new PedidoEnviadoResponse(pedido.Id, Usuario(destinatario), pedido.CreatedAt);
    }

    public Task<Resultado<Vazio>> AceitarAsync(Guid userId, Guid pedidoId, CancellationToken cancellationToken) =>
        ResponderAsync(userId, pedidoId, (pedido, agora) => pedido.Aceitar(agora), cancellationToken);

    public Task<Resultado<Vazio>> RecusarAsync(Guid userId, Guid pedidoId, CancellationToken cancellationToken) =>
        ResponderAsync(userId, pedidoId, (pedido, agora) => pedido.Recusar(agora), cancellationToken);

    /// <summary>
    /// Só quem enviou cancela, e só enquanto está pendente. O pedido é apagado.
    /// </summary>
    public async Task<Resultado<Vazio>> CancelarAsync(Guid userId, Guid pedidoId, CancellationToken cancellationToken)
    {
        var pedido = await amizades.BuscarPorIdAsync(pedidoId, cancellationToken);
        if (pedido is null || pedido.RequesterId != userId)
        {
            return ErrosDaTaberna.PedidoNaoEncontrado;
        }

        if (!pedido.EstaPendente)
        {
            return ErrosDaTaberna.PedidoNaoPendente;
        }

        amizades.Remover(pedido);
        await amizades.SalvarAlteracoesAsync(cancellationToken);
        return Vazio.Valor;
    }

    /// <summary>
    /// Só o destinatário responde, e só enquanto está pendente. Para os outros, o pedido "não existe" (404).
    /// </summary>
    private async Task<Resultado<Vazio>> ResponderAsync(
        Guid userId, Guid pedidoId, Action<Amizade, DateTimeOffset> responder, CancellationToken cancellationToken)
    {
        var pedido = await amizades.BuscarPorIdAsync(pedidoId, cancellationToken);
        if (pedido is null || pedido.AddresseeId != userId)
        {
            return ErrosDaTaberna.PedidoNaoEncontrado;
        }

        if (!pedido.EstaPendente)
        {
            return ErrosDaTaberna.PedidoNaoPendente;
        }

        responder(pedido, relogio.GetUtcNow());
        await amizades.SalvarAlteracoesAsync(cancellationToken);
        return Vazio.Valor;
    }

    private UsuarioDaTaberna Usuario(DadosDoPerfil perfil) => new(
        perfil.Id,
        perfil.Nome,
        perfil.Nick,
        perfil.AvatarPath is null ? null : storage.ObterUrlPublica(perfil.AvatarPath));
}
