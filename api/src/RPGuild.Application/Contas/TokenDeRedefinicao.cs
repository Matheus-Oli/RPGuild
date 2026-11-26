namespace RPGuild.Application.Contas;

/// <summary>
/// Token de redefinição de senha que vai no link: um texto opaco com o id do usuário e o token do Identity.
/// </summary>
public sealed record TokenDeRedefinicao(Guid UserId, string TokenIdentity)
{
    public string Codificar() => throw new NotImplementedException();

    /// <summary>
    /// Devolve null se o texto não for um token válido.
    /// </summary>
    public static TokenDeRedefinicao? Decodificar(string? texto) => throw new NotImplementedException();
}
