using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RPGuild.Domain.Campanhas;
using RPGuild.Domain.Contas;
using RPGuild.Domain.Notificacoes;
using RPGuild.Domain.Taberna;
using RPGuild.Infrastructure.Identity;

namespace RPGuild.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityUserContext<ApplicationUser, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Amizade> Amizades => Set<Amizade>();

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    public DbSet<SistemaDeJogo> SistemasDeJogo => Set<SistemaDeJogo>();

    public DbSet<Campanha> Campanhas => Set<Campanha>();

    public DbSet<MembroDaCampanha> MembrosDeCampanha => Set<MembroDaCampanha>();

    public DbSet<PosicaoNaGrade> OrdemDasCampanhas => Set<PosicaoNaGrade>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
