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
