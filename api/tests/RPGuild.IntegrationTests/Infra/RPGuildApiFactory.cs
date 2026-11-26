using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RPGuild.Application.Notificacoes;
using RPGuild.Application.Uploads;

namespace RPGuild.IntegrationTests.Infra;

/// <param name="emailSender">Troca o <see cref="FakeEmailSender"/> (ex.: um sender que falha).</param>
/// <param name="validadeDoTokenDeRedefinicao">
/// Troca a validade do token de redefinição. O provider de token do Identity usa o relógio real, não o <see cref="TimeProvider"/>.
/// </param>
/// <param name="configurarServicos">Ajuste extra nos serviços da API (ex.: um interceptor do EF que simula erro do banco).</param>
public sealed class RPGuildApiFactory(
    string connectionString,
    TimeProvider? relogio = null,
    IEmailSender? emailSender = null,
    TimeSpan? validadeDoTokenDeRedefinicao = null,
    Action<IServiceCollection>? configurarServicos = null)
    : WebApplicationFactory<Program>
{
    public const string FrontBaseUrl = "http://localhost:5173";

    public const string JwtKey = "testes-rpguild-chave-hs256-com-pelo-menos-32-bytes-0123456789";

    /// <summary>
    /// Banco desta factory, para os helpers que preparam dados direto no Postgres.
    /// </summary>
    public string ConnectionString => connectionString;

    public RPGuildApiFactory(
        PostgresFixture postgres,
        TimeProvider? relogio = null,
        IEmailSender? emailSender = null,
        TimeSpan? validadeDoTokenDeRedefinicao = null,
        Action<IServiceCollection>? configurarServicos = null)
        : this(postgres.ConnectionString, relogio, emailSender, validadeDoTokenDeRedefinicao, configurarServicos)
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
        builder.UseSetting("Front:BaseUrl", FrontBaseUrl);
        // Só para a configuração passar na validação: o envio de verdade é trocado pelo FakeEmailSender.
        builder.UseSetting("Email:Usuario", "testes@rpguild.invalid");
        builder.UseSetting("Email:Senha", "nao-usada");
        builder.UseSetting("Email:RemetenteEmail", "testes@rpguild.invalid");
        // Idem para o Supabase: o storage de verdade é trocado pelo FakeStorageService.
        builder.UseSetting("Supabase:Url", "https://supabase.rpguild.invalid");
        builder.UseSetting("Supabase:ServiceKey", "nao-usada");
        builder.UseSetting("Supabase:Bucket", "imagens");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(emailSender ?? Emails);

            services.RemoveAll<IStorageService>();
            services.AddSingleton<IStorageService>(Storage);

            if (relogio is not null)
            {
                services.Replace(ServiceDescriptor.Singleton(relogio));
            }

            if (validadeDoTokenDeRedefinicao is { } validade)
            {
                services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = validade);
            }

            configurarServicos?.Invoke(services);
        });
    }

    /// <summary>
    /// E-mails "enviados" pela API desta factory.
    /// </summary>
    public FakeEmailSender Emails { get; } = new();

    /// <summary>
    /// Arquivos "criados" e "apagados" no storage pela API desta factory.
    /// </summary>
    public FakeStorageService Storage { get; } = new();

    /// <summary>
    /// Cliente em https, sem cookie automático: os testes mandam o cookie de refresh explicitamente.
    /// </summary>
    public HttpClient CriarCliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    public HttpClient CriarClienteAutenticado(string accessToken)
    {
        var cliente = CriarCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return cliente;
    }

    /// <summary>
    /// Cadastra um usuário novo (dados únicos) e devolve os dados enviados e a resposta.
    /// </summary>
    public async Task<(NovoUsuario Dados, UsuarioResponse Criado)> RegistrarUsuarioAsync(NovoUsuario? dados = null)
    {
        dados ??= NovoUsuario.Gerar();
        using var cliente = CriarCliente();

        var registro = await cliente.PostAsJsonAsync("/api/auth/register", dados);
        registro.EnsureSuccessStatusCode();

        return (dados, (await registro.Content.ReadFromJsonAsync<UsuarioResponse>())!);
    }

    public async Task<UsuarioLogado> CriarUsuarioELogarAsync()
    {
        var (usuario, _) = await RegistrarUsuarioAsync();
        return await LogarAsync(usuario);
    }

    /// <summary>
    /// Abre uma sessão nova (outra família de refresh token) para um usuário que já existe.
    /// </summary>
    public async Task<UsuarioLogado> LogarAsync(NovoUsuario usuario)
    {
        using var cliente = CriarCliente();

        var login = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario.Email, usuario.Senha });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;

        return new UsuarioLogado(usuario, token.AccessToken, RefreshCookie.Extrair(login)!);
    }

    /// <summary>
    /// Cadastra um usuário, pede a redefinição de senha e devolve o token opaco que veio no e-mail.
    /// </summary>
    public async Task<(NovoUsuario Usuario, string Token)> PedirRedefinicaoAsync(NovoUsuario? dados = null)
    {
        var (usuario, _) = await RegistrarUsuarioAsync(dados);
        return (usuario, await PedirRedefinicaoAsync(usuario.Email));
    }

    /// <summary>
    /// Pede a redefinição de senha de uma conta que já existe e devolve o token opaco que veio no e-mail.
    /// </summary>
    public async Task<string> PedirRedefinicaoAsync(string email)
    {
        using var cliente = CriarCliente();

        var pedido = await cliente.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        pedido.EnsureSuccessStatusCode();

        return LinkNoEmail.Token(Emails.UltimaPara(email)!, LinkNoEmail.RedefinirSenha);
    }

    /// <summary>
    /// Pede a troca de e-mail do usuário logado e devolve o e-mail novo e o token opaco que chegou nele.
    /// </summary>
    public async Task<(string NovoEmail, string Token)> PedirTrocaDeEmailAsync(UsuarioLogado logado, string? novoEmail = null)
    {
        novoEmail ??= NovoUsuario.Gerar().Email;
        using var cliente = CriarClienteAutenticado(logado.AccessToken);

        var pedido = await cliente.PutAsJsonAsync("/api/me/email", new { novoEmail, senha = logado.Dados.Senha });
        pedido.EnsureSuccessStatusCode();

        return (novoEmail, LinkNoEmail.Token(Emails.UltimaPara(novoEmail)!, LinkNoEmail.ConfirmarEmail));
    }

    /// <summary>
    /// Se o refresh desta sessão ainda é aceito. Rotaciona o cookie: use uma vez por sessão.
    /// </summary>
    public async Task<bool> SessaoContinuaAsync(string refreshCookie)
    {
        using var cliente = CriarCliente();
        using var resposta = await cliente.SendAsync(RefreshCookie.Post("/api/auth/refresh", refreshCookie));
        return resposta.IsSuccessStatusCode;
    }
}
