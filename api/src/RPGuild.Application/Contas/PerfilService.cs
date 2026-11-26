using Microsoft.Extensions.Logging;
using RPGuild.Application.Comum;
using RPGuild.Application.Uploads;
using RPGuild.Domain.Contas;

namespace RPGuild.Application.Contas;

public sealed class PerfilService(IIdentityService identity, IStorageService storage, ILogger<PerfilService> logger) : IPerfilService
{
    public async Task<Resultado<PerfilResponse>> AtualizarAsync(
        Guid userId, AtualizarPerfilRequest request, CancellationToken cancellationToken)
    {
        if (request.AvatarPath is not null && !CaminhoDeAvatar.PertenceAo(userId, request.AvatarPath))
        {
            return ErrosDeConta.AvatarForaDaPasta;
        }

        var atualizado = await identity.AtualizarPerfilAsync(
            userId, request.Nome.Trim(), Nick.Criar(request.Nick), request.AvatarPath, cancellationToken);
        if (!atualizado.Sucesso)
        {
            return atualizado.Erro!;
        }

        var (perfil, anterior) = atualizado.Valor!;
        if (anterior is not null && anterior != perfil.AvatarPath)
        {
            await ApagarAvatarAntigoAsync(anterior, cancellationToken);
        }

        return perfil;
    }

    private async Task ApagarAvatarAntigoAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await storage.ApagarAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            // O perfil já foi salvo: um arquivo órfão no bucket não justifica falhar a requisição.
            logger.LogWarning(ex, "Falha ao apagar o avatar antigo {Path} do storage", path);
        }
    }
}
