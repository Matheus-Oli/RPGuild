using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RPGuild.Domain.Notificacoes;
using RPGuild.Infrastructure.Identity;

namespace RPGuild.Infrastructure.Persistence.Configurations;

public sealed class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Tipo).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Dados).HasColumnType("jsonb").IsRequired();
        builder.Ignore(n => n.Lida);

        // A lista do sininho: do usuário, as mais novas primeiro, desempatando pelo id (paginação por cursor).
        builder.HasIndex(n => new { n.UserId, n.CreatedAt, n.Id }).IsDescending(false, true, false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .HasConstraintName("fk_notifications_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
