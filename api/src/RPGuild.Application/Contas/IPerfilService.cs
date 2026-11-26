using RPGuild.Application.Comum;

namespace RPGuild.Application.Contas;

/// <summary>
/// Casos de uso do próprio usuário logado (/api/me).
/// </summary>
public interface IPerfilService
{
    Task<Resultado<PerfilResponse>> AtualizarAsync(Guid userId, AtualizarPerfilRequest request, CancellationToken cancellationToken);
}
