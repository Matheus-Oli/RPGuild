using System.Text;
using Microsoft.Extensions.Configuration;

namespace RPGuild.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    /// <summary>
    /// HS256 exige chave de pelo menos 256 bits.
    /// </summary>
    public const int TamanhoMinimoDaChaveEmBytes = 32;

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public static JwtOptions Ler(IConfiguration configuration)
    {
        var options = configuration.GetSection(Secao).Get<JwtOptions>() ?? new JwtOptions();

        if (Encoding.UTF8.GetByteCount(options.Key) < TamanhoMinimoDaChaveEmBytes)
        {
            throw new InvalidOperationException(
                $"'Jwt:Key' precisa ter pelo menos {TamanhoMinimoDaChaveEmBytes} bytes. " +
                "Em desenvolvimento ela fica no appsettings.Development.json; em produção, na variável de ambiente Jwt__Key.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("'Jwt:Issuer' e 'Jwt:Audience' precisam estar configurados.");
        }

        return options;
    }
}
