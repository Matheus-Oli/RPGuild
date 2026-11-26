using RPGuild.Application.Comum;

namespace RPGuild.Application.Contas;

/// <summary>
/// Casos de uso do próprio usuário logado (/api/me).
/// </summary>
public interface IPerfilService
{
    Task<Resultado<PerfilResponse>> AtualizarAsync(Guid userId, AtualizarPerfilRequest request, CancellationToken cancellationToken);

    Task<Resultado<Vazio>> PedirTrocaDeEmailAsync(Guid userId, TrocarEmailRequest request, CancellationToken cancellationToken);

    /// <param name="sessaoAtual">Claim "sid" do Bearer, se veio um; essa sessão continua aberta.</param>
    Task<Resultado<Vazio>> ConfirmarTrocaDeEmailAsync(
        ConfirmarTrocaDeEmailRequest request, Guid? sessaoAtual, CancellationToken cancellationToken);
}
