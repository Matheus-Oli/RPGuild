using RPGuild.Application.Comum;

namespace RPGuild.Application.Campanhas;

public interface IGradeDeCampanhasRepository
{
    /// <summary>
    /// Campanhas da grade do usuário, na ordem gravada.
    /// </summary>
    Task<IReadOnlyList<Guid>> OrdemAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Campanhas em que o usuário é mestre ou jogador.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> CampanhasDoUsuarioAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Troca a ordem inteira do usuário, numa transação.
    /// </summary>
    Task GravarOrdemAsync(Guid userId, IReadOnlyList<Guid> ordem, CancellationToken cancellationToken);
}

/// <summary>
/// A ordem da grade de campanhas de cada usuário (mestre ou jogador). Campanha nova entra no início, então quem nunca
/// reordenou vê a mais nova primeiro.
/// </summary>
public sealed class GradeDeCampanhas(IGradeDeCampanhasRepository grade)
{
    public async Task ColocarNoInicioAsync(Guid userId, Guid campanhaId, CancellationToken cancellationToken)
    {
        var atual = await grade.OrdemAsync(userId, cancellationToken);
        await grade.GravarOrdemAsync(userId, [campanhaId, .. atual.Where(id => id != campanhaId)], cancellationToken);
    }

    /// <summary>
    /// A lista precisa ter exatamente as campanhas do usuário: sem repetir, sem faltar e sem sobrar.
    /// </summary>
    public async Task<Resultado<Vazio>> ReordenarAsync(Guid userId, IReadOnlyList<Guid> ordem, CancellationToken cancellationToken)
    {
        var campanhas = await grade.CampanhasDoUsuarioAsync(userId, cancellationToken);
        var exata = ordem.Count == campanhas.Count && ordem.Distinct().Count() == ordem.Count && ordem.All(campanhas.Contains);
        if (!exata)
        {
            return ErrosDeCampanha.OrdemInvalida;
        }

        await grade.GravarOrdemAsync(userId, ordem, cancellationToken);
        return Vazio.Valor;
    }
}
