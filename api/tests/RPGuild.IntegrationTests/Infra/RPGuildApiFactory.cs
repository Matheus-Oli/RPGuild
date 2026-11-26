using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RPGuild.IntegrationTests.Infra;

public sealed class RPGuildApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public RPGuildApiFactory(PostgresFixture postgres)
        : this(postgres.ConnectionString)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
    }
}
