using Microsoft.Extensions.Logging;
using RPGuild.Application.Comum;
using RPGuild.Application.Notificacoes;
using RPGuild.Application.Uploads;
using RPGuild.Domain.Campanhas;

namespace RPGuild.Application.Campanhas;

public sealed class CampanhaService(
    ICampanhaRepository campanhas,
    ISistemasRepository sistemas,
    GradeDeCampanhas grade,
    AcessoACampanha acessos,
    ConviteService convites,
    IStorageService storage,
    INotificador notificador,
    ITransacao transacao,
    TimeProvider relogio,
    ILogger<CampanhaService> logger)
{
    /// <summary>
    /// Quem cria é o mestre, e a campanha entra no início da grade dele. Os convidados (amigos do mestre) recebem os
    /// convites na mesma transação: se a posição ou um convite falhar, nada é criado. O request já chega validado.
    /// </summary>
    public async Task<Resultado<CampanhaDetalheResponse>> CriarAsync(
        Guid userId, CriarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var sistema = await sistemas.BuscarPorSlugAsync(request.Sistema, cancellationToken);
        if (sistema is not { Ativo: true })
        {
            return ErrosDeCampanha.SistemaIndisponivel;
        }

        if (!ImagensDoUsuario(userId, request.CapaPath, request.BannerPath))
        {
            return ErrosDeCampanha.ImagemForaDaPasta;
        }

        var convidados = request.Convidados ?? [];
        if (await convites.ValidarAsync(userId, convidados, jogadores: [], comPendente: [], cancellationToken) is { } erro)
        {
            return erro;
        }

        var campanha = Campanha.Criar(
            userId, sistema.Id, request.Nome.Trim(), request.Descricao, request.CapaPath, request.BannerPath, relogio.GetUtcNow());
        IReadOnlyList<ConviteDeCampanha> novos = [];
        await transacao.ExecutarAsync(
            async () =>
            {
                await campanhas.AdicionarAsync(campanha, cancellationToken);
                await campanhas.SalvarAlteracoesAsync(cancellationToken);
                await grade.ColocarNoInicioAsync(userId, campanha.Id, cancellationToken);
                novos = await convites.AdicionarAsync(campanha.Id, userId, convidados, cancellationToken);
                await campanhas.SalvarAlteracoesAsync(cancellationToken);
            },
            cancellationToken);

        var criada = await RecarregarAsync(campanha.Id, cancellationToken);
        await convites.AvisarConvidadosAsync(novos, criada, cancellationToken);
        return Detalhe(criada, userId);
    }

    /// <summary>
    /// Só o mestre. O sistema não muda. Capa ou banner trocados ou removidos são apagados do storage, e os jogadores
    /// recebem o aviso nº 17 se algo mudou.
    /// </summary>
    public async Task<Resultado<CampanhaDetalheResponse>> EditarAsync(
        Guid userId, Guid campanhaId, EditarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var acesso = await acessos.DeMestreAsync(userId, campanhaId, cancellationToken);
        if (!acesso.Sucesso)
        {
            return acesso.Erro!;
        }

        if (!ImagensDoUsuario(userId, request.CapaPath, request.BannerPath))
        {
            return ErrosDeCampanha.ImagemForaDaPasta;
        }

        var campanha = acesso.Valor!;
        string?[] imagensAntigas = [campanha.CapaPath, campanha.BannerPath];
        var mudou = campanha.Editar(request.Nome.Trim(), request.Descricao, request.CapaPath, request.BannerPath, relogio.GetUtcNow());
        await campanhas.SalvarAlteracoesAsync(cancellationToken);

        await ApagarImagensAsync(imagensAntigas.Where(path => path != campanha.CapaPath && path != campanha.BannerPath), cancellationToken);

        var editada = await RecarregarAsync(campanhaId, cancellationToken);
        if (mudou)
        {
            foreach (var jogador in editada.Jogadores)
            {
                await notificador.NotificarAsync(jogador.Pessoa.Id, new CampanhaEditada(campanha.Id, campanha.Nome), cancellationToken);
            }
        }

        return Detalhe(editada, userId);
    }

    /// <summary>
    /// Só o mestre, com o nome da campanha e a frase de confirmação. Apaga a campanha com os jogadores e as posições
    /// nas grades e os convites, apaga capa e banner do storage e avisa os jogadores (nº 18) e quem tinha convite
    /// pendente (nº 12).
    /// </summary>
    public async Task<Resultado<Vazio>> ExcluirAsync(
        Guid userId, Guid campanhaId, ExcluirCampanhaRequest request, CancellationToken cancellationToken)
    {
        var acesso = await acessos.DeMestreAsync(userId, campanhaId, cancellationToken);
        if (!acesso.Sucesso)
        {
            return acesso.Erro!;
        }

        var campanha = acesso.Valor!;
        if (!ConfirmacaoDeExclusao.Confere(campanha.Nome, request.ConfirmacaoNome, request.ConfirmacaoFrase))
        {
            return ErrosDeCampanha.ConfirmacaoIncorreta;
        }

        // Os jogadores, o nome do mestre e quem tinha convite pendente, antes que o cascade apague tudo.
        var guardada = await RecarregarAsync(campanhaId, cancellationToken);
        var avisosDeConvite = await convites.AvisosDeCampanhaExcluidaAsync(campanha, cancellationToken);

        campanhas.Remover(campanha);
        await campanhas.SalvarAlteracoesAsync(cancellationToken);

        await ApagarImagensAsync([campanha.CapaPath, campanha.BannerPath], cancellationToken);
        foreach (var jogador in guardada.Jogadores)
        {
            await notificador.NotificarAsync(
                jogador.Pessoa.Id, new CampanhaExcluida(campanha.Nome, guardada.Mestre.Nome), cancellationToken);
        }

        foreach (var aviso in avisosDeConvite)
        {
            await notificador.NotificarAsync(aviso.UserId, aviso.Aviso, cancellationToken);
        }

        return Vazio.Valor;
    }

    public async Task<IReadOnlyList<CampanhaCardResponse>> ListarAsync(Guid userId, CancellationToken cancellationToken) =>
        [.. (await campanhas.ListarDoUsuarioAsync(userId, cancellationToken)).Select(c => Card(c, userId))];

    public async Task<Resultado<CampanhaDetalheResponse>> DetalheAsync(Guid userId, Guid campanhaId, CancellationToken cancellationToken)
    {
        var guardada = await DeMembroAsync(userId, campanhaId, cancellationToken);
        return guardada.Sucesso ? Detalhe(guardada.Valor!, userId) : guardada.Erro!;
    }

    /// <summary>
    /// O mestre primeiro (entrou na criação da campanha), depois os jogadores na ordem em que entraram.
    /// </summary>
    public async Task<Resultado<MembroDaCampanhaResponse[]>> MembrosAsync(
        Guid userId, Guid campanhaId, CancellationToken cancellationToken)
    {
        var guardada = await DeMembroAsync(userId, campanhaId, cancellationToken);
        if (!guardada.Sucesso)
        {
            return guardada.Erro!;
        }

        var (campanha, mestre, jogadores) = (guardada.Valor!.Campanha, guardada.Valor.Mestre, guardada.Valor.Jogadores);
        MembroDaCampanhaResponse[] membros =
        [
            Membro(mestre, PapeisNaCampanha.Mestre, campanha.CreatedAt),
            .. jogadores.Select(j => Membro(j.Pessoa, PapeisNaCampanha.Jogador, j.EntrouEm)),
        ];
        return membros;
    }

    private async Task<Resultado<CampanhaGuardada>> DeMembroAsync(Guid userId, Guid campanhaId, CancellationToken cancellationToken)
    {
        var acesso = await acessos.DeMembroAsync(userId, campanhaId, cancellationToken);
        if (!acesso.Sucesso)
        {
            return acesso.Erro!;
        }

        return await campanhas.BuscarComPessoasAsync(campanhaId, cancellationToken) is { } guardada
            ? guardada
            : ErrosDeCampanha.CampanhaNaoEncontrada;
    }

    // A campanha com as pessoas, logo depois de o acesso confirmar que ela existe.
    private async Task<CampanhaGuardada> RecarregarAsync(Guid campanhaId, CancellationToken cancellationToken) =>
        await campanhas.BuscarComPessoasAsync(campanhaId, cancellationToken)
            ?? throw new InvalidOperationException($"A campanha {campanhaId} sumiu no meio da requisição.");

    // A campanha já foi salva: um arquivo órfão no bucket não justifica falhar a requisição.
    private async Task ApagarImagensAsync(IEnumerable<string?> paths, CancellationToken cancellationToken)
    {
        foreach (var path in paths.OfType<string>())
        {
            try
            {
                await storage.ApagarAsync(path, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao apagar a imagem de campanha {Path} do storage", path);
            }
        }
    }

    // Mesma regra do avatar: só vale imagem que o próprio usuário subiu (campaigns/{userId}/...).
    private static bool ImagensDoUsuario(Guid userId, params string?[] paths) =>
        paths.All(path => path is null || CaminhoDeImagemDeCampanha.PertenceAo(userId, path));

    private CampanhaCardResponse Card(CampanhaGuardada guardada, Guid userId)
    {
        var campanha = guardada.Campanha;
        return new CampanhaCardResponse(
            campanha.Id,
            campanha.Nome,
            new SistemaDaCampanhaResponse(guardada.Sistema.Slug, guardada.Sistema.Nome),
            campanha.OwnerId == userId ? PapeisNaCampanha.Mestre : PapeisNaCampanha.Jogador,
            Url(campanha.CapaPath),
            Pessoa(guardada.Mestre),
            [.. guardada.Jogadores.Select(j => Pessoa(j.Pessoa))],
            // TODO(sessoes): a próxima sessão agendada.
            ProximaSessao: null);
    }

    private CampanhaDetalheResponse Detalhe(CampanhaGuardada guardada, Guid userId)
    {
        var card = Card(guardada, userId);
        var campanha = guardada.Campanha;
        return new CampanhaDetalheResponse(
            card.Id,
            card.Nome,
            card.Sistema,
            card.Papel,
            card.CapaUrl,
            card.Mestre,
            card.Jogadores,
            card.ProximaSessao,
            campanha.Descricao,
            Url(campanha.BannerPath),
            campanha.CreatedAt,
            // TODO(sessoes, documentos): sessões jogadas, horas de mesa e documentos.
            new EstatisticasDaCampanhaResponse(SessoesJogadas: 0, HorasDeMesa: 0, Documentos: 0),
            // TODO(sessoes): o resumo da última sessão jogada.
            OndeParamos: null,
            // TODO(sessoes, documentos): só o que é público.
            AtividadeRecente: []);
    }

    private MembroDaCampanhaResponse Membro(PessoaGuardada pessoa, string papel, DateTimeOffset entrouEm) =>
        MembroDaCampanhaResponse.De(pessoa, papel, entrouEm, Url);

    private PessoaDaCampanhaResponse Pessoa(PessoaGuardada pessoa) => new(pessoa.Id, pessoa.Nome, pessoa.Nick, Url(pessoa.AvatarPath));

    private string? Url(string? path) => path is null ? null : storage.ObterUrlPublica(path);
}
