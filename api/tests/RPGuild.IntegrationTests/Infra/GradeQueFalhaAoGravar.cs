using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RPGuild.Application.Campanhas;
using RPGuild.Infrastructure.Campanhas;
using RPGuild.Infrastructure.Persistence;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// A grade de verdade, mas a gravação da ordem falha depois de gravar as linhas: simula o banco caindo no meio.
/// </summary>
public sealed class GradeQueFalhaAoGravar(GradeDeCampanhasRepository real) : IGradeDeCampanhasRepository
{
    public static void Registrar(IServiceCollection services) =>
        services.Replace(ServiceDescriptor.Scoped<IGradeDeCampanhasRepository>(
            sp => new GradeQueFalhaAoGravar(new GradeDeCampanhasRepository(sp.GetRequiredService<AppDbContext>()))));

    public Task<IReadOnlyList<Guid>> OrdemAsync(Guid userId, CancellationToken cancellationToken) =>
        real.OrdemAsync(userId, cancellationToken);

    public Task<IReadOnlyCollection<Guid>> CampanhasDoUsuarioAsync(Guid userId, CancellationToken cancellationToken) =>
        real.CampanhasDoUsuarioAsync(userId, cancellationToken);

    public async Task GravarOrdemAsync(Guid userId, IReadOnlyList<Guid> ordem, CancellationToken cancellationToken)
    {
        await real.GravarOrdemAsync(userId, ordem, cancellationToken);
        throw new InvalidOperationException("Falha ao gravar a ordem da grade (simulada nos testes).");
    }
}
