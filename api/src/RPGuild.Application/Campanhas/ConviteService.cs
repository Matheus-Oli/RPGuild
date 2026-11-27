using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Application.Notificacoes;
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
    INotificador notificador,
    TimeProvider relogio) : IConvitesEntreAmigos
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
        await AvisarConvidadosAsync(novos, campanha, cancellationToken);

        var respostas = new List<ConviteDaCampanhaResponse>();
        foreach (var convite in novos)
        {
            respostas.Add(new ConviteDaCampanhaResponse(convite.Id, await PessoaAsync(convite.UserId, cancellationToken), convite.CreatedAt));
        }

        return respostas.ToArray();
    }

    /// <summary>
    /// O card do convite para qualquer usuário logado: o convidado e o mestre veem tudo; os outros, só para quem é.
    /// </summary>
    public async Task<Resultado<CardDoConviteVisto>> CardAsync(Guid userId, Guid conviteId, CancellationToken cancellationToken)
    {
        var convite = await convites.BuscarPorIdAsync(conviteId, cancellationToken);
        var campanha = convite is null ? null : await campanhas.BuscarComPessoasAsync(convite.CampaignId, cancellationToken);
        if (convite is null || campanha is null)
        {
            return ErrosDeConvite.ConviteNaoEncontrado;
        }

        return CardDoConvite.Montar(
            convite,
            campanha,
            await PessoaGuardadaAsync(convite.InvitedBy, cancellationToken),
            await PessoaGuardadaAsync(convite.UserId, cancellationToken),
            userId,
            Url);
    }

    /// <summary>
    /// Os convites pendentes da campanha, os mais novos primeiro. Só o mestre.
    /// </summary>
    public async Task<Resultado<ConviteDaCampanhaResponse[]>> ListarDaCampanhaAsync(
        Guid userId, Guid campanhaId, CancellationToken cancellationToken)
    {
        var acesso = await acessos.DeMestreAsync(userId, campanhaId, cancellationToken);
        if (!acesso.Sucesso)
        {
            return acesso.Erro!;
        }

        var pendentes = await convites.ListarPendentesDaCampanhaAsync(campanhaId, cancellationToken);
        return pendentes.Select(c => new ConviteDaCampanhaResponse(c.Id, Pessoa(c.Convidado), c.CriadoEm)).ToArray();
    }

    /// <summary>
    /// Os convites pendentes que o usuário recebeu, os mais novos primeiro.
    /// </summary>
    public async Task<IReadOnlyList<MeuConviteResponse>> ListarMeusAsync(Guid userId, CancellationToken cancellationToken) =>
    [
        .. (await convites.ListarPendentesDoUsuarioAsync(userId, cancellationToken)).Select(c => new MeuConviteResponse(
            c.Id,
            new CampanhaDoConviteResponse(c.CampanhaId, c.CampanhaNome, new SistemaDaCampanhaResponse(c.Sistema.Slug, c.Sistema.Nome), Url(c.CapaPath)),
            Pessoa(c.Mestre),
            c.CriadoEm)),
    ];

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
    /// Aviso nº 9 para cada convidado. Só depois de gravar os convites.
    /// </summary>
    public async Task AvisarConvidadosAsync(
        IEnumerable<ConviteDeCampanha> novos, CampanhaGuardada campanha, CancellationToken cancellationToken)
    {
        foreach (var convite in novos)
        {
            await notificador.NotificarAsync(
                convite.UserId,
                new ConviteRecebido(convite.Id, campanha.Campanha.Id, campanha.Campanha.Nome, campanha.Mestre.Nome, campanha.Mestre.Nick),
                cancellationToken);
        }
    }

    /// <summary>
    /// Só o convidado, e só pendente. Vira jogador e a campanha entra no início da grade dele, tudo junto. Depois, o
    /// mestre recebe o aviso nº 10 e os outros jogadores, o nº 13.
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

        await AvisarEntradaAsync(convite.Valor!, cancellationToken);
        return Vazio.Valor;
    }

    /// <summary>
    /// Só o convidado, e só pendente. O mestre recebe o aviso nº 11.
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

        if (await campanhas.BuscarPorIdAsync(convite.Valor.CampaignId, cancellationToken) is { } campanha)
        {
            var quem = await PessoaGuardadaAsync(userId, cancellationToken);
            await notificador.NotificarAsync(
                campanha.OwnerId,
                new ConviteRecusado(convite.Valor.Id, campanha.Id, campanha.Nome, quem.Id, quem.Nome, quem.Nick),
                cancellationToken);
        }

        return Vazio.Valor;
    }

    /// <summary>
    /// Só o mestre, e só pendente. O convidado e os jogadores recebem 403; quem não participa, 404. O convidado recebe
    /// o aviso nº 12.
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
        await notificador.NotificarAsync(convite.UserId, new ConviteCancelado(acesso.Valor!.Id, acesso.Valor.Nome), cancellationToken);
        return Vazio.Valor;
    }

    /// <summary>
    /// A amizade foi desfeita: os convites pendentes entre os dois, nos dois sentidos, ficam cancelados.
    /// </summary>
    public async Task<IReadOnlyList<AvisoParaEnviar>> CancelarPendentesEntreAsync(
        Guid usuarioA, Guid usuarioB, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var avisos = new List<AvisoParaEnviar>();
        foreach (var (mestre, convidado) in new[] { (usuarioA, usuarioB), (usuarioB, usuarioA) })
        {
            foreach (var convite in await convites.PendentesEntreAsync(mestre, convidado, cancellationToken))
            {
                convite.Cancelar(agora);
                var campanha = await campanhas.BuscarPorIdAsync(convite.CampaignId, cancellationToken)
                    ?? throw new InvalidOperationException($"A campanha {convite.CampaignId} sumiu no meio da requisição.");
                avisos.Add(new AvisoParaEnviar(convite.UserId, new ConviteCancelado(campanha.Id, campanha.Nome)));
            }
        }

        await convites.SalvarAlteracoesAsync(cancellationToken);
        return avisos;
    }

    // Com os nomes de agora: a campanha, o mestre e quem entrou.
    private async Task AvisarEntradaAsync(ConviteDeCampanha convite, CancellationToken cancellationToken)
    {
        var campanha = await campanhas.BuscarComPessoasAsync(convite.CampaignId, cancellationToken);
        if (campanha is null)
        {
            return;
        }

        var quem = await PessoaGuardadaAsync(convite.UserId, cancellationToken);
        var nome = campanha.Campanha.Nome;
        await notificador.NotificarAsync(
            campanha.Campanha.OwnerId, new ConviteAceito(convite.Id, campanha.Campanha.Id, nome, quem.Id, quem.Nome, quem.Nick), cancellationToken);

        foreach (var jogador in campanha.Jogadores.Where(j => j.Pessoa.Id != quem.Id))
        {
            await notificador.NotificarAsync(
                jogador.Pessoa.Id, new JogadorEntrou(campanha.Campanha.Id, nome, quem.Id, quem.Nome, quem.Nick), cancellationToken);
        }
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

    private PessoaDaCampanhaResponse Pessoa(PessoaGuardada pessoa) => new(pessoa.Id, pessoa.Nome, pessoa.Nick, Url(pessoa.AvatarPath));

    private string? Url(string? path) => path is null ? null : storage.ObterUrlPublica(path);

    private async Task<PessoaDaCampanhaResponse> PessoaAsync(Guid userId, CancellationToken cancellationToken) =>
        Pessoa(await PessoaGuardadaAsync(userId, cancellationToken));

    private async Task<PessoaGuardada> PessoaGuardadaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var perfil = await identity.BuscarPerfilAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"O usuário {userId} sumiu no meio da requisição.");
        return new PessoaGuardada(perfil.Id, perfil.Nome, perfil.Nick, perfil.AvatarPath);
    }
}
