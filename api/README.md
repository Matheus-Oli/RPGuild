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

### Testes

```bash
dotnet test RPGuild.sln                                              # suíte inteira (precisa do Docker)
dotnet test RPGuild.sln --filter "FullyQualifiedName~<ClasseDeTeste>"  # só uma classe, no ciclo de TDD
dotnet test tests/RPGuild.UnitTests                                  # só os unitários, sem Docker
```

- Os testes de integração usam o container `rpguild-tests-db` (Testcontainers), reaproveitado entre execuções em dev, numa porta aleatória que não conflita com o Postgres do compose na 5432. Ao subir, a suíte aplica as migrations pendentes e limpa os dados com Respawn.
- Com `CI=true` o container é sempre novo e removido no fim.
- Para recriar o container: `docker rm -f rpguild-tests-db`.

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

## Autenticação

ASP.NET Identity (sem roles) para usuários e senhas, JWT para o access token e refresh token próprio, guardado no banco. Swagger em `/swagger` (só em Development), com o botão **Authorize** para colar o access token.

| Endpoint | Entrada | Saída |
| --- | --- | --- |
| `POST /api/auth/register` | `{nome, nick, email, senha}` | 201 `{id, nome, nick, email}` |
| `POST /api/auth/login` | `{email, senha}` | 200 `{accessToken, expiresAt}` + cookie `rpguild_refresh` |
| `POST /api/auth/refresh` | cookie | 200 `{accessToken, expiresAt}` + cookie novo |
| `POST /api/auth/logout` | cookie | 204, revoga o refresh e apaga o cookie |
| `GET /api/me` | `Authorization: Bearer <token>` | 200 `{id, nome, nick, email, avatarPath}` |

Erros em ProblemDetails: 400 validação (`errors` por campo, em camelCase) · 401 credencial ou sessão inválida (mensagem genérica, sem dizer se foi o e-mail ou a senha) · 409 e-mail ou nick em uso · 423 conta bloqueada.

**Fluxo**

1. **Access token** (JWT HS256, 15 min, claims `sub`, `email`, `nick`, `name`): vai no corpo do login/refresh e o front manda no header `Authorization: Bearer`. Validade conferida sem tolerância de relógio.
2. **Refresh token** (64 bytes aleatórios, 14 dias): vai **só** no cookie `rpguild_refresh` (`HttpOnly`, `Secure`, `SameSite=None`, `Path=/api/auth`), nunca no corpo. No banco (`refresh_tokens`) fica só o hash SHA-256.
3. **Rotação:** cada `refresh` revoga o token usado (`revoked_at`, `replaced_by_id`) e emite outro na mesma família (`family_id`). Cada login começa uma família nova.
4. **Reuso:** se chegar um refresh já revogado, alguém pode ter copiado o cookie. A família inteira é revogada e a resposta é 401: os dois lados precisam entrar de novo.
5. **Lockout:** 5 senhas erradas bloqueiam a conta por 15 minutos (423).
6. **Regras de conta:** nick com 3 a 20 caracteres (`A-Z`, `a-z`, `0-9`, `_`), único sem diferenciar caixa (`nick_normalizado`); e-mail único; senha com 8+ caracteres, uma letra e um número.

No front, as chamadas a `/api/auth/*` precisam de `credentials: "include"` para o navegador mandar e receber o cookie.

**Configuração**

| Chave | Para quê | Dev (`appsettings.Development.json`) | Produção (variável de ambiente) |
| --- | --- | --- | --- |
| `Jwt:Key` | Chave HS256, mínimo 32 bytes | chave fixa de dev | `Jwt__Key` (obrigatória; sem ela a API não sobe) |
| `Jwt:Issuer` | Emissor do token | `rpguild-api` | `Jwt__Issuer` |
| `Jwt:Audience` | Público do token | `rpguild-web` | `Jwt__Audience` |
| `Cors:Origins` | Origens do front (com `AllowCredentials`) | `http://localhost:5173` | `Cors__Origins__0`, `Cors__Origins__1`... |

Nos testes, a `RPGuildApiFactory` define uma chave própria e aceita um `TimeProvider` (por exemplo `FakeTimeProvider`) para controlar o relógio. O helper `CriarUsuarioELogarAsync()` devolve o access token e o cookie de refresh.

## Testes

- `RPGuild.UnitTests`: regras de Domain e Application, organizadas por módulo (`Contas/`, `Fichas/`, ...). `Arquitetura/` verifica a regra de dependência entre camadas (NetArchTest).
- `RPGuild.IntegrationTests`: endpoints, banco e EF Core. `Infra/` tem a `PostgresFixture` (container `postgres:17-alpine`, um por execução, via coleção `Integration`, com as migrations já aplicadas) e a `RPGuildApiFactory` (ambiente `Testing`, `ConnectionStrings:Default` apontando para o container).
- Bibliotecas: xUnit, Shouldly, NSubstitute, NetArchTest, Testcontainers.

```bash
dotnet test RPGuild.sln                                       # tudo (precisa do Docker rodando)
dotnet test RPGuild.sln --filter FullyQualifiedName~UnitTests  # só os unitários, sem Docker
```

Convenções de TDD em [CONTRIBUTING.md](../CONTRIBUTING.md#tdd).
