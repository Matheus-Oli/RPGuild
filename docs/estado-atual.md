# RPGuild — Estado atual e decisões recentes

Atualizado em 24/11/2026. Este arquivo complementa o "Documento do projeto": onde os dois divergirem, vale este.

## Onde as coisas estão

| O quê | Onde |
| --- | --- |
| Documento do projeto (visão, regras, telas, modelo de dados, arquitetura, endpoints) | `RPGuild - Documento do projeto.md` (cópia online: https://claude.ai/artifact/EXNiR9iQK7saQz8z4SQfa5) |
| Design system (cores, fontes, logo, Tailwind/shadcn) | `design-system/` (online: https://claude.ai/artifact/Cx3CwKHaM7kTE5zS9KoFJb) |
| Repositório | https://github.com/Matheus-Oli/RPGuild (só o commit `v1` com a documentação) |
| Pasta local | `C:\Users\Usuario\Downloads\Projetos\RPG` |
| Backlog | Trello, quadro RPGuild: https://trello.com/b/qBRH7HXp/rpguild (a ser refeito com as fases abaixo) |

## Mudança de plano: ordem de execução

O backlog antigo (S0 a S10, fatiado por camada) foi descartado. A nova ordem é:

1. **Protótipo**: todas as telas no Claude Design, usando o design system da RPGuild. Uma tela (ou grupo) por vez, seguindo o mapa de telas do documento.
2. **Back-end e banco**: um módulo por vez (setup; contas; taberna; notificações; campanhas e convites; conteúdo D&D; fichas; rolagens; sessões e lembretes; documentos; saída de player). Cada módulo com entidades, migration, endpoints, regras e testes. Antes de começar, revisar os endpoints contra o protótipo.
3. **Front-end + integração**: uma tela do protótipo por vez, já ligada na API.
4. **Deploy e polimento**: VM Oracle, DuckDNS + Caddy, CI/CD, mobile, backup.

## Troca do front: Angular → React (24/11/2026)

- Motivo: usar bibliotecas de componentes do ecossistema React, como o React Bits.
- Base de UI: Tailwind v4 + shadcn/ui (o React Bits instala pelo CLI do shadcn, variante TS + Tailwind). Ícones: Lucide.
- Sai: Angular, PrimeNG, PrimeIcons. Fica igual: TipTap, dice-box, FullCalendar (versões React), motor de regras D&D em TypeScript no front, SignalR (`@microsoft/signalr`), deploy no Cloudflare Pages.
- Onde o Documento do projeto ainda falar em Angular/PrimeNG, vale esta seção.

## Decisões fixas (resumo)

- Stack: React + Vite + TypeScript, Tailwind + shadcn/ui, React Bits (efeitos), React Router, TanStack Query, Zustand, React Hook Form + Zod · ASP.NET Core .NET 10 · PostgreSQL + EF Core · SignalR · Hangfire · Gmail SMTP · Supabase Storage.
- Hospedagem grátis: Cloudflare Pages (rpguild.pages.dev) + Oracle Always Free (API em rpguild.duckdns.org).
- v1 = só D&D 2024 (+ conteúdo 2014 marcado "Legacy"), automação completa, ficha no layout do D&D Beyond + painel de rolagens + aba Anotações + Extras.
- Rolagem sorteada no servidor; motor de regras só no front.
- Vantagem/desvantagem: seletor que vale uma rolagem e volta para Normal.
- Monorepo `/web`, `/api`, `/docs`; Git Flow (main, develop, feature/*); Conventional Commits; PR para develop.
- Sem prazo fixo. Custo zero.

## Identidade visual (resumo)

- Taverna medieval, só tema escuro. Fundo `#15100c`, cards `#211812`, texto `#f2e7d3`, dourado `#d4a24c`.
- Títulos: Cinzel Decorative 700. Texto e ficha: EB Garamond (números com lining/tabular nums).
- Logo: escudo dourado com d20; arquivos PNG em `design-system/logo/` (não há vetor de verdade ainda).
