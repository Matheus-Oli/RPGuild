namespace RPGuild.IntegrationTests.Infra;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Integration";
}
