using Microsoft.Extensions.Configuration;
using RPGuild.Api.Configuracao;
using RPGuild.IntegrationTests.Infra;

namespace RPGuild.IntegrationTests.Configuracao;

// Na coleção de integração para não rodar em paralelo com as factories: a variável de ambiente é do processo.
[Collection(IntegrationCollection.Name)]
public sealed class DotEnvConfigurationTests : IDisposable
{
    private const string Variavel = "Email__Usuario";

    private readonly string _pasta = Directory.CreateTempSubdirectory("rpguild-dotenv-").FullName;
    private readonly string? _valorOriginal = Environment.GetEnvironmentVariable(Variavel);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Variavel, _valorOriginal);
        Directory.Delete(_pasta, recursive: true);
    }

    [Fact]
    public void AdicionarDotEnv_ChaveNoAmbienteENoArquivo_ValeADoAmbiente()
    {
        // Arrange
        var arquivo = Path.Combine(_pasta, ".env");
        File.WriteAllText(arquivo, "Email__Usuario=do-arquivo@teste.rpguild\nEmail__Senha=so-no-arquivo\n");
        Environment.SetEnvironmentVariable(Variavel, "do-ambiente@teste.rpguild");

        var configuration = new ConfigurationManager();
        configuration.AddEnvironmentVariables();

        // Act
        configuration.AdicionarDotEnv(arquivo);

        // Assert
        configuration["Email:Usuario"].ShouldBe("do-ambiente@teste.rpguild");
        configuration["Email:Senha"].ShouldBe("so-no-arquivo");
        Environment.GetEnvironmentVariable(Variavel).ShouldBe("do-ambiente@teste.rpguild");
    }
}
