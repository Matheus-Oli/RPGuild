using RPGuild.Application.Comum;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public sealed class AuthService(IIdentityService identity) : IAuthService
{
    public Task<Resultado<UsuarioResponse>> RegistrarAsync(RegistrarRequest request, CancellationToken cancellationToken) =>
        identity.CriarUsuarioAsync(
            request.Nome.Trim(), Nick.Criar(request.Nick), request.Email.Trim(), request.Senha, cancellationToken);
}
