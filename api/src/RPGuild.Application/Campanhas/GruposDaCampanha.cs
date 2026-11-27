namespace RPGuild.Application.Campanhas;

/// <summary>
/// Os grupos do hub de campanha: todos os que estão com a campanha aberta, e só o mestre (o que é só dele, como os
/// convites).
/// </summary>
public static class GruposDaCampanha
{
    public static string Todos(Guid campanhaId) => $"campaign:{campanhaId}";

    public static string Mestre(Guid campanhaId) => $"campaign:{campanhaId}:mestre";
}
