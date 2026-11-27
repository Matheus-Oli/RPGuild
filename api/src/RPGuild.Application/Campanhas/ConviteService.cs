using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Application.Taberna;
using RPGuild.Application.Uploads;
using RPGuild.Domain.Campanhas;
using RPGuild.Domain.Taberna;

namespace RPGuild.Application.Campanhas;

/// <summary>
/// Convites de campanha: só amigos do mestre, um pendente por pessoa, e só o convidado responde.
/// </summary>
public sealed class ConviteService(
    IConviteRepository convites,
    ICampanhaRepository campanhas,
    IAmizadeRepository amizades,
    AcessoACampanha acessos,
    GradeDeCampanhas grade,
    ITransacao transacao,
    IIdentityService identity,
    IStorageService storage,
    TimeProvider relogio)
{
    /// <summary>
    /// Só o mestre. Tudo ou nada: se alguém da lista não pode ser convidado, ninguém é. O request já chega validado.
    /// </summary>
    public async Task<Resultado<ConviteDaCampanhaResponse[]>> ConvidarAsync(
        Guid userId, Guid campanhaId, ConvidarRequest request, CancellationToken cancellationToken)
    {
        var acesso = await acessos.DeMestreAsync(userId, campanhaId, cancellationToken);
        if (!acesso.Sucesso)
        {
            return acesso.Erro!;
        }

        var campanha = await campanhas.BuscarComPessoasAsync(campanhaId, cancellationToken)
            ?? throw new InvalidOperationException($"A campanha {campanhaId} sumiu no meio da requisição.");
        var pendentes = await convites.PendentesDaCampanhaAsync(campanhaId, cancellationToken);
        var erro = await ValidarAsync(
            userId,
            request.UserIds,
            [.. campanha.Jogadores.Select(j => j.Pessoa.Id)],
            [.. pendentes.Select(p => p.UserId)],
            cancellationToken);
        if (erro is not null)
        {
            return erro;
        }

        var novos = await AdicionarAsync(campanhaId, userId, request.UserIds, cancellationToken);
        await convites.SalvarAlteracoesAsync(cancellationToken);

        var respostas = new List<ConviteDaCampanhaResponse>();
        foreach (var convite in novos)
        {
            respostas.Add(new ConviteDaCampanhaResponse(convite.Id, await PessoaAsync(convite.UserId, cancellationToken), convite.CreatedAt));
        }

        return respostas.ToArray();
    }

    /// <summary>
    /// Confere a lista inteira e devolve o erro com todos os recusados, ou null. 400 vence 409.
    /// </summary>
    public async Task<Erro?> ValidarAsync(
        Guid mestreId,
        IReadOnlyList<Guid> userIds,
        IReadOnlyCollection<Guid> jogadores,
        IReadOnlyCollection<Guid> comPendente,
        CancellationToken cancellationToken)
    {
        var invalidos = new List<ConvidadoInvalido>();
        foreach (var id in userIds)
        {
            var motivo = id == mestreId ? MotivosDeConvite.Mestre
                : !await SaoAmigosAsync(mestreId, id, cancellationToken) ? MotivosDeConvite.NaoAmigo
                : jogadores.Contains(id) ? MotivosDeConvite.JaJogador
                : comPendente.Contains(id) ? MotivosDeConvite.JaConvidado
                : null;
            if (motivo is not null)
            {
                invalidos.Add(new ConvidadoInvalido(id, motivo));
            }
        }

        return invalidos.Count == 0 ? null : ErrosDeConvite.ConvidadosInvalidos(invalidos);
    }

    /// <summary>
    /// Os convites pendentes, já validados, prontos para salvar (sem salvar).
    /// </summary>
    public async Task<IReadOnlyList<ConviteDeCampanha>> AdicionarAsync(
        Guid campanhaId, Guid mestreId, IReadOnlyList<Guid> userIds, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var novos = userIds.Select(id => ConviteDeCampanha.Criar(campanhaId, mestreId, id, agora)).ToList();
        foreach (var convite in novos)
        {
            await convites.AdicionarAsync(convite, cancellationToken);
        }

        return novos;
    }

    /// <summary>
    /// Só o convidado, e só pendente. Vira jogador e a campanha entra no início da grade dele, tudo junto.
    /// </summary>
    public async Task<Resultado<Vazio>> AceitarAsync(Guid userId, Guid conviteId, CancellationToken cancellationToken)
    {
        var convite = await DoConvidadoAsync(userId, conviteId, cancellationToken);
        if (!convite.Sucesso)
        {
            return convite.Erro!;
        }

        var agora = relogio.GetUtcNow();
        await transacao.ExecutarAsync(
            async () =>
            {
                convite.Valor!.Aceitar(agora);
                await campanhas.AdicionarJogadorAsync(new MembroDaCampanha(convite.Valor.CampaignId, userId, agora), cancellationToken);
                await convites.SalvarAlteracoesAsync(cancellationToken);
                await grade.ColocarNoInicioAsync(userId, convite.Valor.CampaignId, cancellationToken);
            },
            cancellationToken);

        return Vazio.Valor;
    }

    /// <summary>
    /// Só o convidado, e só pendente.
    /// </summary>
    public async Task<Resultado<Vazio>> RecusarAsync(Guid userId, Guid conviteId, CancellationToken cancellationToken)
    {
        var convite = await DoConvidadoAsync(userId, conviteId, cancellationToken);
        if (!convite.Sucesso)
        {
            return convite.Erro!;
        }

        convite.Valor!.Recusar(relogio.GetUtcNow());
        await convites.SalvarAlteracoesAsync(cancellationToken);
        return Vazio.Valor;
    }

    /// <summary>
    /// Só o mestre, e só pendente. O convidado e os jogadores recebem 403; quem não participa, 404.
    /// </summary>
    public async Task<Resultado<Vazio>> CancelarAsync(Guid userId, Guid conviteId, CancellationToken cancellationToken)
    {
        var convite = await convites.BuscarPorIdAsync(conviteId, cancellationToken);
        if (convite is null)
        {
            return ErrosDeConvite.ConviteNaoEncontrado;
        }

        if (convite.UserId == userId)
        {
            return ErrosDeCampanha.SoOMestre;
        }

        var acesso = await acessos.DeMestreAsync(userId, convite.CampaignId, cancellationToken);
        if (!acesso.Sucesso)
        {
            return acesso.Erro!.Tipo == TipoErro.NaoEncontrado ? ErrosDeConvite.ConviteNaoEncontrado : acesso.Erro;
        }

        if (!convite.EstaPendente)
        {
            return ErrosDeConvite.ConviteNaoPendente;
        }

        convite.Cancelar(relogio.GetUtcNow());
        await convites.SalvarAlteracoesAsync(cancellationToken);
        return Vazio.Valor;
    }

    // Para quem não é o convidado, o convite "não existe" (404).
    private async Task<Resultado<ConviteDeCampanha>> DoConvidadoAsync(Guid userId, Guid conviteId, CancellationToken cancellationToken)
    {
        var convite = await convites.BuscarPorIdAsync(conviteId, cancellationToken);
        if (convite is null || convite.UserId != userId)
        {
            return ErrosDeConvite.ConviteNaoEncontrado;
        }

        return convite.EstaPendente ? convite : ErrosDeConvite.ConviteNaoPendente;
    }

    private async Task<bool> SaoAmigosAsync(Guid mestreId, Guid userId, CancellationToken cancellationToken) =>
        await amizades.BuscarDoParAsync(mestreId, userId, cancellationToken) is { Status: StatusDaAmizade.Aceita };

    private async Task<PessoaDaCampanhaResponse> PessoaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var perfil = await identity.BuscarPerfilAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"O usuário {userId} sumiu no meio da requisição.");
        return new PessoaDaCampanhaResponse(
            perfil.Id, perfil.Nome, perfil.Nick, perfil.AvatarPath is null ? null : storage.ObterUrlPublica(perfil.AvatarPath));
    }
}
