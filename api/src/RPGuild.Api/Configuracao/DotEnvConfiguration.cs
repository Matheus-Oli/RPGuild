using DotNetEnv;
using DotNetEnv.Configuration;

namespace RPGuild.Api.Configuracao;

public static class DotEnvConfiguration
{
    /// <summary>
    /// Carrega o .env (procurando a partir da pasta atual até a raiz) como fonte de configuração.
    /// </summary>
    public static void AdicionarDotEnv(this IConfigurationBuilder configuration, string arquivo = ".env") =>
        configuration.AddDotNetEnv(arquivo, LoadOptions.TraversePath());
}
