using Microsoft.Extensions.Configuration;

namespace RPGuild.Infrastructure.Notificacoes;

public sealed class EmailOptions
{
    public const string Secao = "Email";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public bool UseStartTls { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;

    public string RemetenteNome { get; set; } = string.Empty;

    public string RemetenteEmail { get; set; } = string.Empty;

    public static EmailOptions Ler(IConfiguration configuration)
    {
        var options = configuration.GetSection(Secao).Get<EmailOptions>() ?? new EmailOptions();

        var faltando = new[]
            {
                (Chave: nameof(Host), Valor: options.Host),
                (Chave: nameof(Usuario), Valor: options.Usuario),
                (Chave: nameof(Senha), Valor: options.Senha),
                (Chave: nameof(RemetenteEmail), Valor: options.RemetenteEmail),
            }
            .Where(c => string.IsNullOrWhiteSpace(c.Valor))
            .Select(c => $"{Secao}:{c.Chave}")
            .ToList();

        if (options.Port <= 0)
        {
            faltando.Add($"{Secao}:{nameof(Port)}");
        }

        if (faltando.Count > 0)
        {
            throw new InvalidOperationException(
                $"Configuração de e-mail incompleta: {string.Join(", ", faltando)}. " +
                "Em desenvolvimento as credenciais ficam no .env da raiz (Email__Usuario, Email__Senha, Email__RemetenteEmail); " +
                "em produção, nas variáveis de ambiente com os mesmos nomes.");
        }

        return options;
    }
}
