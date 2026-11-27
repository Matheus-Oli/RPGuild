using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi;
using RPGuild.Api.Configuracao;
using RPGuild.Api.Http;
using RPGuild.Api.TempoReal;
using RPGuild.Application;
using RPGuild.Application.Campanhas;
using RPGuild.Application.Notificacoes;
using RPGuild.Infrastructure;
using RPGuild.Infrastructure.Notificacoes;

const string FrontCorsPolicy = "Front";

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Credenciais de dev (ex.: Email__Senha) no .env da raiz do monorepo, fora do Git. Em produção vêm do ambiente.
    builder.Configuration.AdicionarDotEnv();
}

// O log de início e fim de requisição imprime a URL com a query, e nos hubs a query leva o access token.
// Não dá para mascarar só esse trecho, então ele fica travado em Warning aqui, mesmo que a configuração suba
// "Microsoft.AspNetCore" para Information.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

builder.Services.AddControllers(options =>
{
    // Obrigatoriedade dos campos fica a cargo do FluentValidation, não do nullable do C#.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<ValidacaoFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, UsuarioPeloSub>();
builder.Services.AddSingleton<INotificacoesTempoReal, HubNotificacoesTempoReal>();
builder.Services.AddSingleton<ICampanhaTempoReal, HubCampanhaTempoReal>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Fila de e-mail e jobs. Desligado no ambiente Testing (os testes usam uma fila falsa), salvo "Hangfire:Habilitado".
var hangfireHabilitado = FilaHangfire.EstaHabilitado(builder.Configuration, builder.Environment.IsEnvironment("Testing"));
if (hangfireHabilitado)
{
    builder.Services.AddFilaHangfire(builder.Configuration);
}

builder.Services.AddCors(options => options.AddPolicy(FrontCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token do POST /api/auth/login.",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.Rota);
app.MapHub<CampaignHub>(CampaignHub.Rota);
app.MapHealthChecks("/health");

if (hangfireHabilitado)
{
    app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<LimpezaDeNotificacoes>(
        LimpezaDeNotificacoes.IdDoJob, job => job.ExecutarAsync(CancellationToken.None), Cron.Daily());

    // Painel do Hangfire só em Development (e, pelo padrão do Hangfire, só para requisições locais).
    if (app.Environment.IsDevelopment())
    {
        app.MapHangfireDashboard("/hangfire");
    }
}

app.Run();

public partial class Program;
