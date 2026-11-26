# RPGuild

O RPGuild tem cara de taverna medieval vista à noite: madeira escura, texto cor de pergaminho e dourado nos pontos que importam. É só tema escuro. Os títulos usam uma fonte de inscrição decorada (Cinzel Decorative) e todo o resto usa uma serifa clássica de livro (EB Garamond), para o site inteiro ter cara de livro de fantasia sem perder a leitura.

Esta é a **primeira versão**. Tem a logo (escudo com d20 dourado), as fundações (cores, tipografia, espaçamento, raios) e os dez símbolos das faces do dado. Os componentes vêm do shadcn/ui (Tailwind), ajustados com estes tokens; os efeitos animados vêm do React Bits.

## Princípios

- **Clima nos detalhes, clareza no uso.** A fantasia aparece na paleta, nas fontes e nas bordas; formulários, tabelas e a ficha seguem limpos e funcionais.
- **Dourado é raro.** `gold-500` marca uma ação principal por tela, o item ativo do menu, o foco e os números em destaque. Se tudo é dourado, nada é.
- **Bordas em vez de sombras.** As camadas se separam por tom (`bg-900` → `surface-800` → `surface-700`) e por bordas `border-600`. Nada de sombras pesadas ou gradientes.
- **Cor sempre com significado.** `ember-400` = perigo e dano, `moss-400` = sucesso e cura, `arcane-300` = informação e magia. Toda cor de estado vem acompanhada de texto ou ícone.

## Voz e textos

- Português do Brasil, frases curtas, tom de mesa entre amigos.
- Nomes do produto: **Taberna** (amigos), **Campanhas**, **Agenda**, **Mestre**, **Jogador**, **Ficha**.
- Botões dizem a ação: "Convidar", "Marcar sessão", "Rolar", "Excluir campanha". Nada de "OK" genérico.
- Confirmações destrutivas usam a dupla verificação: o nome do item e depois a frase "desejo excluir …".
- Empty states explicam e oferecem o próximo passo: "Você ainda não tem campanhas. Crie a primeira ou peça um convite ao seu mestre."

## Cores

| Token | Uso |
| --- | --- |
| `bg-900` | Fundo da página |
| `surface-800` | Cards, menu, colunas da ficha |
| `surface-700` | Inputs, modais, hover |
| `border-600` | Divisórias e bordas de card |
| `border-strong` | Borda de controles (inputs, selects, checkboxes) |
| `ink` / `ink-muted` | Texto principal / secundário |
| `gold-500` + `ink-on-gold` | Ação principal, ativo, foco, destaque |
| `ember-400` | Perigo, erro, dano |
| `moss-400` | Sucesso, cura, presença confirmada |
| `arcane-300` | Informação, magia, vantagem |

Todos os textos passam de 4.5:1 sobre as três superfícies (o menor é `ember-400` sobre `surface-700`, 4.65:1).

## Tipografia

- **Cinzel Decorative** (Google Fonts, 700) em `display`, `h1` e `h2`. Só títulos curtos: nunca em texto corrido, botões, rótulos ou números.
- **EB Garamond** (Google Fonts, 400, 600 e 700) em todo o resto: texto, formulários, botões, ficha. Como a Garamond é mais miúda, o corpo é 17px e as legendas 13px.
- Números da ficha (modificadores, CA, PV, rolagens): sempre `font-variant-numeric: lining-nums tabular-nums`, porque a Garamond usa algarismos antigos (de alturas diferentes) por padrão.
- Carregue as duas pelo Google Fonts: `https://fonts.googleapis.com/css2?family=Cinzel+Decorative:wght@700&family=EB+Garamond:wght@400;600;700&display=swap`.

## Espaçamento e forma

- Escala de 4 em 4: `space-1` 4 · `space-2` 8 · `space-4` 16 · `space-6` 24 · `space-8` 32.
- Raios: `radius-sm` 4 (badges e caixinhas da ficha), `radius-md` 8 (botões, inputs, cards) e `radius-lg` 12 (modais, painel de rolagens, banner).

## Logo e iconografia

- **Logo:** escudo ornamental dourado com um d20 ao centro, cujas faces trazem os símbolos da marca (`assets/Logos`). A paleta dele (dourado, marrom e pergaminho) é a mesma dos tokens `gold-500`, `border-600` e `ink`.
  - `rpguild_logo_completo.png`: escudo + dado. É a versão principal: home, tela de login, e-mails, favicon grande.
  - `rpguild_dado.png`: só o d20. Para espaços pequenos: menu, favicon, avatar padrão, botão de rolar.
  - `rpguild_escudo.png`: só o escudo vazio. Uso decorativo (moldura de destaque, fundo de empty state), nunca como logo.
- **Logotipo:** "RPGuild" em Cinzel Decorative 700, cor `ink`, ao lado ou abaixo do dado/escudo. O "RPG" pode ir em `gold-500`.
- **Tamanho mínimo:** 32px de altura para o dado e 64px para a logo completa. Abaixo disso os símbolos das faces viram ruído.
- **Fundo:** sempre sobre `bg-900` ou `surface-800`. Não aplicar sobre fotos nem sobre `gold-500`.
- **Símbolos** (`assets/Simbolos`): os dez desenhos das faces (espadas cruzadas, escudo, elmo, chama, dragão, poção, machados cruzados, estrela arcana, lua crescente, coroa). São ilustrações, não ícones de interface: use em empty states, cards de campanha, conquistas e na home, a partir de 48px.
- **Ícones de interface:** **Lucide** (`lucide-react`, padrão do shadcn/ui), em `ink-muted` quando inativos e `ink` ou `gold-500` quando ativos.
- **Formato dos arquivos:** os arquivos chegaram como `.svg`, mas cada um contém uma imagem PNG embutida, sem vetor de verdade. Aqui eles estão como PNG. Para escalar sem perder qualidade (favicon nítido, impressão, animação), a logo precisa ser vetorizada de verdade.

## Usando com Tailwind + shadcn/ui (React)

- Tailwind v4: declare os tokens no `@theme` do CSS global (`--color-gold-500`, `--color-surface-800`, `--spacing-4`, `--radius-md` …) para virarem classes (`bg-surface-800`, `text-gold-500`).
- shadcn/ui: mapeie as variáveis do tema para os tokens:
  - `--background` → `bg-900`; `--card` e `--popover` → `surface-800`; `--muted` e `--secondary` → `surface-700`;
  - `--border` e `--input` → `border-600`; `--ring` → `gold-500`;
  - `--primary` → `gold-500` com `--primary-foreground` → `bg-900` (ink-on-gold);
  - `--foreground` → `ink`; `--muted-foreground` → `ink-muted`;
  - `--destructive` → `ember-400`; sucesso → `moss-400`; info → `arcane-300`.
- Só modo escuro: deixe a classe `dark` fixa no `<html>`. Não existe tema claro.
- React Bits: instale pelo CLI do shadcn na variante **TS + Tailwind** (`npx shadcn@latest add @react-bits/<Componente>-TS-TW`) e troque as cores fixas dos componentes pelos tokens. Só a versão gratuita (Pro é paga).
- Componentes próprios (ficha, painel de rolagens, agenda) usam as mesmas classes e variáveis.

## Próximos passos deste sistema

Componentes a documentar aqui conforme forem construídos:
- botão (primário, secundário, perigo);
- input e select;
- card de campanha;
- caixinha de atributo e linha de perícia da ficha;
- card de rolagem;
- modal de dupla confirmação;
- empty state;
- toast;
- badge de status (vou, não vou, talvez).
