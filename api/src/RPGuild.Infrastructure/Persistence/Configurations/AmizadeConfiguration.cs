using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RPGuild.Domain.Taberna;
using RPGuild.Infrastructure.Identity;

namespace RPGuild.Infrastructure.Persistence.Configurations;

public sealed class AmizadeConfiguration : IEntityTypeConfiguration<Amizade>
{
    // O índice único do par ordenado (ix_friendships_par, sobre LEAST/GREATEST dos ids) é de expressão
    // e fica só na migration AddFriendships: o EF não modela esse tipo de índice.
    public void Configure(EntityTypeBuilder<Amizade> builder)
    {
        builder.ToTable("friendships");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        // Guardado como texto em minúsculas: pendente, aceita, recusada.
        builder.Property(a => a.Status)
            .HasConversion(status => status.ToString().ToLowerInvariant(), texto => Enum.Parse<StatusDaAmizade>(texto, true))
            .HasMaxLength(10)
            .IsRequired();

        // Nome fixo: esta configuração roda antes da que renomeia a tabela de usuários para "users".
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.RequesterId)
            .HasConstraintName("fk_friendships_users_requester_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.AddresseeId)
            .HasConstraintName("fk_friendships_users_addressee_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
