using System.Text.Json;
namespace RPGuild.IntegrationTests.Infra;

/// <summary>
/// As respostas com outras pessoas (Taberna, campanhas) nunca expõem e-mail: nem propriedade "email" em nível nenhum, nem o endereço em algum valor.
/// </summary>
public static class RespostaSemEmail
{
    public static void Conferir(string json, params UsuarioLogado[] envolvidos)
    {
        var propriedades = new List<string>();
        Coletar(JsonDocument.Parse(json).RootElement, propriedades);
        propriedades.ShouldNotContain(nome => nome.Contains("email", StringComparison.OrdinalIgnoreCase));

        // Nenhum valor tem "@": nomes, nicks, ids, datas e URLs não têm; só e-mail teria.
        json.ShouldNotContain("@");
        foreach (var usuario in envolvidos)
        {
            json.ShouldNotContain(usuario.Dados.Email, Case.Insensitive);
        }
    }

    private static void Coletar(JsonElement elemento, List<string> propriedades)
    {
        switch (elemento.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var propriedade in elemento.EnumerateObject())
                {
                    propriedades.Add(propriedade.Name);
                    Coletar(propriedade.Value, propriedades);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in elemento.EnumerateArray())
                {
                    Coletar(item, propriedades);
                }

                break;
        }
    }
}
