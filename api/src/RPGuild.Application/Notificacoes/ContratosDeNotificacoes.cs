using System.Text.Json;
using FluentValidation;

namespace RPGuild.Application.Notificacoes;

/// <param name="NaoLidas">Só as não lidas.</param>
/// <param name="Cursor">O <c>proximoCursor</c> da página anterior; sem ele, começa das mais novas.</param>
/// <param name="Limite">Quantas por página: padrão 20, no máximo 50.</param>
public sealed record ListarNotificacoesRequest(bool NaoLidas = false, string? Cursor = null, int? Limite = null)
{
    public const int LimitePadrao = 20;
    public const int LimiteMaximo = 50;

    public int LimiteEfetivo => Math.Min(Limite ?? LimitePadrao, LimiteMaximo);
}

public sealed class ListarNotificacoesRequestValidator : AbstractValidator<ListarNotificacoesRequest>
{
    public ListarNotificacoesRequestValidator()
    {
        RuleFor(r => r.Limite)
            .GreaterThanOrEqualTo(1).WithMessage("O limite precisa ser pelo menos 1.");

        RuleFor(r => r.Cursor)
            .Must(cursor => string.IsNullOrEmpty(cursor) || CursorDeNotificacoes.Decodificar(cursor) is not null)
            .WithMessage("Cursor inválido. Use o proximoCursor devolvido pela página anterior.");
    }
}

/// <param name="Dados">O objeto JSON gravado com a notificação; o front monta o texto a partir dele e do tipo.</param>
public sealed record NotificacaoResponse(Guid Id, string Tipo, JsonElement Dados, bool Lida, DateTimeOffset CriadoEm);

/// <param name="ProximoCursor">Null na última página.</param>
public sealed record PaginaDeNotificacoesResponse(IReadOnlyList<NotificacaoResponse> Itens, string? ProximoCursor);

public sealed record ContagemDeNaoLidasResponse(int Total);
