using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using RPGuild.Domain.Campanhas;
using RPGuild.Infrastructure.Persistence.Configurations;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Simula dois convites para a mesma pessoa ao mesmo tempo: ao gravar um convite novo, o Postgres responde com a violação
/// do índice único de pendente (23505 em ix_campaign_invites_pendente), como se o outro tivesse entrado antes.
/// </summary>
/// <param name="outroEntrouAntes">O que o "outro request" gravou antes (ex.: o convite pendente que ganhou a corrida).</param>
public sealed class ConvitePendenteDuplicadoNoBanco(Func<Task>? outroEntrouAntes = null) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context?.ChangeTracker.Entries<ConviteDeCampanha>().Any(e => e.State == EntityState.Added) ?? false)
        {
            if (outroEntrouAntes is not null)
            {
                await outroEntrouAntes();
            }

            throw ViolacaoDeUnique.Criar("campaign_invites", ConviteDeCampanhaConfiguration.IndicePendente);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

/// <summary>
/// Simula dois aceites do mesmo convite ao mesmo tempo: ao gravar o jogador, o Postgres responde com a violação da PK
/// de campaign_members (23505 em pk_campaign_members), como se o outro aceite tivesse entrado antes.
/// </summary>
public sealed class JogadorDuplicadoNoBanco : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context?.ChangeTracker.Entries<MembroDaCampanha>().Any(e => e.State == EntityState.Added) ?? false)
        {
            throw ViolacaoDeUnique.Criar("campaign_members", "pk_campaign_members");
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

internal static class ViolacaoDeUnique
{
    public static DbUpdateException Criar(string tabela, string restricao) => new(
        "Simulação de gravação concorrente.",
        new PostgresException(
            messageText: $"duplicate key value violates unique constraint \"{restricao}\"",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            tableName: tabela,
            constraintName: restricao));
}
