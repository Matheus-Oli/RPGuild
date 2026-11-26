using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RPGuild.Application.Contas;

namespace RPGuild.Infrastructure.Identity;

public sealed class JwtTokenService(JwtOptions options, TimeProvider relogio) : ITokenService
{
    public const string ClaimNick = "nick";

    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);

    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken GerarAccessToken(UsuarioResponse usuario, Guid sessaoId)
    {
        var agora = relogio.GetUtcNow();
        var expiraEm = agora + Validade;

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = usuario.Email,
                [JwtRegisteredClaimNames.Name] = usuario.Nome,
                [ClaimNick] = usuario.Nick,
                [JwtRegisteredClaimNames.Sid] = sessaoId.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(ChaveDeAssinatura(options), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiraEm);
    }

    public static SymmetricSecurityKey ChaveDeAssinatura(JwtOptions options) => new(Encoding.UTF8.GetBytes(options.Key));
}
