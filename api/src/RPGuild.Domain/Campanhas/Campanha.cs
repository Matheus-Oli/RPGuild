namespace RPGuild.Domain.Campanhas;

/// <summary>
/// Sistema de RPG (D&amp;D 2024, Tormenta 20...). Só os ativos aceitam campanha nova.
/// </summary>
public sealed class SistemaDeJogo
{
    private SistemaDeJogo()
    {
    }

    public SistemaDeJogo(int id, string slug, string nome, bool ativo)
    {
        Id = id;
        Slug = slug;
        Nome = nome;
        Ativo = ativo;
    }

    public int Id { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string Nome { get; private set; } = string.Empty;

    public bool Ativo { get; private set; }
}

/// <summary>
/// Campanha. Quem cria é o mestre (<see cref="OwnerId"/>); os jogadores ficam em <see cref="MembroDaCampanha"/>.
/// O sistema não muda depois de criada.
/// </summary>
public sealed class Campanha
{
    private Campanha()
    {
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public int SystemId { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string? Descricao { get; private set; }

    public string? CapaPath { get; private set; }

    public string? BannerPath { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Campanha Criar(
        Guid mestreId, int sistemaId, string nome, string? descricao, string? capaPath, string? bannerPath, DateTimeOffset agora) => new()
    {
        Id = Guid.CreateVersion7(agora),
        OwnerId = mestreId,
        SystemId = sistemaId,
        Nome = nome,
        Descricao = descricao,
        CapaPath = capaPath,
        BannerPath = bannerPath,
        CreatedAt = agora,
        UpdatedAt = agora,
    };

    /// <summary>
    /// Troca nome, descrição e imagens. Devolve se algo mudou (só então o updated_at anda).
    /// </summary>
    public bool Editar(string nome, string? descricao, string? capaPath, string? bannerPath, DateTimeOffset agora)
    {
        if (nome == Nome && descricao == Descricao && capaPath == CapaPath && bannerPath == BannerPath)
        {
            return false;
        }

        Nome = nome;
        Descricao = descricao;
        CapaPath = capaPath;
        BannerPath = bannerPath;
        UpdatedAt = agora;
        return true;
    }
}

/// <summary>
/// Jogador da campanha (o mestre não entra aqui). Quem cria a linha é o aceite do convite.
/// </summary>
public sealed class MembroDaCampanha
{
    private MembroDaCampanha()
    {
    }

    public MembroDaCampanha(Guid campaignId, Guid userId, DateTimeOffset joinedAt)
    {
        CampaignId = campaignId;
        UserId = userId;
        JoinedAt = joinedAt;
    }

    public Guid CampaignId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }
}

/// <summary>
/// Posição de uma campanha na grade de um usuário (mestre ou jogador). 0 é a primeira.
/// </summary>
public sealed class PosicaoNaGrade
{
    private PosicaoNaGrade()
    {
    }

    public PosicaoNaGrade(Guid userId, Guid campaignId, int posicao)
    {
        UserId = userId;
        CampaignId = campaignId;
        Posicao = posicao;
    }

    public Guid UserId { get; private set; }

    public Guid CampaignId { get; private set; }

    public int Posicao { get; private set; }
}
