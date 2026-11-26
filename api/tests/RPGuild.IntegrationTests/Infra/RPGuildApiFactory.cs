using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RPGuild.IntegrationTests.Infra;

public sealed class RPGuildApiFactory(string connectionString, TimeProvider? relogio = null) : WebApplicationFactory<Program>
{
    public const string JwtKey = "testes-rpguild-chave-hs256-com-pelo-menos-32-bytes-0123456789";

    public RPGuildApiFactory(PostgresFixture postgres, TimeProvider? relogio = null)
        : this(postgres.ConnectionString, relogio)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", "rpguild-api");
        builder.UseSetting("Jwt:Audience", "rpguild-web");
        builder.UseSetting("Cors:Origins:0", "http://localhost:5173");

        if (relogio is not null)
        {
            builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton(relogio)));
        }
    }

    /// <summary>
    /// Cliente em https, sem cookie automático: os testes mandam o cookie de refresh explicitamente.
    /// </summary>
    public HttpClient CriarCliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    public async Task<UsuarioLogado> CriarUsuarioELogarAsync()
    {
        var usuario = NovoUsuario.Gerar();
        using var cliente = CriarCliente();

        var registro = await cliente.PostAsJsonAsync("/api/auth/register", usuario);
        registro.EnsureSuccessStatusCode();

        var login = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario.Email, usuario.Senha });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;

        return new UsuarioLogado(usuario, token.AccessToken, RefreshCookie.Extrair(login)!);
    }
}
