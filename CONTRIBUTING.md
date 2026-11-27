# Contribuindo

## Branches (Git Flow)

| Branch | Para quê | Sai de | Volta para |
| --- | --- | --- | --- |
| `main` | Produção. Cada merge aqui é um deploy. | — | — |
| `develop` | Integração do que está pronto para a próxima versão. | `main` | `main` (via release) |
| `feature/<card>-<descricao>` | Uma funcionalidade ou card do Trello. | `develop` | `develop` |
| `release/<versao>` | Ajustes finais antes de publicar. | `develop` | `main` e `develop` |
| `hotfix/<descricao>` | Correção urgente em produção. | `main` | `main` e `develop` |

Exemplos: `feature/s1-auth-jwt`, `feature/s5-criacao-ficha`, `release/1.0.0`, `hotfix/login-refresh`.

## Commits (Conventional Commits)

```
<tipo>(<escopo>): <descrição no imperativo>
```

- Tipos: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `ci`, `build`, `perf`.
- Escopos: `web`, `api`, `docs`, `infra`, ou o módulo (`auth`, `campanhas`, `fichas`, `rolagens`, `agenda`, `documentos`, `taberna`, `notificacoes`).
- Exemplos: `feat(api): adiciona endpoint de rolagem`, `fix(web): corrige alinhamento das perícias na ficha`.

## Pull requests

- Todo código entra por PR, nunca direto em `main` ou `develop`.
- Título no padrão dos commits; descrição pelo template, com o link do card do Trello.
- O PR só entra com o CI verde (build e testes).
- Prefira squash merge em `feature/*` → `develop`, e merge commit em `release/*` e `hotfix/*`.
- Ao fazer merge, mova o card no Trello para **Feito**.

## Versionamento

SemVer (`MAJOR.MINOR.PATCH`), com tag em `main` a cada release (`v1.0.0`).

## TDD

- Ciclo red → green → refactor. Nenhum código de produção sem um teste falhando antes.
- Regra de domínio/aplicação → `RPGuild.UnitTests`. Endpoint, banco, EF Core → `RPGuild.IntegrationTests` (Postgres real via Testcontainers; precisa do Docker rodando).
- Nome dos testes: `Metodo_Cenario_ResultadoEsperado` (ex.: `Rolar_JogadorComVisibilidadeOculta_LancaExcecao`). Classe: `<ClasseTestada>Tests`.
- Estrutura Arrange / Act / Assert, com Shouldly nas asserções e NSubstitute para mocks.
- Commits do ciclo: `test(<modulo>): ...` no red, `feat(<modulo>): ...` no green, `refactor(<modulo>): ...` no refactor.
- Durante o ciclo red/green, rode só os testes afetados:
  - `dotnet test api/RPGuild.sln --filter "FullyQualifiedName~<ClasseDeTeste>"`
  - Unitários: `dotnet test api/tests/RPGuild.UnitTests`, que roda sem Docker.
- A suíte inteira (`dotnet test api/RPGuild.sln`) roda ao fim de cada ciclo e antes do push.
- Se um teste passar de primeira, valide com mutação: quebre de propósito a regra que ele protege e confira que ele falha.
  - Antes de mutar, garanta que a implementação do green já está commitada ou em stash.
  - Desfaça a mutação editando o arquivo de volta à mão, ou com `git stash` / `git stash pop`.
  - Nunca use `git checkout -- <arquivo>`, `git restore` ou `git reset --hard` com trabalho não commitado: eles apagam a implementação junto com a mutação.
- O container de testes (`rpguild-tests-db`) é reaproveitado em dev: ele fica rodando entre as execuções, numa porta aleatória, e os dados são limpos (Respawn) ao subir a suíte. Não precisa de flag do Testcontainers. Em CI (`CI=true`) o container é sempre novo e removido no fim. Para recriar: `docker rm -f rpguild-tests-db`.

## PRs por módulo

- Uma feature branch por módulo do back (`feature/<modulo>`).
- Vários commits e pushes durante o módulo.
- PR para `develop` só quando o módulo fecha, com todos os testes verdes.

## Como rodar os testes

```bash
dotnet test api/RPGuild.sln                                       # tudo (precisa do Docker)
dotnet test api/RPGuild.sln --filter FullyQualifiedName~UnitTests  # só os unitários, sem Docker
```
