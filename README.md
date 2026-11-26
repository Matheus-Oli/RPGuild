# RPGuild

Site fechado para o grupo de RPG de mesa: agenda de sessões, campanhas, fichas (D&D 2024 na v1), rolagens em tempo real e documentos, tudo num lugar só.

## Estrutura

```
/web    Front-end React + Vite + TypeScript (Tailwind v4, shadcn/ui, TipTap, dice-box, FullCalendar)
/api    Back-end ASP.NET Core (.NET 10), PostgreSQL, SignalR, Hangfire
/docs   Documento do projeto, design system e diagramas
```

## Stack

| Camada | Tecnologia |
| --- | --- |
| Front | React + Vite + TypeScript, Tailwind v4 + shadcn/ui, React Bits, TipTap, FullCalendar, dice-box-threejs, motor de regras D&D em TypeScript |
| Back | ASP.NET Core Web API (.NET 10), EF Core, ASP.NET Identity (JWT + refresh token) |
| Banco | PostgreSQL |
| Tempo real | SignalR |
| Jobs | Hangfire |
| E-mail | Gmail SMTP (MailKit) |
| Imagens | Supabase Storage |
| Hospedagem | Cloudflare Pages (front) + Oracle Cloud Always Free (API e banco, Docker Compose + Caddy) |
| CI/CD | GitHub Actions |

## Documentação

- [Estado atual](docs/estado-atual.md): decisões recentes e ordem de execução. Onde divergir do Documento do projeto, vale este.
- [Documento do projeto](docs/documento-do-projeto.md): visão, requisitos, regras de negócio, telas, modelo de dados, arquitetura e endpoints.
- [Design system](docs/design-system/README.md): cores, tipografia, logo e uso com Tailwind/shadcn.
- Backlog: quadro RPGuild no Trello.

## Como rodar

Em construção (sprint S0). Esta seção vai trazer os comandos de `web` e `api` e o `docker compose` do banco local.

## Contribuindo

Fluxo de branches, commits e PRs em [CONTRIBUTING.md](CONTRIBUTING.md).

## Licenças de conteúdo

O conteúdo de regras de D&D vem do SRD 5.2 e 5.1, da Wizards of the Coast, sob CC-BY-4.0.
