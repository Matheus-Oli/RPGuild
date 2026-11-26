using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace RPGuild.Api.Http;

/// <summary>
/// Roda o validator do FluentValidation de cada argumento da action e responde 400 em ValidationProblemDetails,
/// com os campos em camelCase, iguais aos do JSON.
/// </summary>
public sealed class ValidacaoFilter(IServiceProvider services, ProblemDetailsFactory problemDetails) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argumento in context.ActionArguments.Values.OfType<object>())
        {
            var tipoValidator = typeof(IValidator<>).MakeGenericType(argumento.GetType());
            if (services.GetService(tipoValidator) is not IValidator validator)
            {
                continue;
            }

            var resultado = await validator.ValidateAsync(new ValidationContext<object>(argumento), context.HttpContext.RequestAborted);
            foreach (var falha in resultado.Errors)
            {
                context.ModelState.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(falha.PropertyName), falha.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            var problema = problemDetails.CreateValidationProblemDetails(
                context.HttpContext, context.ModelState, StatusCodes.Status400BadRequest, title: "Dados inválidos");
            context.Result = new ObjectResult(problema)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" },
            };
            return;
        }

        await next();
    }
}
