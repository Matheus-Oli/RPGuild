using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RPGuild.Application.Comum;
using RPGuild.Application.Contas;
using RPGuild.Domain.Contas;
using RPGuild.Infrastructure.Identity;

namespace RPGuild.Infrastructure.Contas;

public sealed class IdentityService(UserManager<ApplicationUser> userManager, TimeProvider relogio) : IIdentityService
{
    private const string IndiceNick = "ix_users_nick_normalizado";

    public async Task<Resultado<UsuarioResponse>> CriarUsuarioAsync(
        string nome, Nick nick, string email, string senha, CancellationToken cancellationToken)
    {
        if (await userManager.Users.AnyAsync(u => u.NickNormalizado == nick.Normalizado, cancellationToken))
        {
            return ErrosDeConta.NickEmUso;
        }

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

        IdentityResult resultado;
        try
        {
            resultado = await userManager.CreateAsync(usuario, senha);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceNick })
        {
            // Dois cadastros simultâneos com o mesmo nick: o índice único decide.
            return ErrosDeConta.NickEmUso;
        }

        if (resultado.Errors.Any(e => e.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return ErrosDeConta.EmailEmUso;
        }

        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Code)));
        }

        return new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Nick, email);
    }

    public async Task<Resultado<UsuarioResponse>> VerificarCredenciaisAsync(
        string email, string senha, CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByEmailAsync(email);
        if (usuario is null)
        {
            // Mesma resposta para e-mail inexistente e senha errada: não revela quais e-mails têm conta.
            return ErrosDeConta.CredenciaisInvalidas;
        }

        if (await userManager.IsLockedOutAsync(usuario))
        {
            return ErrosDeConta.ContaBloqueada;
        }

        if (!await userManager.CheckPasswordAsync(usuario, senha))
        {
            await userManager.AccessFailedAsync(usuario);
            return await userManager.IsLockedOutAsync(usuario) ? ErrosDeConta.ContaBloqueada : ErrosDeConta.CredenciaisInvalidas;
        }

        await userManager.ResetAccessFailedCountAsync(usuario);

        return new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Nick, usuario.Email!);
    }

    public Task<PerfilResponse?> BuscarPerfilAsync(Guid userId, CancellationToken cancellationToken) =>
        userManager.Users
            .Where(u => u.Id == userId)
            .Select(u => new PerfilResponse(u.Id, u.Nome, u.Nick, u.Email!, u.AvatarPath))
            .SingleOrDefaultAsync(cancellationToken);
}
