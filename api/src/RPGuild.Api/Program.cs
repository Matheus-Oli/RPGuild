using Hangfire;
using Microsoft.OpenApi;
using RPGuild.Api.Configuracao;
using RPGuild.Api.Http;
using RPGuild.Application;
using RPGuild.Infrastructure;
using RPGuild.Infrastructure.Notificacoes;

const string FrontCorsPolicy = "Front";

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Credenciais de dev (ex.: Email__Senha) no .env da raiz do monorepo, fora do Git. Em produção vêm do ambiente.
    builder.Configuration.AdicionarDotEnv();
}

builder.Services.AddControllers(options =>
{
    // Obrigatoriedade dos campos fica a cargo do FluentValidation, não do nullable do C#.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<ValidacaoFilter>();
});
builder.Services.AddProblemDetails();
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
app.MapHealthChecks("/health");

// Painel do Hangfire só em Development (e, pelo padrão do Hangfire, só para requisições locais).
if (hangfireHabilitado && app.Environment.IsDevelopment())
{
    app.MapHangfireDashboard("/hangfire");
}

app.Run();

public partial class Program;
