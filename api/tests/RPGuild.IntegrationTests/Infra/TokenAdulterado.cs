using RPGuild.Application.Contas;

namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// Variações inválidas de um token opaco de conta (redefinição de senha, troca de e-mail), para os [Theory].
/// </summary>
public static class TokenAdulterado
{
    public const string Lixo = "lixo";
    public const string TokenIdentityAlterado = "token-identity-alterado";
    public const string OutroUsuario = "outro-usuario";

    public static string Gerar(string token, string caso)
    {
        var original = TokenDeRedefinicao.Decodificar(token)!;
        return caso switch
        {
            Lixo => "nao-e-um-token",
            TokenIdentityAlterado => (original with { TokenIdentity = original.TokenIdentity + "x" }).Codificar(),
            OutroUsuario => (original with { UserId = Guid.NewGuid() }).Codificar(),
            _ => throw new ArgumentOutOfRangeException(nameof(caso), caso, null),
        };
    }
}
