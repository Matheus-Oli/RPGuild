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

    public static IActionResult Problema(this ControllerBase controller, Erro erro)
    {
        var status = StatusDe(erro.Tipo);
        var problema = controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext, status, title: erro.Titulo, detail: erro.Detalhe);
        foreach (var (nome, valor) in erro.Extensoes ?? new Dictionary<string, object>())
        {
            problema.Extensions[nome] = valor;
        }

        return new ObjectResult(problema) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }

    public static int StatusDe(TipoErro tipo) => tipo switch
    {
        TipoErro.Validacao => StatusCodes.Status400BadRequest,
        TipoErro.NaoAutorizado => StatusCodes.Status401Unauthorized,
        TipoErro.Conflito => StatusCodes.Status409Conflict,
        TipoErro.Bloqueado => StatusCodes.Status423Locked,
        TipoErro.NaoEncontrado => StatusCodes.Status404NotFound,
        TipoErro.Proibido => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };
}
