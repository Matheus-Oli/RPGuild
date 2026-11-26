using Microsoft.AspNetCore.Identity;
using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Domain.Contas;
using RPGuild.Infrastructure.Identity;

namespace RPGuild.Infrastructure.Contas;

public sealed class IdentityService(UserManager<ApplicationUser> userManager, TimeProvider relogio) : IIdentityService
{
    public async Task<Resultado<UsuarioResponse>> CriarUsuarioAsync(
        string nome, Nick nick, string email, string senha, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var usuario = new ApplicationUser
        {
            Id = Guid.CreateVersion7(agora),
            Nome = nome,
            Nick = nick.Valor,
            NickNormalizado = nick.Normalizado,
            Email = email,
            UserName = email,
            CreatedAt = agora,
        };

        var resultado = await userManager.CreateAsync(usuario, senha);
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Code)));
        }

        return new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Nick, email);
    }
}
