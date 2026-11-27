using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Application.Notificacoes;
using RPGuild.Application.Uploads;
using RPGuild.Domain.Taberna;

namespace RPGuild.Application.Taberna;

public sealed class AmizadeService(
    IAmizadeRepository amizades,
    IIdentityService identity,
    IStorageService storage,
    INotificador notificador,
    IConvitesEntreAmigos convites,
    ICampanhasEmComum campanhasEmComum,
    ITransacao transacao,
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

        try
        {
            await amizades.SalvarAlteracoesAsync(cancellationToken);
        }
        catch (ParDeAmizadeJaExisteException)
        {
            // Os dois pediram ao mesmo tempo e o outro pedido entrou antes: é o mesmo caso do pedido cruzado.
            return ErrosDaTaberna.PedidoRecebidoPendente;
        }

        await AvisarDestinatarioAsync(userId, pedido.Id, destinatario.Id, cancellationToken);

        return new PedidoEnviadoResponse(pedido.Id, Usuario(destinatario), pedido.CreatedAt);
    }

    public async Task<IReadOnlyList<AmigoResponse>> ListarAmigosAsync(Guid userId, CancellationToken cancellationToken)
    {
        var amigos = await amizades.ListarAmigosAsync(userId, cancellationToken);
        var emComum = await campanhasEmComum.ContarAsync(userId, [.. amigos.Select(a => a.Amigo.Id)], cancellationToken);
        return
        [
            .. amigos.Select(a =>
            {
                var amigo = Usuario(a.Amigo);
                return new AmigoResponse(
                    amigo.UserId, amigo.Nome, amigo.Nick, amigo.AvatarUrl, emComum.GetValueOrDefault(amigo.UserId), a.AmigosDesde);
            }),
        ];
    }

    public async Task<PedidosResponse> ListarPedidosAsync(Guid userId, CancellationToken cancellationToken)
    {
        var pendentes = await amizades.ListarPendentesAsync(userId, cancellationToken);
        return new PedidosResponse(
            [.. pendentes.Where(p => !p.Enviado).Select(p => new PedidoRecebidoResponse(p.Id, Usuario(p.Outro), p.CriadoEm))],
            [.. pendentes.Where(p => p.Enviado).Select(p => new PedidoEnviadoResponse(p.Id, Usuario(p.Outro), p.CriadoEm))]);
    }

    /// <summary>
    /// Quem pediu recebe o aviso nº 6 (só sininho).
    /// </summary>
    public Task<Resultado<Vazio>> AceitarAsync(Guid userId, Guid pedidoId, CancellationToken cancellationToken) =>
        ResponderAsync(
            userId,
            pedidoId,
            (pedido, agora) => pedido.Aceitar(agora),
            quem => new AmizadeAceita(quem.Id, quem.Nome, quem.Nick),
            cancellationToken);

    /// <summary>
    /// Quem pediu recebe o aviso nº 7 (só sininho).
    /// </summary>
    public Task<Resultado<Vazio>> RecusarAsync(Guid userId, Guid pedidoId, CancellationToken cancellationToken) =>
        ResponderAsync(
            userId,
            pedidoId,
            (pedido, agora) => pedido.Recusar(agora),
            quem => new AmizadeRecusada(quem.Id, quem.Nome, quem.Nick),
            cancellationToken);

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
    /// Qualquer um dos dois pode desfazer a amizade: a linha é apagada e quem foi removido não é avisado (aviso nº 8).
    /// Os convites de campanha pendentes entre os dois são cancelados junto, e o convidado recebe o aviso nº 12.
    /// </summary>
    public async Task<Resultado<Vazio>> RemoverAmigoAsync(Guid userId, Guid amigoId, CancellationToken cancellationToken)
    {
        var amizade = await amizades.BuscarDoParAsync(userId, amigoId, cancellationToken);
        if (amizade is not { Status: StatusDaAmizade.Aceita })
        {
            return ErrosDaTaberna.AmizadeNaoEncontrada;
        }

        IReadOnlyList<AvisoParaEnviar> avisos = [];
        await transacao.ExecutarAsync(
            async () =>
            {
                amizades.Remover(amizade);
                await amizades.SalvarAlteracoesAsync(cancellationToken);
                avisos = await convites.CancelarPendentesEntreAsync(userId, amigoId, cancellationToken);
            },
            cancellationToken);

        foreach (var aviso in avisos)
        {
            await notificador.NotificarAsync(aviso.UserId, aviso.Aviso, cancellationToken);
        }

        return Vazio.Valor;
    }

    /// <summary>
    /// Só o destinatário responde, e só enquanto está pendente. Para os outros, o pedido "não existe" (404).
    /// </summary>
    private async Task<Resultado<Vazio>> ResponderAsync(
        Guid userId,
        Guid pedidoId,
        Action<Amizade, DateTimeOffset> responder,
        Func<DadosDoPerfil, Aviso> aviso,
        CancellationToken cancellationToken)
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

        // Com o nome e o nick de quem respondeu como estão agora.
        if (await identity.BuscarPerfilAsync(userId, cancellationToken) is { } quemRespondeu)
        {
            await notificador.NotificarAsync(pedido.RequesterId, aviso(quemRespondeu), cancellationToken);
        }

        return Vazio.Valor;
    }

    // Aviso nº 5 (sininho + e-mail), com o nome e o nick de quem pediu como estão agora.
    private async Task AvisarDestinatarioAsync(Guid userId, Guid pedidoId, Guid destinatarioId, CancellationToken cancellationToken)
    {
        var remetente = await identity.BuscarPerfilAsync(userId, cancellationToken);
        if (remetente is null)
        {
            return;
        }

        await notificador.NotificarAsync(
            destinatarioId, new AmizadeRecebida(pedidoId, userId, remetente.Nome, remetente.Nick), cancellationToken);
    }

    private UsuarioDaTaberna Usuario(DadosDoPerfil perfil) => Usuario(perfil.Id, perfil.Nome, perfil.Nick, perfil.AvatarPath);

    private UsuarioDaTaberna Usuario(PessoaDaLista pessoa) => Usuario(pessoa.Id, pessoa.Nome, pessoa.Nick, pessoa.AvatarPath);

    private UsuarioDaTaberna Usuario(Guid id, string nome, string nick, string? avatarPath) =>
        new(id, nome, nick, avatarPath is null ? null : storage.ObterUrlPublica(avatarPath));
}
