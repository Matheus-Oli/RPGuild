namespace RPGuild.Application.Notificacoes;

/// <summary>
/// E-mail mascarado para log, sem expor o endereço completo: "ma***@gmail.com".
/// </summary>
public static class MascaraDeEmail
{
    private const string Mascara = "***";
    private const int CaracteresVisiveis = 2;

    public static string Mascarar(string? email)
    {
        var arroba = email?.LastIndexOf('@') ?? -1;
        if (arroba <= 0)
        {
            return Mascara;
        }

        var local = email![..arroba];
        var dominio = email[arroba..];

        // Com 2 caracteres ou menos, mostrar o começo seria mostrar tudo.
        return local.Length <= CaracteresVisiveis ? $"{Mascara}{dominio}" : $"{local[..CaracteresVisiveis]}{Mascara}{dominio}";
    }
}
