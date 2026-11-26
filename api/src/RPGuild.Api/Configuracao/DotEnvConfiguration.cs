using DotNetEnv;
using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace RPGuild.Api.Configuracao;

public static class DotEnvConfiguration
{
    /// <summary>
    /// Carrega o .env (procurando a partir da pasta atual até a raiz) como fonte de configuração,
    /// abaixo das variáveis de ambiente: o .env só preenche o que o ambiente não definiu.
    /// </summary>
    public static void AdicionarDotEnv(this IConfigurationBuilder configuration, string arquivo = ".env")
    {
        // Não grava no ambiente do processo nem sobrescreve variáveis que já existem.
        var opcoes = LoadOptions.TraversePath().NoEnvVars().NoClobber();
        var dotEnv = new ConfigurationBuilder().AddDotNetEnv(arquivo, opcoes).Sources.Single();

        // Entra logo antes das variáveis de ambiente da aplicação (as sem prefixo), que continuam valendo mais.
        var ambiente = configuration.Sources
            .Select((fonte, indice) => (Fonte: fonte, Indice: indice))
            .LastOrDefault(f => f.Fonte is EnvironmentVariablesConfigurationSource { Prefix: null or "" });

        if (ambiente.Fonte is null)
        {
            configuration.Sources.Add(dotEnv);
        }
        else
        {
            configuration.Sources.Insert(ambiente.Indice, dotEnv);
        }
    }
}
