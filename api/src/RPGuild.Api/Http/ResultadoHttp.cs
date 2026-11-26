using Microsoft.AspNetCore.Mvc;
using RPGuild.Application.Comum;

namespace RPGuild.Api.Http;

/// <summary>
/// Único ponto de tradução de <see cref="Erro"/> do Application para ProblemDetails.
/// </summary>
public static class ResultadoHttp
{
    public static IActionResult ParaResposta<T>(this ControllerBase controller, Resultado<T> resultado, Func<T, IActionResult> sucesso) =>
        resultado.Sucesso ? sucesso(resultado.Valor!) : controller.Problema(resultado.Erro!);

    public static IActionResult Problema(this ControllerBase controller, Erro erro) =>
        controller.Problem(title: erro.Titulo, detail: erro.Detalhe, statusCode: StatusDe(erro.Tipo));

    public static int StatusDe(TipoErro tipo) => tipo switch
    {
        TipoErro.Validacao => StatusCodes.Status400BadRequest,
        TipoErro.NaoAutorizado => StatusCodes.Status401Unauthorized,
        TipoErro.Conflito => StatusCodes.Status409Conflict,
        TipoErro.Bloqueado => StatusCodes.Status423Locked,
        _ => StatusCodes.Status500InternalServerError,
    };
}
