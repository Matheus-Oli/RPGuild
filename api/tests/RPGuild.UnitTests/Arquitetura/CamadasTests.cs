using System.Reflection;
using NetArchTest.Rules;

namespace RPGuild.UnitTests.Arquitetura;

public class CamadasTests
{
    private const string Domain = "RPGuild.Domain";
    private const string Application = "RPGuild.Application";
    private const string Infrastructure = "RPGuild.Infrastructure";
    private const string Api = "RPGuild.Api";
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";

    [Theory]
    [InlineData(Application)]
    [InlineData(Infrastructure)]
    [InlineData(Api)]
    [InlineData(EntityFrameworkCore)]
    public void Domain_DependenciaProibida_NaoDepende(string dependencia)
    {
        DeveSerIndependente(Domain, dependencia);
    }

    [Theory]
    [InlineData(Infrastructure)]
    [InlineData(Api)]
    public void Application_DependenciaProibida_NaoDepende(string dependencia)
    {
        DeveSerIndependente(Application, dependencia);
    }

    [Fact]
    public void Infrastructure_Api_NaoDepende()
    {
        DeveSerIndependente(Infrastructure, Api);
    }

    private static void DeveSerIndependente(string camada, string dependencia)
    {
        // Arrange
        var assembly = Assembly.Load(camada);

        // Act
        var resultado = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(dependencia)
            .GetResult();

        // Assert
        resultado.IsSuccessful.ShouldBeTrue(
            $"{camada} não pode depender de {dependencia}: {string.Join(", ", resultado.FailingTypeNames ?? [])}");
    }
}
