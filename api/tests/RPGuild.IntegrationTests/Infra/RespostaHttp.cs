using System.Net;

namespace RPGuild.IntegrationTests.Infra;

public static class RespostaHttp
{
    /// <summary>
    /// Confere o status e que o corpo é um ProblemDetails (application/problem+json).
    /// </summary>
    public static void DeveSerProblema(this HttpResponseMessage resposta, HttpStatusCode status)
    {
        resposta.StatusCode.ShouldBe(status);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
