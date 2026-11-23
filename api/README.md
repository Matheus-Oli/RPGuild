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
    RPGuild.Tests/             Testes (xUnit) de Application e Domain
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
| Tests | Application, Domain |

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
