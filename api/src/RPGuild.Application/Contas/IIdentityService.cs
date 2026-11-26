using RPGuild.Application.Comum;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

/// <summary>
/// Porta para o armazenamento de usuários e senhas (ASP.NET Identity, na Infrastructure).
/// </summary>
public interface IIdentityService
{
    Task<Resultado<UsuarioResponse>> CriarUsuarioAsync(
        string nome, Nick nick, string email, string senha, CancellationToken cancellationToken);
}
