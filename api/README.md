# api

Back-end ASP.NET Core (.NET 10) do RPGuild: monólito modular em camadas.

## Estrutura

```
api/
  RPGuild.sln
  Directory.Build.props        Configuração comum (net10.0, Nullable, ImplicitUsings, warnings como erro)
  global.json                  Versão do SDK (10.0.x)
  src/
    RPGuild.Api/               Web API: controllers, configuração e composição (DI)
    RPGuild.Application/       Casos de uso, DTOs e interfaces (portas) para a infraestrutura
    RPGuild.Domain/            Entidades, value objects e regras de negócio
    RPGuild.Infrastructure/    Implementações das interfaces: banco, e-mail, storage, jobs
  tests/
    RPGuild.UnitTests/         Testes unitários (xUnit) de Domain e Application + testes de arquitetura
    RPGuild.IntegrationTests/  Testes de integração: API via WebApplicationFactory + Postgres (Testcontainers)
```

Application e Domain são organizados por módulo: `Contas`, `Taberna`, `Campanhas`, `Fichas`, `Rolagens`, `Sessoes`, `Documentos`, `Notificacoes` e `Conteudo`.

## Regra de dependência

As dependências apontam para dentro, em direção ao Domain:

```
Api ──────────────► Application ──► Domain
 │                      ▲             ▲
 └──► Infrastructure ───┴─────────────┘
```

| Projeto | Referencia |
| --- | --- |
| Api | Application, Infrastructure |
| Application | Domain |
| Infrastructure | Application, Domain |
| Domain | nenhum projeto |
| UnitTests | Domain, Application, Infrastructure (só para os testes de arquitetura) |
| IntegrationTests | Api, Infrastructure |

- O Domain não conhece nenhuma outra camada nem frameworks.
- A Application define as interfaces; a Infrastructure as implementa.
- A Api só compõe as camadas e expõe os endpoints.

## Como rodar

Pré-requisito: SDK do .NET 10 (`dotnet --list-sdks`).

```bash
cd api
dotnet build
dotnet test
dotnet run --project src/RPGuild.Api
```

Os warnings são tratados como erro, então o build só passa limpo.

## Banco local

PostgreSQL 17 pelo `docker-compose.yml` da raiz do monorepo (precisa do Docker rodando). Os comandos abaixo rodam da raiz.

```bash
cp .env.example .env          # Windows: copy .env.example .env
docker compose up -d          # sobe o serviço db; espere ficar "healthy" em docker compose ps
dotnet tool restore           # instala o dotnet-ef do manifesto .config/dotnet-tools.json
dotnet ef database update --project api/src/RPGuild.Infrastructure --startup-project api/src/RPGuild.Api
```

- A connection string de desenvolvimento fica em `appsettings.Development.json` (`ConnectionStrings:Default`). Em produção ela vem da variável de ambiente `ConnectionStrings__Default`; sem ela, a API não sobe.
- As migrations **não** são aplicadas no startup da API: rode o `database update` sempre que entrar migration nova.
- O schema usa snake_case (`EFCore.NamingConventions`) e as migrations ficam em `src/RPGuild.Infrastructure/Persistence/Migrations`.
- `GET /health` responde `Healthy` (200) com o banco no ar e `Unhealthy` (503) sem ele.

Criar uma migration nova (depois de mudar o modelo):

```bash
dotnet ef migrations add <NomeDaMigration> --project api/src/RPGuild.Infrastructure --startup-project api/src/RPGuild.Api --output-dir Persistence/Migrations
```

O teste `Modelo_SemMudancasPendentes_NaoPrecisaDeNovaMigration` falha se o modelo mudar sem migration.

Resetar o banco (apaga todos os dados do volume `rpguild_pgdata`):

```bash
docker compose down -v
docker compose up -d
dotnet ef database update --project api/src/RPGuild.Infrastructure --startup-project api/src/RPGuild.Api
```

## Testes

- `RPGuild.UnitTests`: regras de Domain e Application, organizadas por módulo (`Contas/`, `Fichas/`, ...). `Arquitetura/` verifica a regra de dependência entre camadas (NetArchTest).
- `RPGuild.IntegrationTests`: endpoints, banco e EF Core. `Infra/` tem a `PostgresFixture` (container `postgres:17-alpine`, um por execução, via coleção `Integration`, com as migrations já aplicadas) e a `RPGuildApiFactory` (ambiente `Testing`, `ConnectionStrings:Default` apontando para o container).
- Bibliotecas: xUnit, Shouldly, NSubstitute, NetArchTest, Testcontainers.

```bash
dotnet test RPGuild.sln                                       # tudo (precisa do Docker rodando)
dotnet test RPGuild.sln --filter FullyQualifiedName~UnitTests  # só os unitários, sem Docker
```

Convenções de TDD em [CONTRIBUTING.md](../CONTRIBUTING.md#tdd).
