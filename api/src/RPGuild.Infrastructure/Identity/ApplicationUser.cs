using Microsoft.AspNetCore.Identity;

namespace RPGuild.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Nome { get; set; } = string.Empty;

    public string Nick { get; set; } = string.Empty;

    public string NickNormalizado { get; set; } = string.Empty;

    public string? AvatarPath { get; set; }

    /// <summary>
    /// E-mail novo pedido em PUT /api/me/email, esperando a confirmação no próprio endereço.
    /// </summary>
    public string? EmailPendente { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
