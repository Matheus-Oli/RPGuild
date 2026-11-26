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

    Task<DadosDoPerfil?> BuscarPerfilAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Troca o e-mail (e o UserName) pelo pendente se o token for válido para ele, e limpa o pendente.
    /// Erros: token inválido, expirado ou já usado; e-mail tomado por outra conta depois do pedido.
    /// </summary>
    Task<Resultado<EmailTrocado>> ConfirmarTrocaDeEmailAsync(TokenDeRedefinicao token, CancellationToken cancellationToken);

    /// <summary>
    /// Troca a senha conferindo a atual. Erro: senha atual incorreta.
    /// </summary>
    Task<Resultado<UsuarioResponse>> TrocarSenhaAsync(Guid userId, string atual, string nova, CancellationToken cancellationToken);

    /// <summary>
    /// Troca nome, nick e avatar. Erros: nick em uso por outro usuário, ou usuário que não existe mais.
    /// </summary>
    Task<Resultado<PerfilAtualizado>> AtualizarPerfilAsync(
        Guid userId, string nome, Nick nick, string? avatarPath, CancellationToken cancellationToken);

    /// <summary>
    /// Confere a senha, grava o e-mail pendente e gera o token de troca. O e-mail atual não muda.
    /// Erros: senha incorreta, mesmo e-mail, e-mail em uso por outra conta.
    /// </summary>
    Task<Resultado<DadosDeTrocaDeEmail>> PedirTrocaDeEmailAsync(
        Guid userId, string senha, string novoEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Gera o token de redefinição de senha do usuário com este e-mail, ou null se não houver conta.
    /// </summary>
    Task<DadosDeRedefinicao?> GerarTokenDeRedefinicaoAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Troca a senha se o token for válido. Devolve false para token inválido, expirado ou já usado.
    /// </summary>
    Task<bool> RedefinirSenhaAsync(TokenDeRedefinicao token, string novaSenha, CancellationToken cancellationToken);
}
