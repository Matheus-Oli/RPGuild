namespace RPGuild.IntegrationTests.Infra;

public sealed record NovoUsuario(string Nome, string Nick, string Email, string Senha)
{
    public const string SenhaPadrao = "senha1234";

    /// <summary>
    /// Dados únicos por chamada: o banco do container é compartilhado entre os testes.
    /// </summary>
    public static NovoUsuario Gerar()
    {
        var nick = $"u_{Guid.NewGuid():N}"[..14];
        return new NovoUsuario("Usuário de Teste", nick, $"{nick}@teste.rpguild", SenhaPadrao);
    }
}

public sealed record UsuarioLogado(NovoUsuario Dados, string AccessToken, string RefreshCookie)
{
    public Guid Id => ClaimsDoToken.UserId(AccessToken);

    /// <summary>
    /// Path de avatar novo na pasta do usuário, como o que o POST /api/uploads/sign gera.
    /// </summary>
    public string NovoAvatar() => $"avatars/{Id}/{Guid.NewGuid()}.png";
}

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record UsuarioResponse(Guid Id, string Nome, string Nick, string Email);

public sealed record ProblemaResponse(int Status, string Title, string? Detail);

public sealed record PerfilResponse(Guid Id, string Nome, string Nick, string Email, string? AvatarPath, string? AvatarUrl);
