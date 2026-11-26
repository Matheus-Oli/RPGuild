namespace RPGuild.Application.Uploads;

/// <summary>
/// Porta para o armazenamento de arquivos (Supabase Storage, na Infrastructure). O front sobe o arquivo direto
/// pela URL assinada; a API só gera a URL, monta a URL pública e apaga o que não é mais usado.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// URL assinada para o front subir o arquivo neste path sem outra autenticação.
    /// </summary>
    Task<string> CriarUrlDeUploadAsync(string path, CancellationToken ct);

    /// <summary>
    /// URL pública do arquivo (o bucket é público).
    /// </summary>
    string ObterUrlPublica(string path);

    Task ApagarAsync(string path, CancellationToken ct);
}
