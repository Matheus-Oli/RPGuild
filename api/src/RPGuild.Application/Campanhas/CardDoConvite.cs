using RPGuild.Domain.Campanhas;

namespace RPGuild.Application.Campanhas;

public sealed record CampanhaDoCardResponse(
    Guid Id, string Nome, string? Descricao, SistemaDaCampanhaResponse Sistema, string? CapaUrl, string? BannerUrl);

public sealed record QuemConvidouResponse(string Nome, string Nick);

public sealed record ParaQuemResponse(string Nick);

/// <summary>
/// O card do convite como alguém o vê: completo (<see cref="CardDoConviteResponse"/>) para o convidado e o mestre, ou só
/// para quem é (<see cref="CardDoConviteParaOutroResponse"/>) para os outros.
/// </summary>
public abstract record CardDoConviteVisto(Guid Id, bool PodeResponder, ParaQuemResponse Para);

/// <summary>
/// O card que o convidado e o mestre veem. Nunca leva e-mail.
/// </summary>
/// <param name="Status">pendente, aceito, recusado ou cancelado.</param>
/// <param name="PodeResponder">Só o convidado, e só com o convite pendente.</param>
/// <param name="NaMesa">O mestre e os jogadores.</param>
public sealed record CardDoConviteResponse(
    Guid Id,
    string Status,
    bool PodeResponder,
    CampanhaDoCardResponse Campanha,
    PessoaDaCampanhaResponse Mestre,
    IReadOnlyList<PessoaDaCampanhaResponse> NaMesa,
    QuemConvidouResponse ConvidadoPor,
    ParaQuemResponse Para,
    DateTimeOffset CriadoEm) : CardDoConviteVisto(Id, PodeResponder, Para);

/// <summary>
/// O que outro usuário logado vê do convite ("Este convite é para outra pessoa"): nada da campanha.
/// </summary>
public sealed record CardDoConviteParaOutroResponse(Guid Id, bool PodeResponder, ParaQuemResponse Para)
    : CardDoConviteVisto(Id, PodeResponder, Para);

public static class CardDoConvite
{
    /// <summary>
    /// O card de acordo com quem pede: o convidado e o mestre veem tudo; qualquer outro, só para quem é.
    /// </summary>
    /// <param name="url">Monta a URL pública de um path do storage (null fica null).</param>
    public static CardDoConviteVisto Montar(
        ConviteDeCampanha convite,
        CampanhaGuardada campanha,
        PessoaGuardada convidadoPor,
        PessoaGuardada convidado,
        Guid quemPede,
        Func<string?, string?> url)
    {
        var para = new ParaQuemResponse(convidado.Nick);
        if (quemPede != convite.UserId && quemPede != campanha.Campanha.OwnerId)
        {
            return new CardDoConviteParaOutroResponse(convite.Id, PodeResponder: false, para);
        }

        PessoaDaCampanhaResponse Pessoa(PessoaGuardada p) => new(p.Id, p.Nome, p.Nick, url(p.AvatarPath));

        var mestre = Pessoa(campanha.Mestre);
        return new CardDoConviteResponse(
            convite.Id,
            convite.Status.ToString().ToLowerInvariant(),
            PodeResponder: quemPede == convite.UserId && convite.EstaPendente,
            new CampanhaDoCardResponse(
                campanha.Campanha.Id,
                campanha.Campanha.Nome,
                campanha.Campanha.Descricao,
                new SistemaDaCampanhaResponse(campanha.Sistema.Slug, campanha.Sistema.Nome),
                url(campanha.Campanha.CapaPath),
                url(campanha.Campanha.BannerPath)),
            mestre,
            [mestre, .. campanha.Jogadores.Select(j => Pessoa(j.Pessoa))],
            new QuemConvidouResponse(convidadoPor.Nome, convidadoPor.Nick),
            para,
            convite.CreatedAt);
    }
}
