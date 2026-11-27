using RPGuild.Domain.Campanhas;

namespace RPGuild.Application.Campanhas;

public sealed record SistemaDisponivelResponse(string Slug, string Nome, bool Ativo);

public interface ISistemasRepository
{
    /// <summary>
    /// Todos os sistemas, na ordem do seed.
    /// </summary>
    Task<IReadOnlyList<SistemaDeJogo>> ListarAsync(CancellationToken cancellationToken);

    Task<SistemaDeJogo?> BuscarPorSlugAsync(string slug, CancellationToken cancellationToken);
}

public sealed class SistemasService(ISistemasRepository sistemas)
{
    public async Task<IReadOnlyList<SistemaDisponivelResponse>> ListarAsync(CancellationToken cancellationToken) =>
        [.. (await sistemas.ListarAsync(cancellationToken)).Select(s => new SistemaDisponivelResponse(s.Slug, s.Nome, s.Ativo))];
}
