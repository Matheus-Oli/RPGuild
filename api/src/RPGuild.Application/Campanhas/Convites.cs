using FluentValidation;
using RPGuild.Application.Comum;
using RPGuild.Domain.Campanhas;

namespace RPGuild.Application.Campanhas;

/// <param name="UserIds">Amigos do mestre que vão receber o convite. Sem repetir.</param>
public sealed record ConvidarRequest(IReadOnlyList<Guid> UserIds);

public sealed class ConvidarRequestValidator : AbstractValidator<ConvidarRequest>
{
    public ConvidarRequestValidator()
    {
        RuleFor(r => r.UserIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe quem vai ser convidado.")
            .SemRepetidos();
    }
}

public sealed record ConviteDaCampanhaResponse(Guid Id, PessoaDaCampanhaResponse Convidado, DateTimeOffset CriadoEm);

public sealed record CampanhaDoConviteResponse(Guid Id, string Nome, SistemaDaCampanhaResponse Sistema, string? CapaUrl);

/// <summary>
/// Um convite pendente que eu recebi. Nunca leva e-mail.
/// </summary>
public sealed record MeuConviteResponse(Guid Id, CampanhaDoConviteResponse Campanha, PessoaDaCampanhaResponse Mestre, DateTimeOffset CriadoEm);

/// <summary>
/// Convite pendente com o convidado, como está guardado.
/// </summary>
public sealed record ConviteComConvidado(Guid Id, PessoaGuardada Convidado, DateTimeOffset CriadoEm);

/// <summary>
/// Convite pendente recebido, com a campanha e o mestre, como está guardado.
/// </summary>
public sealed record ConviteGuardado(
    Guid Id, Guid CampanhaId, string CampanhaNome, SistemaDeJogo Sistema, string? CapaPath, PessoaGuardada Mestre, DateTimeOffset CriadoEm);

/// <param name="Motivo">Um dos <see cref="MotivosDeConvite"/>.</param>
public sealed record ConvidadoInvalido(Guid UserId, string Motivo);

public static class TextoDoStatus
{
    /// <summary>
    /// O status como sai nas respostas e nos eventos: pendente, aceito, recusado ou cancelado.
    /// </summary>
    public static string Texto(this StatusDoConvite status) => status.ToString().ToLowerInvariant();
}

public static class MotivosDeConvite
{
    /// <summary>Não é amigo do mestre (400).</summary>
    public const string NaoAmigo = "nao_amigo";

    /// <summary>É o próprio mestre (400).</summary>
    public const string Mestre = "mestre";

    /// <summary>Já é jogador da campanha (409).</summary>
    public const string JaJogador = "ja_jogador";

    /// <summary>Já tem um convite pendente na campanha (409).</summary>
    public const string JaConvidado = "ja_convidado";

    public static bool EhConflito(string motivo) => motivo is JaJogador or JaConvidado;
}

public interface IConviteRepository
{
    Task AdicionarAsync(ConviteDeCampanha convite, CancellationToken cancellationToken);

    Task<ConviteDeCampanha?> BuscarPorIdAsync(Guid conviteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConviteDeCampanha>> PendentesDaCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken);

    /// <summary>
    /// Os pendentes para <paramref name="convidadoId"/> em campanhas de <paramref name="mestreId"/>.
    /// </summary>
    Task<IReadOnlyList<ConviteDeCampanha>> PendentesEntreAsync(Guid mestreId, Guid convidadoId, CancellationToken cancellationToken);

    /// <summary>
    /// Os pendentes da campanha com o convidado, os mais novos primeiro.
    /// </summary>
    Task<IReadOnlyList<ConviteComConvidado>> ListarPendentesDaCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken);

    /// <summary>
    /// Os pendentes que o usuário recebeu, com a campanha e o mestre, os mais novos primeiro.
    /// </summary>
    Task<IReadOnlyList<ConviteGuardado>> ListarPendentesDoUsuarioAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Salva convites e jogadores. Corridas viram <see cref="ConvitePendenteJaExisteException"/> (dois convites pendentes
    /// para a mesma pessoa) ou <see cref="JogadorJaNaCampanhaException"/> (dois aceites do mesmo convite).
    /// </summary>
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}

public static class ErrosDeConvite
{
    /// <summary>
    /// Nome da extensão do ProblemDetails com os convidados recusados: [{userId, motivo}].
    /// </summary>
    public const string ExtensaoConvidados = "convidados";

    public static readonly Erro ConviteNaoEncontrado = new(
        TipoErro.NaoEncontrado, "Convite não encontrado", "O convite não existe ou não é para você.");

    public static readonly Erro ConviteNaoPendente = new(
        TipoErro.Conflito, "Convite já respondido", "Este convite não está mais pendente.");

    /// <summary>
    /// 400 se algum motivo é de entrada (não amigo, o próprio mestre); senão, 409.
    /// </summary>
    public static Erro ConvidadosInvalidos(IReadOnlyList<ConvidadoInvalido> invalidos) => invalidos.All(i => MotivosDeConvite.EhConflito(i.Motivo))
        ? new Erro(
            TipoErro.Conflito,
            "Convite não enviado",
            "Alguém da lista já está na campanha ou já tem um convite pendente.",
            new Dictionary<string, object> { [ExtensaoConvidados] = invalidos })
        : new Erro(
            TipoErro.Validacao,
            "Convite não enviado",
            "Só dá para convidar amigos seus, e não a si mesmo.",
            new Dictionary<string, object> { [ExtensaoConvidados] = invalidos });
}
