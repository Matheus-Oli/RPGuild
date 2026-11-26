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

    public async Task<DadosDeRedefinicao?> GerarTokenDeRedefinicaoAsync(string email, CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByEmailAsync(email);
        if (usuario is null)
        {
            return null;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        return new DadosDeRedefinicao(usuario.Nome, usuario.Email!, new TokenDeRedefinicao(usuario.Id, token));
    }

    public async Task<bool> RedefinirSenhaAsync(TokenDeRedefinicao token, string novaSenha, CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByIdAsync(token.UserId.ToString());
        if (usuario is null)
        {
            return false;
        }

        // O reset troca o security stamp: o mesmo token (e qualquer outro emitido antes) deixa de valer.
        var resultado = await userManager.ResetPasswordAsync(usuario, token.TokenIdentity, novaSenha);
        return resultado.Succeeded;
    }

    public async Task<Resultado<PerfilAtualizado>> AtualizarPerfilAsync(
        Guid userId, string nome, Nick nick, string? avatarPath, CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByIdAsync(userId.ToString());
        if (usuario is null)
        {
            return ErrosDeConta.SessaoInvalida;
        }

        if (await userManager.Users.AnyAsync(u => u.Id != userId && u.NickNormalizado == nick.Normalizado, cancellationToken))
        {
            return ErrosDeConta.NickEmUso;
        }

        var anterior = usuario.AvatarPath;
        usuario.Nome = nome;
        usuario.Nick = nick.Valor;
        usuario.NickNormalizado = nick.Normalizado;
        usuario.AvatarPath = avatarPath;

        IdentityResult resultado;
        try
        {
            resultado = await userManager.UpdateAsync(usuario);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceNick })
        {
            // Outro usuário pegou o nick entre a consulta e o update.
            return ErrosDeConta.NickEmUso;
        }

        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Code)));
        }

        var perfil = new PerfilResponse(usuario.Id, usuario.Nome, usuario.Nick, usuario.Email!, usuario.AvatarPath);
        return new PerfilAtualizado(perfil, anterior);
    }

    public Task<PerfilResponse?> BuscarPerfilAsync(Guid userId, CancellationToken cancellationToken) =>
        userManager.Users
            .Where(u => u.Id == userId)
            .Select(u => new PerfilResponse(u.Id, u.Nome, u.Nick, u.Email!, u.AvatarPath))
            .SingleOrDefaultAsync(cancellationToken);
}
