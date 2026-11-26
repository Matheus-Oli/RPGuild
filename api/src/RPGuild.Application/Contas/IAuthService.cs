using RPGuild.Application.Comum;

namespace RPGuild.Application.Contas;

public interface IAuthService
{
    Task<Resultado<UsuarioResponse>> RegistrarAsync(RegistrarRequest request, CancellationToken cancellationToken);

    Task<Resultado<SessaoAutenticada>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Resultado<SessaoAutenticada>> RenovarAsync(string? refreshToken, CancellationToken cancellationToken);

    Task<Resultado<PerfilResponse>> ObterPerfilAsync(Guid userId, CancellationToken cancellationToken);
}
