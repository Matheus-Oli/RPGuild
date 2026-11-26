using Microsoft.IdentityModel.JsonWebTokens;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Lê as claims do access token, sem validar a assinatura (quem valida é a API).
/// </summary>
public static class ClaimsDoToken
{
    private static readonly JsonWebTokenHandler Handler = new();

    public static Guid UserId(string accessToken) => Guid.Parse(Valor(accessToken, JwtRegisteredClaimNames.Sub));

    public static Guid Sid(string accessToken) => Guid.Parse(Valor(accessToken, JwtRegisteredClaimNames.Sid));

    private static string Valor(string accessToken, string claim)
    {
        var token = Handler.ReadJsonWebToken(accessToken);
        token.TryGetPayloadValue<string>(claim, out var valor).ShouldBeTrue($"O access token não tem a claim '{claim}'.");
        return valor;
    }
}
