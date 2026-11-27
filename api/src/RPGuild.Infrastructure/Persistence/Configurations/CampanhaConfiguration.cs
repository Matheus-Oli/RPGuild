using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RPGuild.Application.Campanhas;
using RPGuild.Domain.Campanhas;
using RPGuild.Infrastructure.Identity;

namespace RPGuild.Infrastructure.Persistence.Configurations;

public sealed class SistemaDeJogoConfiguration : IEntityTypeConfiguration<SistemaDeJogo>
{
    public void Configure(EntityTypeBuilder<SistemaDeJogo> builder)
    {
        builder.ToTable("game_systems");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Slug).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Nome).HasMaxLength(100).IsRequired();
        builder.HasIndex(s => s.Slug).IsUnique();

        // Na v1 só o D&D 2024 aceita campanha; os outros aparecem como "em breve".
        builder.HasData(
            new SistemaDeJogo(1, "dnd2024", "D&D 5e (regras de 2024)", ativo: true),
            new SistemaDeJogo(2, "ordem2", "Ordem Paranormal 2", ativo: false),
            new SistemaDeJogo(3, "t20", "Tormenta 20", ativo: false),
            new SistemaDeJogo(4, "coc", "Call of Cthulhu", ativo: false),
            new SistemaDeJogo(5, "deadlands", "Deadlands", ativo: false));
    }
}

public sealed class CampanhaConfiguration : IEntityTypeConfiguration<Campanha>
{
    public void Configure(EntityTypeBuilder<Campanha> builder)
    {
        builder.ToTable("campaigns");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Nome).HasMaxLength(RegrasDeCampanha.NomeTamanhoMaximo).IsRequired();
        builder.Property(c => c.Descricao).HasMaxLength(RegrasDeCampanha.DescricaoTamanhoMaximo);
        builder.Property(c => c.CapaPath).HasMaxLength(RegrasDeCampanha.ImagemPathTamanhoMaximo);
        builder.Property(c => c.BannerPath).HasMaxLength(RegrasDeCampanha.ImagemPathTamanhoMaximo);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .HasConstraintName("fk_campaigns_users_owner_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SistemaDeJogo>()
            .WithMany()
            .HasForeignKey(c => c.SystemId)
            .HasConstraintName("fk_campaigns_game_systems_system_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MembroDaCampanhaConfiguration : IEntityTypeConfiguration<MembroDaCampanha>
{
    public void Configure(EntityTypeBuilder<MembroDaCampanha> builder)
    {
        builder.ToTable("campaign_members");

        builder.HasKey(m => new { m.CampaignId, m.UserId });

        // "Minhas campanhas como jogador".
        builder.HasIndex(m => m.UserId);

        builder.HasOne<Campanha>()
            .WithMany()
            .HasForeignKey(m => m.CampaignId)
            .HasConstraintName("fk_campaign_members_campaigns_campaign_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .HasConstraintName("fk_campaign_members_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PosicaoNaGradeConfiguration : IEntityTypeConfiguration<PosicaoNaGrade>
{
    public void Configure(EntityTypeBuilder<PosicaoNaGrade> builder)
    {
        builder.ToTable("campaign_user_order");

        builder.HasKey(p => new { p.UserId, p.CampaignId });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .HasConstraintName("fk_campaign_user_order_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Campanha>()
            .WithMany()
            .HasForeignKey(p => p.CampaignId)
            .HasConstraintName("fk_campaign_user_order_campaigns_campaign_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ConviteDeCampanhaConfiguration : IEntityTypeConfiguration<ConviteDeCampanha>
{
    /// <summary>
    /// No máximo um convite pendente por (campanha, usuário). Recusados e cancelados ficam como histórico.
    /// </summary>
    public const string IndicePendente = "ix_campaign_invites_pendente";

    public void Configure(EntityTypeBuilder<ConviteDeCampanha> builder)
    {
        builder.ToTable("campaign_invites");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        // Guardado como texto em minúsculas: pendente, aceito, recusado, cancelado.
        builder.Property(c => c.Status)
            .HasConversion(status => status.ToString().ToLowerInvariant(), texto => Enum.Parse<StatusDoConvite>(texto, true))
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(c => new { c.CampaignId, c.UserId })
            .IsUnique()
            .HasFilter("status = 'pendente'")
            .HasDatabaseName(IndicePendente);

        // "Meus convites pendentes".
        builder.HasIndex(c => c.UserId);

        builder.HasOne<Campanha>()
            .WithMany()
            .HasForeignKey(c => c.CampaignId)
            .HasConstraintName("fk_campaign_invites_campaigns_campaign_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.InvitedBy)
            .HasConstraintName("fk_campaign_invites_users_invited_by")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .HasConstraintName("fk_campaign_invites_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
