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

    Task<Resultado<UsuarioResponse>> VerificarCredenciaisAsync(string email, string senha, CancellationToken cancellationToken);

    Task<PerfilResponse?> BuscarPerfilAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Troca nome, nick e avatar. Erros: nick em uso por outro usuário, ou usuário que não existe mais.
    /// </summary>
    Task<Resultado<PerfilAtualizado>> AtualizarPerfilAsync(
        Guid userId, string nome, Nick nick, string? avatarPath, CancellationToken cancellationToken);

    /// <summary>
    /// Gera o token de redefinição de senha do usuário com este e-mail, ou null se não houver conta.
    /// </summary>
    Task<DadosDeRedefinicao?> GerarTokenDeRedefinicaoAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Troca a senha se o token for válido. Devolve false para token inválido, expirado ou já usado.
    /// </summary>
    Task<bool> RedefinirSenhaAsync(TokenDeRedefinicao token, string novaSenha, CancellationToken cancellationToken);
}
