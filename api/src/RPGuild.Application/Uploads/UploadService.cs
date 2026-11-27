namespace RPGuild.Application.Uploads;

public sealed record UrlDeUploadResponse(string UploadUrl, string Path, string PublicUrl);

/// <summary>
/// Gera a URL assinada para o front subir a imagem direto no storage. A API não recebe o arquivo;
/// o limite de tamanho e os tipos também ficam configurados no bucket.
/// </summary>
public sealed class UploadService(IStorageService storage)
{
    /// <summary>
    /// O request já chega validado (tipo suportado e content type permitido).
    /// </summary>
    public async Task<UrlDeUploadResponse> AssinarAsync(Guid userId, AssinarUploadRequest request, CancellationToken cancellationToken)
    {
        var path = TiposDeUpload.GerarPath(request.Tipo, userId, request.ContentType);
        var uploadUrl = await storage.CriarUrlDeUploadAsync(path, cancellationToken);

        return new UrlDeUploadResponse(uploadUrl, path, storage.ObterUrlPublica(path));
    }
}
