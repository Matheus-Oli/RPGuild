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

/// <param name="Motivo">Um dos <see cref="MotivosDeConvite"/>.</param>
public sealed record ConvidadoInvalido(Guid UserId, string Motivo);

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
