# RPGuild — Estado atual e decisões recentes

Atualizado em 27/11/2026. Este arquivo complementa o "Documento do projeto": onde os dois divergirem, vale este.

## Onde as coisas estão

| O quê | Onde |
| --- | --- |
| Documento do projeto (visão, regras, telas, modelo de dados, arquitetura, endpoints) | `RPGuild - Documento do projeto.md` (cópia online: https://claude.ai/artifact/EXNiR9iQK7saQz8z4SQfa5); no repositório, `docs/documento-do-projeto.md` |
| Design system (cores, fontes, logo, Tailwind/shadcn) | `docs/design-system/` (online: https://claude.ai/artifact/Cx3CwKHaM7kTE5zS9KoFJb) |
| Protótipo (Fase 1, pronto) | Claude Design: https://claude.ai/artifact/8fy5sfDaAmNEEhSwbnGLqk |
| Repositório | https://github.com/Matheus-Oli/RPGuild (monorepo; docs em `docs/`, inclusive `docs/estado-atual.md`) |
| Pasta local | Monorepo em `C:\Users\Usuario\Downloads\Projetos\RPGuild` (a pasta `Projetos\RPG` é o repositório antigo, só de documentos) |
| Backlog | Trello, quadro RPGuild: https://trello.com/b/qBRH7HXp/rpguild (desatualizado; não é usado no acompanhamento da Fase 2, e o fluxo de PR não depende dele) |

## Mudança de plano: ordem de execução

O backlog antigo (S0 a S10, fatiado por camada) foi descartado. A nova ordem é:

1. **Protótipo**: todas as telas no Claude Design, usando o design system da RPGuild. Uma tela (ou grupo) por vez, seguindo o mapa de telas do documento.
2. **Back-end e banco**: um módulo por vez, nesta ordem (atualizada em 27/11/2026): setup; contas; taberna; notificações; campanhas e convites; **fichas (base)**; sessões e lembretes; documentos; saída de player; **rolagens**; **conteúdo D&D (regras do livro)**. Cada módulo com entidades, migration, endpoints, regras e testes. Antes de cada módulo, revisar os endpoints contra o protótipo.
3. **Front-end + integração**: uma tela do protótipo por vez, já ligada na API, **na mesma ordem do back** (fichas base antes; rolagens e regras do livro por último).
4. **Deploy e polimento**: VM Oracle, DuckDNS + Caddy, CI/CD, mobile, backup.

### Regras do livro por último (27/11/2026)

- O que depende das regras do livro fica para o fim, no back e no front: o conteúdo do SRD 2024 + Legacy (seed), todas as regras, inclusive as pequenas, e a automação da ficha (motor de regras, criação passo a passo com as escolhas do livro, subir de nível, bônus calculados, inventário com catálogo).
- A parte da ficha que **não** depende das regras entra antes, no módulo **Fichas (base)**: criar, editar (PATCH parcial), permissões (dono e mestre), retrato, anotações, excluir com dupla confirmação, NPCs do mestre e tempo real.
- **Rolagens** vem depois de Saída de player, logo antes do Conteúdo D&D.

## Andamento da Fase 2 (back-end) — 27/11/2026

| Módulo | Status | PR |
| --- | --- | --- |
| Setup (Postgres, EF Core, health check, base de testes) | Mergeado | #3 |
| Contas (Identity, JWT + refresh com rotação, recuperação de senha, perfil, troca de e-mail e senha, avatar no Supabase) | Mergeado | #4 |
| Taberna (amizades) | Mergeado | #5 |
| Notificações (serviço central da matriz, sininho paginado, fila de e-mail no Hangfire, limpeza de 90 dias, `NotificationHub`) | Mergeado | #6 |
| Campanhas e convites | PR aberto (partes 1 a 3: CRUD, grade, convites, tempo real) | #7 |
| Fichas (base), Sessões e lembretes, Documentos, Saída de player, Rolagens, Conteúdo D&D | A fazer, nesta ordem | — |

**Como o back está sendo feito**

- TDD (red → green → refactor), com testes unitários e de integração (Postgres real via Testcontainers, container reaproveitado em dev).
- Uma branch `feature/<modulo>` e um PR para `develop` por módulo, aberto só quando o módulo fecha.
- Prompts para o Claude Code montados no projeto RPGuild do Claude; decisões técnicas são perguntadas ao Matheus antes.
- Antes de cada módulo, as telas do protótipo daquele módulo são conferidas contra os endpoints.
- **Campos que dependem de outros módulos** (ex.: campanhas em comum, próxima sessão, classe/nível/PV dos jogadores): entram já no módulo atual, vazios (null, 0 ou lista vazia) e com `TODO`, e passam a ser calculados no módulo dono do dado.
- Os detalhes de cada módulo (regras, endpoints, eventos, configuração) ficam no `api/README.md` do repositório.

## Nova feature v1: visão geral da campanha — "Onde paramos" e "Atividade recente" (26/11/2026)

Vêm do protótipo (página da campanha, seção Visão geral) e entram na v1. Os detalhes são definidos nos módulos donos dos dados.

- **Onde paramos:** resumo da última sessão jogada (título, data e texto) e o link "Ler o diário da campanha". Detalhes no módulo de Sessões (e no de Documentos, para o diário).
- **Atividade recente:** lista curta do que aconteceu na campanha, só com o que é público (ex.: "Selene publicou 'Anotações da biblioteca'", "Você marcou a Sessão 12 para 29/11"). Detalhes nos módulos de Sessões e Documentos.
- No módulo de Campanhas, a página da campanha já devolve esses campos vazios, seguindo a regra dos campos de outros módulos.

## Nova feature v1: grade de campanhas com ordem do usuário (26/11/2026)

Não está no protótipo; entra na v1 por decisão do Matheus.

- A lista de campanhas (`GET /campaigns`) é uma grade. Por padrão, a mais nova vem primeiro.
- Cada usuário pode reorganizar a grade (arrastar) do jeito que quiser. A ordem é **só dele**: não muda a de mais ninguém.
- A ordem fica salva **na API** (mesma ordem no PC e no celular): `PUT /campaigns/order {campaignIds[]}`.
- Campanha nova (criada ou em que o usuário entrou ao aceitar um convite) aparece **no início** da grade.

## Troca do front: Angular → React (24/11/2026)

- Motivo: usar bibliotecas de componentes do ecossistema React, como o React Bits.
- Base de UI: Tailwind v4 + shadcn/ui (o React Bits instala pelo CLI do shadcn, variante TS + Tailwind). Ícones: Lucide.
- Sai: Angular, PrimeNG, PrimeIcons. Fica igual: TipTap, FullCalendar (versões React), motor de regras D&D em TypeScript no front, SignalR (`@microsoft/signalr`), deploy no Cloudflare Pages.
- Onde o Documento do projeto ainda falar em Angular/PrimeNG, vale esta seção.

## Protótipo: decisões gerais de interface (Home, Agenda, Taberna e todas as telas) (nov/2026)

- **Paleta papiro/papel antigo** (substitui o tema escuro "taverna" e o "carvão neutro"): ver "Identidade visual" abaixo.
- **Home:** o hero usa um GIF de paisagem de fantasia (campina com ruínas).
- **Música de fundo em todo o site** (não só na Home): trilha medieval "A Quiet Night – Medieval Village", tocada por um botão de som no menu. O arquivo de áudio fica **no front** (`/web/public`), servido pelo Cloudflare Pages junto com o site (não vai para o Supabase).
- **Agenda:** só a visualização **mensal**, no estilo do mês do Google Calendar.
- **Regras para todas as telas:**
  - Bordas arredondadas (nada de canto reto).
  - Botões sempre na mesma cor dos da Home.
  - Menu, rodapé e animações iguais aos da Home (componentes do React Bits).
  - O menu **não** anima ao surgir (continuidade entre páginas).
  - Título só com fade suave (nada de letra por letra).
  - "Nova campanha" é um **modal**, não uma página.
  - O dado do menu gira e volta para o Início ao ser clicado.
- **Textos e recursos genéricos:** o projeto começa com D&D, mas fichas, rolagens e textos não ficam presos ao D&D.
- **Linguagem para leigos:** termos da ficha com dica (tooltip) de fácil acesso; nas magias, "nível de magia" (não "círculo"), com dica explicando que também se chama ciclo ou círculo.

## Ficha do protótipo: o que entrou (26–27/11/2026)

Tudo abaixo está no protótipo (Ficha, Ficha de NPC e Nova ficha). A ficha **não tem versão mobile**. Cada item indica onde entra: **(base)** no módulo Fichas (base); **(regras)** na automação, com o Conteúdo D&D.

**Estrutura e uso**

- **(base)** Cards numa grade de 12 colunas. No modo "Organizar ficha" o jogador arrasta, redimensiona e recolhe cards; cada card tem tamanho mínimo para não quebrar; botão "Restaurar layout padrão". O layout é de cada usuário.
- **(base)** Valores editáveis no próprio lugar. Lápis no cabeçalho abre "Editar personagem" (nome, espécie, classe, antecedente, nível para cima e para baixo). Botão "← Voltar para a campanha".
- **(base)** Texto de botão que não cabe vai diminuindo a fonte (mínimo 9 px), sem cortar.
- **(base)** Dicas "?" em todos os cards e nos termos principais (CA, proficiência, deslocamento, sintonização, CD de magia, vantagem/desvantagem, descansos, ajustes). Abrem ao passar o mouse, focar ou tocar; fecham ao tocar fora ou com Esc. Texto em linguagem de leigo.
- **(base)** Excluir item e companheiro mostra um aviso com "Desfazer".
- **(base)** Ficha de NPC: o mestre tem fichas dos seus NPCs, iguais à do jogador, com um selo "Ficha de NPC".

**Criação (Nova ficha)**

- **(regras)** Assistente em 7 passos: classe, antecedente, espécie, atributos, equipamento, magias, detalhes; resumo ao lado.
- **(regras)** Mago escolhe 6 magias de nível 1 (grimório) e 3 truques.
- **(base)** Retrato no passo de detalhes, recortado em quadrado.

**Combate, vida e descansos**

- **(regras)** Dados de vida: contador no card de PV. "Descanso curto" abre um popup para gastar dados (dado da classe + CON cada). O descanso longo recupera todos os PV, os espaços de magia, metade dos dados de vida e tira 1 nível de exaustão.
- **(regras)** Concentração: botão "C" nas magias de concentração; só uma ativa por vez (trocar encerra a anterior com aviso). Ao sofrer dano, alerta explícito com a CD (10 ou metade do dano, o que for maior) e o botão "Rolar salvaguarda", que resolve sozinho (manteve/perdeu). Também tem "Rolou na mesa? Manteve / Perdeu". Incapacitado, Inconsciente, Paralisado, Petrificado e Atordoado encerram a concentração. PV em 0 também.
- **(regras)** Crítico: 20 natural num ataque destaca o dano daquela arma; o próximo dano rola o dobro de dados.
- **(regras)** Condições nas rolagens de d20: vantagem/desvantagem automáticas (Envenenado, Amedrontado, Caído, Cego, Impedido, Invisível, Incapacitado), falha automática em salvaguardas de FOR/DES (Atordoado, Paralisado, Inconsciente, Petrificado), exaustão −2 por nível. Vantagem e desvantagem se anulam. O motivo aparece na fórmula.
- **(regras)** Recurso de classe com ativação (exemplo: Marca do Caçador): botão "Conjurar" gasta primeiro o uso grátis e depois um espaço de magia, e ativa a concentração; com a marca ativa, cada arma ganha o botão "+1d6 marca".

**Ajustes manuais**

- **(regras)** Botão "Ajustes" no cabeçalho para bônus que a regra não calcula: item mágico, bênção, regra da casa. Alvos: PV máximo, CA, Iniciativa, Ataque com arma, Dano com arma, Salvaguardas, Perícias, Deslocamento, CD e ataque de magia. Cada ajuste tem valor (±) e origem.
- O ajuste entra direto no número da ficha. Campo com ajuste ganha um ponto dourado; a dica mostra a origem. Editar o campo mexe no valor base.
- O popup também lista, só para leitura, os bônus que vêm de itens.

**Magias**

- **(regras)** Termo: **nível de magia** (ex.: "Magia de nível 1"), com dica avisando que algumas traduções chamam de círculo ou ciclo. Salvaguarda continua "salvaguarda".
- **(regras)** Clique no nome abre o detalhe: conjuração, alcance, componentes, duração, descrição, CD e botões de rolar dano/cura. Texto do SRD 5.2 na versão final.

**Ações, perícias e proficiências**

- **(regras)** "Ataques e ações" com filtro: Todas, Ataque, Ação, Ação bônus, Reação, Outras. Reações incluem Ataque de Oportunidade; magias preparadas aparecem no tipo de ação certo.
- **(regras)** Legenda clara de perícias: "Sem treino · Proficiente +X · Especialista +2X", com uma frase explicando cada um. O mesmo nas salvaguardas.
- **(regras)** Teste com ferramenta: ferramentas em que o personagem é proficiente são clicáveis e rolam d20 + atributo da ferramenta + proficiência (ex.: Ferramentas de ladrão usa DES).

**Inventário e sentidos**

- **(regras)** Sintonização: contador "x/3" e botão "S" nos itens que exigem; limite de 3 com aviso. Bônus do item só vale sintonizado.
- **(base)** Moedas: por padrão aparecem cobre, prata e ouro (PC, PP, PO). Electro e platina são opcionais ("+ Peça de electro", "+ Peça de platina") e podem ser ocultadas.
- **(base)** Sentidos ganhou "Movimento e tamanho": natação, escalada, voo e tamanho, editáveis.

**Pendências de decisão**

- Mestre vê as fichas dos jogadores? No protótipo ele vê só as dos NPCs; a bandeja diz "o mestre vê todas".
- O servidor valida as regras da bandeja (modo, pedido, espera de 4 s) ou só o front?

## Nova feature v1: Bandeja da mesa — rolagens compartilhadas (25/11/2026, regras atualizadas em 26/11/2026)

Entra na v1, dentro do módulo de rolagens. Muda a regra antiga "jogador vê só as rolagens das próprias fichas".

- **Onde fica:** painel lateral (bottom sheet no celular) aberto por um botão flutuante na ficha e na página da campanha. Não é popup, para não tapar a ficha.
- **O que tem dentro:** quem está com a bandeja aberta (avatares), o pote com o dado 3D, o seletor de rolagem e o histórico curto.
- **Como rolar na bandeja:**
  - **Da ficha:** o jogador escolhe a ficha (o mestre vê todas), a categoria (perícias, atributos, salvaguardas, ataques, outros) e o teste. Testes com d20 têm o seletor Normal / Vantagem / Desvantagem.
  - **Dados livres:** d4, d6, d8, d10, d12, d20 e d100 (dois d10), até 12 dados, mais modificador.
- **Quem vê o quê:**
  - Bandeja aberta: toda rolagem (pela ficha ou pela bandeja) anima no pote de todos que estiverem com a bandeja aberta.
  - Bandeja fechada: quem rolou pela ficha vê o dado 3D cair na própria tela e um alerta com o resultado (teste, fórmula, dados e total; destaque para 20 e 1 naturais). A rolagem só entra no histórico da ficha quando o alerta aparece. O painel de rolagens da ficha não abre sozinho.
  - Com a bandeja fechada, a pessoa não vê as rolagens dos outros.
- **Histórico:** a bandeja tem um histórico curto das últimas rolagens da campanha (quem, rolagem, resultado), sem animação, para quem abriu depois.
- **Modo da bandeja (só o mestre muda; todos veem um selo com o modo atual):**
  - `livre`: todos rolam quando quiserem.
  - `so_quando_pedido`: o jogador só rola o teste que o mestre pediu. Na bandeja, o seletor some e aparece um aviso; na ficha, clicar num bônus fora do pedido mostra um aviso e não rola.
  - `pausada`: ninguém rola (cena de narração ou discussão).
  - O mestre sempre pode rolar.
- **Pedido de teste (mestre → jogadores):**
  - O mestre escolhe o teste (perícia, atributo, salvaguarda ou iniciativa), quais jogadores rolam e, se quiser, uma CD que só ele vê.
  - Aparece para todos um card "O mestre pediu: <teste>" com a lista dos chamados (aguardando ou resultado). Quem foi chamado tem o botão de rolar com o próprio bônus; também pode responder pelo bônus certo na ficha.
  - Vale em qualquer modo: o chamado pode rolar aquele teste mesmo com a bandeja em `so_quando_pedido` ou `pausada`.
  - O mestre vê ✓ ou ✗ de cada um contra a CD e pode encerrar o pedido. Quando todos rolam, o pedido fecha sozinho e vira uma linha no histórico ("Pedido · Percepção — Rowan 18 · Muirenn 12 · Selene 7").
  - Só um pedido aberto por vez.
- **Fila no pote:** só uma rolagem anima por vez, com o nome de quem rolou; as outras esperam com o indicador "+N na fila". Se houver mais de 3 na fila, as extras vão direto para o histórico, sem animação.
- **Espera entre rolagens:** cada pessoa espera 4 segundos entre uma rolagem e outra na bandeja (o botão mostra "AGUARDE Xs").
- **Som:** botão no cabeçalho da bandeja para silenciar o som dos dados. Vale para a bandeja e para os dados na tela da ficha; preferência de cada pessoa, salva no navegador.
- **Visibilidade da rolagem** (campo novo em `dice_rolls`):
  - `publica`: padrão, todos veem os valores.
  - `oculta_mestre`: só o mestre pode usar, e só na bandeja (as rolagens feitas pela ficha são sempre públicas). A mesa vê que ele rolou e quantos/quais dados, com os números borrados. Os valores **nunca** são enviados ao front dos jogadores; o servidor manda só os dados sem resultado. Só o mestre recebe o valor real.
  - `secreta`: reservado para sistemas futuros (ex.: esconderijo da furtividade em Ordem 2), definido pela regra do sistema, não pelo jogador. Não existe na v1.
- **Jogador não pode** deixar rolagem privada.
- **Sorteio:** continua no servidor. O servidor envia os valores pelo SignalR e o dado 3D de todos cai no mesmo número.
- **Lib do dado 3D:** troca de `@3d-dice/dice-box` para `@3d-dice/dice-box-threejs`, que aceita resultado predeterminado (`2d20@14,7`). O dice-box original só aceita notação simples e o resultado vem da física. Para rolagem oculta, os jogadores recebem a animação sem os valores reais.
- **Painel do mestre:** continua independente da bandeja, com todas as rolagens da campanha.
- **Fica para depois da v1:** animar no pote só quem está no turno (depende de um módulo de combate) e rolagem livre do jogador só para ele.

## Nova feature v1: inventário com catálogo do livro, item personalizado e item da campanha (26/11/2026)

No protótipo existe só uma amostra (25 itens) para simular. Na Fase 2/3, fazer completo:

- **Do livro (catálogo):** itens do **SRD 5.2** (as regras de 2024 liberadas pela Wizards of the Coast sob licença **CC-BY 4.0**): armas, armaduras, equipamento de aventura, ferramentas e os itens mágicos do SRD. Busca por nome e filtro por categoria. O jogador escolhe a quantidade e onde guardar (Equipamento ou Mochila).
  - Não entra conteúdo do Livro do Jogador que não esteja no SRD (texto protegido por direitos).
  - Cada item com os dados que a automação precisa: peso, preço e, por tipo:
    - **Arma:** dano, tipo de dano, propriedades, alcance, maestria.
    - **Armadura:** categoria, CA base, regra da Destreza (soma / máx. 2 / não soma), Força mínima, desvantagem em Furtividade.
    - **Item mágico:** raridade, sintonização, efeito (inclusive bônus que entram nos números, como +1 na CA).
  - O conteúdo fica no banco, como o resto do conteúdo de regras (módulo Conteúdo D&D).
  - **Crédito obrigatório da licença** visível no site (ex.: na tela de adicionar item e numa página "Sobre/Créditos").
- **Personalizado ("outro"):** nome, categoria (equipamento, arma, armadura, mágico, outro), quantidade, peso e descrição. Se for arma ou armadura, aparecem os mesmos campos de regra do catálogo e o item entra nas contas da ficha (ataques, CA, carga) como um item do livro.
- **Item da campanha (do mestre):** o mestre cria o item uma vez na campanha e entrega a um ou mais jogadores. O item aparece para o jogador (aba "Do mestre" ou direto no inventário) marcado como "dado pelo mestre".
- Cada linha do inventário mostra a origem: livro, personalizado ou mestre.
- Arma equipada aparece em "Ataques e ações" (no protótipo isso já acontece). Armadura e escudo entram no cálculo da CA (no protótipo, ainda não).

## Retenção das rolagens: 7 dias (25/11/2026)

- Toda rolagem (ficha, bandeja, livre do mestre, pública ou oculta) fica salva por **7 dias** e depois é apagada do histórico.
- Vale para todos os históricos: bandeja, histórico da ficha e painel do mestre.
- Ninguém edita nem apaga rolagem na mão; a exclusão é só automática.
- Implementação: job recorrente diário no Hangfire que apaga de `dice_rolls` o que tiver `created_at` com mais de 7 dias.
- Substitui a regra antiga "histórico imutável que não pode ser apagado".

## Ideia para depois da v1: Grid tático (estilo Owlbear Rodeo) (25/11/2026)

Não entra na v1. Fica registrado para depois.

- **Quando implementar, vai completo**, incluindo fog of war.
- **Tokens:** o jogador move só os dele; o mestre move todos.
- **Diferencial sobre o Owlbear:** token vinculado à ficha do RPGuild (HP, CA e condições lidos da ficha e atualizados em tempo real), clique no token abre a ficha, rolagem pelo token sai na bandeja da mesa. No Owlbear isso só existe por extensões, com HP guardado no próprio token ou ponte só de rolagens (Beyond20 + Owl20).
- **Escopo previsto:** upload de mapa, grid quadrado ajustável (tamanho e offset), tokens com snap, zoom/pan, régua, ping, fog of war, tokens ocultos, camadas (mapa, tokens, mestre). Extras possíveis: desenho, áreas de magia (cone, esfera), grid hexagonal, iniciativa.
- **Stack prevista (custo zero):** `react-konva` (Konva.js) no front; SignalR no `CampaignHub` (`TokenMoved`, `MapChanged`, `FogChanged`); tabelas `maps` e `map_tokens` no Postgres.
- **Ponto de atenção (custo):** imagem de mapa é pesada. O Supabase grátis tem 1 GB de armazenamento e 5 GB de egress + 5 GB de egress em cache por mês. Proposta: comprimir para WebP no upload, cachear no navegador e guardar os mapas no disco da VM Oracle, servidos pelo Caddy, fora do Supabase.

## Decisões fixas (resumo)

- Stack: React + Vite + TypeScript, Tailwind + shadcn/ui, React Bits (efeitos), React Router, TanStack Query, Zustand, React Hook Form + Zod, dice-box-threejs · ASP.NET Core .NET 10 · PostgreSQL + EF Core · SignalR · Hangfire · Gmail SMTP · Supabase Storage.
- Hospedagem grátis: Cloudflare Pages (rpguild.pages.dev) + Oracle Always Free (API em rpguild.duckdns.org).
- v1 = só D&D 2024 (+ conteúdo 2014 marcado "Legacy"), automação completa, ficha no layout do D&D Beyond + painel de rolagens + aba Anotações + Extras + bandeja da mesa.
- Rolagem sorteada no servidor; motor de regras só no front.
- Rolagens guardadas por 7 dias e apagadas automaticamente.
- Vantagem/desvantagem: seletor que vale uma rolagem e volta para Normal.
- Monorepo `/web`, `/api`, `/docs`; Git Flow (main, develop, feature/*); Conventional Commits; PR para develop.
- Sem prazo fixo. Custo zero.

## Identidade visual (resumo)

- **Paleta papiro/papel antigo** (substitui o tema escuro `#15100c`/`#211812`/`#f2e7d3`/`#d4a24c` e o "carvão neutro"): fundo em gradiente leve entre `#e7dcd0` e `#c8b89b`; complementares `#315b85`, `#1c3a69`, `#7a1212` e `#b04a2d`; preto onde fizer sentido. Se o `docs/design-system/` ainda estiver com a paleta escura, vale esta.
- Títulos: Cinzel Decorative 700. Texto e ficha: EB Garamond (números com lining/tabular nums). Na ficha, os números grandes usam Cinzel.
- Logo: escudo dourado com d20; arquivos PNG em `docs/design-system/logo/` (não há vetor de verdade ainda).
