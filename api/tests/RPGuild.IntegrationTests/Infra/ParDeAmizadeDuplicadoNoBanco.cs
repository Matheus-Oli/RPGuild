using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using RPGuild.Domain.Taberna;
using RPGuild.Infrastructure.Persistence.Configurations;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Simula a corrida de dois pedidos em sentidos opostos: ao gravar um pedido novo, o Postgres responde com a
/// violação do índice único do par (23505 em ix_friendships_par), como se o outro pedido tivesse entrado antes.
/// </summary>
public sealed class ParDeAmizadeDuplicadoNoBanco : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var temPedidoNovo = eventData.Context?.ChangeTracker.Entries<Amizade>().Any(e => e.State == EntityState.Added) ?? false;
        if (temPedidoNovo)
        {
            var violacao = new PostgresException(
                messageText: $"duplicate key value violates unique constraint \"{AmizadeConfiguration.IndicePar}\"",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                tableName: "friendships",
                constraintName: AmizadeConfiguration.IndicePar);
            throw new DbUpdateException("Simulação de pedido cruzado gravado ao mesmo tempo.", violacao);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
