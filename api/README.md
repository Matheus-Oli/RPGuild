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
- O `TimeProvider` da API é o `RelogioEmMicrossegundos`, que trunca o "agora" para a precisão do Postgres. Assim a data que um `POST` devolve é exatamente a que um `GET` lê depois. Nos testes, a `RPGuildApiFactory` embrulha o relógio injetado do mesmo jeito.
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
| `POST /api/auth/forgot-password` | `{email}` | 204 e manda o link por e-mail · 404 se o e-mail não tem conta |
| `POST /api/auth/reset-password` | `{token, novaSenha}` | 204 · 400 se o token for inválido, usado ou vencido, ou se a senha não seguir a política |

Os endpoints do próprio usuário (`/api/me`) estão em [Perfil](#perfil).

Erros em ProblemDetails: 400 validação (`errors` por campo, em camelCase) ou token de redefinição inválido · 401 credencial ou sessão inválida (mensagem genérica, sem dizer se foi o e-mail ou a senha) · 404 e-mail não cadastrado (forgot-password) · 409 e-mail ou nick em uso · 423 conta bloqueada.

**Fluxo**

1. **Access token** (JWT HS256, 15 min, claims `sub`, `email`, `nick`, `name` e `sid`): vai no corpo do login/refresh e o front manda no header `Authorization: Bearer`. Validade conferida sem tolerância de relógio.
   - `sid` é a **sessão**: o `family_id` do refresh token. O login cria a família e o refresh mantém o mesmo `sid`. É com ele que a troca de senha e a confirmação de e-mail derrubam "as outras sessões" e mantêm a atual.
2. **Refresh token** (64 bytes aleatórios, 14 dias): vai **só** no cookie `rpguild_refresh` (`HttpOnly`, `Secure`, `SameSite=None`, `Path=/api/auth`), nunca no corpo. No banco (`refresh_tokens`) fica só o hash SHA-256.
3. **Rotação:** cada `refresh` revoga o token usado (`revoked_at`, `replaced_by_id`) e emite outro na mesma família (`family_id`). Cada login começa uma família nova.
4. **Reuso:** se chegar um refresh já revogado, alguém pode ter copiado o cookie. A família inteira é revogada e a resposta é 401: os dois lados precisam entrar de novo.
5. **Lockout:** 5 senhas erradas bloqueiam a conta por 15 minutos (423). A senha atual errada em `PUT /api/me/email` e `PUT /api/me/password` também conta, no mesmo contador do login (`AccessFailedAsync`). Com a conta bloqueada, os dois endpoints respondem 423 como o login, mesmo com a senha certa, e a senha certa zera o contador.
6. **Regras de conta:** nick com 3 a 20 caracteres (`A-Z`, `a-z`, `0-9`, `_`), único sem diferenciar caixa (`nick_normalizado`); e-mail único; senha com 8+ caracteres, uma letra e um número.

**Recuperação de senha**

1. `forgot-password` com um e-mail que **não** tem conta responde 404. Com conta, responde 204 e manda o e-mail com o link `{Front:BaseUrl}/redefinir-senha?token=<token>`. O e-mail não vai na URL.
2. O token é opaco: `Base64Url("{userId}:{token do Identity}")`, gerado por `GeneratePasswordResetTokenAsync`. Ele vale **1 hora** (`DataProtectionTokenProviderOptions.TokenLifespan`).
3. **Uso único:** o reset troca o security stamp do usuário, e o token usado (e qualquer outro emitido antes dele) deixa de valer.
4. Depois do reset, **todas as sessões caem**: todos os refresh tokens do usuário são revogados e é preciso entrar de novo. O lockout não muda. A revogação é a mesma da troca de senha e de e-mail (`RefreshTokenService.RevogarOutrasSessoesAsync`), aqui sem sessão atual.
5. As chaves do Data Protection, que assinam o token, ficam no Postgres (`data_protection_keys`, app `RPGuild`). Assim o link continua valendo depois de reiniciar a API.

No front, as chamadas a `/api/auth/*` precisam de `credentials: "include"` para o navegador mandar e receber o cookie.

**Configuração**

| Chave | Para quê | Dev (`appsettings.Development.json`) | Produção (variável de ambiente) |
| --- | --- | --- | --- |
| `Jwt:Key` | Chave HS256, mínimo 32 bytes | chave fixa de dev | `Jwt__Key` (obrigatória; sem ela a API não sobe) |
| `Jwt:Issuer` | Emissor do token | `rpguild-api` | `Jwt__Issuer` |
| `Jwt:Audience` | Público do token | `rpguild-web` | `Jwt__Audience` |
| `Cors:Origins` | Origens do front (com `AllowCredentials`) | `http://localhost:5173` | `Cors__Origins__0`, `Cors__Origins__1`... |
| `Front:BaseUrl` | Base dos links nos e-mails (obrigatória; sem ela a API não sobe) | `http://localhost:5173` | `Front__BaseUrl` |

Nos testes, a `RPGuildApiFactory` define uma chave própria e aceita um `TimeProvider` (por exemplo `FakeTimeProvider`) para controlar o relógio. O helper `CriarUsuarioELogarAsync()` devolve o access token e o cookie de refresh; `LogarAsync(usuario)` abre outra sessão do mesmo usuário e `SessaoContinuaAsync(cookie)` diz se uma sessão ainda renova. O `PedirRedefinicaoAsync()` devolve o token de redefinição que veio no e-mail, e o `PedirTrocaDeEmailAsync(logado)`, o token de troca de e-mail.

## Perfil

Endpoints do usuário logado. Todos exigem `Authorization: Bearer`, menos o `confirm`, que é anônimo porque o link chega por e-mail.

| Endpoint | Entrada | Saída |
| --- | --- | --- |
| `GET /api/me` | — | 200 `{id, nome, nick, email, avatarPath, avatarUrl}` |
| `PUT /api/me` | `{nome, nick, avatarPath?}` | 200 com o perfil · 400 avatar fora da pasta do usuário · 409 nick em uso |
| `PUT /api/me/email` | `{novoEmail, senha}` | 202 e manda a confirmação para o novo endereço · 400 senha incorreta ou mesmo e-mail · 409 e-mail em uso · 423 conta bloqueada |
| `POST /api/me/email/confirm` | `{token}` (anônimo; Bearer opcional) | 204 · 400 token inválido, usado ou vencido · 409 e-mail tomado depois do pedido |
| `PUT /api/me/password` | `{atual, nova}` | 204 · 400 senha atual incorreta ou nova fora da política · 423 conta bloqueada |

**Nome, nick e avatar (`PUT /api/me`)**

1. Nome obrigatório (até 100 caracteres). O nick segue a regra do cadastro e pode ser trocado se estiver livre (único sem diferenciar caixa; trocar só a caixa do próprio nick vale).
2. `avatarPath` é o `path` devolvido pelo [`POST /api/uploads/sign`](#uploads). Só vale um path dentro da pasta do próprio usuário (`avatars/{userId}/...`, sem `..`, `.`, `\` ou segmentos vazios); senão, 400. `null` remove o avatar.
3. **Ao trocar ou remover o avatar, o arquivo antigo é apagado do bucket**, depois de salvar o perfil. Se a exclusão falhar, a falha vai para o log (`Warning`, com o path) e a requisição segue com 200; o arquivo fica órfão no bucket.
4. `avatarUrl` é a URL pública do avatar (bucket público), montada a partir do `avatarPath`; `null` sem avatar.

**Troca de e-mail**

1. `PUT /api/me/email` confere a senha atual, grava o endereço em `users.email_pendente` e manda para o **novo** e-mail o link `{Front:BaseUrl}/confirmar-email?token=<token>`. O e-mail da conta **não muda** ainda; o login continua com o antigo.
2. O token tem o mesmo formato do de redefinição (`Base64Url("{userId}:{token do Identity}")`, mesmo codec), gerado por `GenerateChangeEmailTokenAsync` para o e-mail pendente. Vale **1 hora** (o mesmo provider do reset). Um pedido novo troca o pendente, e o token do pedido anterior deixa de valer.
3. `POST /api/me/email/confirm` aplica a troca com `ChangeEmailAsync` (o `UserName` acompanha o e-mail, como no cadastro), limpa o `email_pendente` e troca o security stamp: o token é de uso único.
4. Depois da troca, o **e-mail antigo** recebe o aviso "e-mail alterado" e as **outras sessões caem**. Se a confirmação vier com Bearer, a sessão do `sid` dele continua; sem Bearer, todas caem.

**Troca de senha**

`PUT /api/me/password` confere a senha atual (400 se errada) e aplica a mesma política do cadastro na nova. No sucesso, as **outras sessões caem** (a do `sid` continua) e o usuário recebe o aviso "senha alterada", que orienta a usar o "Esqueci minha senha" se não foi a própria pessoa que trocou.

## Uploads

| Endpoint | Entrada | Saída |
| --- | --- | --- |
| `POST /api/uploads/sign` | `{tipo, contentType}` (Bearer) | 200 `{uploadUrl, path, publicUrl}` · 400 tipo não suportado ou `contentType` fora da lista |

Imagens ficam no Supabase Storage, no bucket público `imagens`. A API não recebe o arquivo: ela gera uma URL assinada e o front sobe direto no Storage.

**Fluxo**

1. O front chama `POST /api/uploads/sign` com o `tipo` e o `contentType` do arquivo. Tipos: `avatar`, `capa` e `banner` (os dois últimos da [campanha](#campanhas)); retrato responde 400 ("ainda não suportado").
2. `contentType` permitido (o mesmo para os três tipos): `image/jpeg`, `image/png`, `image/webp`, `image/gif`. A API escolhe o path, com a extensão vinda do `contentType` (`jpg`, `png`, `webp`, `gif`):
   - avatar: `avatars/{userId}/{guid}.{ext}`;
   - capa e banner: `campaigns/{userId}/{guid}.{ext}`. A pasta é de quem sobe, porque a campanha ainda não existe na hora do upload.
3. A resposta traz a `uploadUrl` (assinada, com `?token=`; o Supabase a deixa valer por 2 horas), o `path` e a `publicUrl`.
4. O front sobe o arquivo com `PUT` na `uploadUrl`, corpo binário e `Content-Type` do arquivo. Não precisa de chave nem de Bearer: o token da URL autoriza só aquele path.
5. Com o upload feito, o front manda o `path` em `PUT /api/me` (`avatarPath`) ou no `POST`/`PUT` da campanha (`capaPath`, `bannerPath`). A imagem antiga é apagada do bucket nessa hora.

**Limites:** 5 MB por arquivo e os mesmos tipos de imagem, configurados **no bucket** do Supabase. Como a API não vê o arquivo, quem barra o tamanho é o Storage.

**Implementação:** `IStorageService` (Application) com `CriarUrlDeUploadAsync`, `ObterUrlPublica` e `ApagarAsync`. O `SupabaseStorageService` (Infrastructure) usa a API REST do Storage com `HttpClient`, sem SDK:

| Operação | Requisição |
| --- | --- |
| URL assinada de upload | `POST {Url}/storage/v1/object/upload/sign/{bucket}/{path}` → `{url}` relativa a `/storage/v1` |
| URL pública | `{Url}/storage/v1/object/public/{bucket}/{path}` (sem chamada) |
| Exclusão | `DELETE {Url}/storage/v1/object/{bucket}` com `{"prefixes": [path]}` |

A chave é do formato novo (`sb_secret_...`), que não é JWT: ela vai **só** no header `apikey`. No `Authorization: Bearer` o Supabase tenta ler como JWT e rejeita. O `HttpClient` tem os headers redigidos no log.

**Configuração**

| Chave | Valor | Dev | Produção |
| --- | --- | --- | --- |
| `Supabase:Url` | base do projeto, `https://<projeto>.supabase.co` (sem `/rest/v1` nem `/storage/v1`) | `.env` | `Supabase__Url` |
| `Supabase:ServiceKey` | secret key do projeto (`sb_secret_...`) | `.env` | `Supabase__ServiceKey` |
| `Supabase:Bucket` | `imagens` (público) | `.env` | `Supabase__Bucket` |

- Sem `Supabase:Url`, `Supabase:ServiceKey` ou `Supabase:Bucket` (ou com uma `Url` que não é absoluta), a API **não sobe**, e a mensagem diz quais chaves faltam. Vale também para o `dotnet ef`, como no e-mail.
- A `ServiceKey` **nunca** vai para o front, para o log ou para arquivo versionado. No `.env.example` as chaves ficam vazias.
- Nos testes, a `RPGuildApiFactory` troca o storage por um `FakeStorageService`, que guarda em memória os paths com URL criada e os apagados (`factory.Storage.Apagados`) e pode simular falha na exclusão (`FalharAoApagar`). O `SupabaseStorageService` não tem teste automatizado; ele é validado manualmente contra o projeto real.

## Taberna

Amizades entre usuários. Todos os endpoints exigem `Authorization: Bearer` (401 sem token).

| Endpoint | Entrada | Saída |
| --- | --- | --- |
| `GET /api/friends` | — | 200 `[{userId, nome, nick, avatarUrl, campanhasEmComum, amigosDesde}]` |
| `GET /api/friends/requests` | — | 200 `{recebidos: [{id, de, criadoEm}], enviados: [{id, para, criadoEm}]}` |
| `POST /api/friends/requests` | `{emailOuNick}` | 201 `{id, para, criadoEm}` · 400 vazio, inválido ou para si mesmo · 404 ninguém com esse e-mail ou nick · 409 já amigos ou pedido pendente |
| `POST /api/friends/requests/{id}/accept` | — | 204 · 404 pedido inexistente ou que não é para mim · 409 não está pendente |
| `POST /api/friends/requests/{id}/decline` | — | 204 · 404 · 409 (mesmas regras do aceite) |
| `DELETE /api/friends/requests/{id}` | — | 204 (cancela o pedido enviado) · 404 inexistente ou não é meu · 409 não está pendente |
| `DELETE /api/friends/{userId}` | — | 204 (desfaz a amizade) · 404 se não são amigos |

`de` e `para` são `{userId, nome, nick, avatarUrl}`.

**Modelo:** tabela `friendships` (`id`, `requester_id`, `addressee_id`, `status` = `pendente` / `aceita` / `recusada`, `created_at`, `responded_at`), com FKs para `users` em cascade. Há **uma linha por par de usuários**, não importa quem pediu: o índice único `ix_friendships_par` é sobre `(LEAST(requester_id, addressee_id), GREATEST(requester_id, addressee_id))`. Esse índice é de expressão e fica só na migration `AddFriendships`, porque o EF não modela esse tipo.

**Enviar**

1. `emailOuNick` com `@` no meio é e-mail, e a busca é por e-mail. Senão, é nick, com ou sem `@` na frente (`@muirenn`), sem diferenciar caixa (`Nick.Normalizar`). Vazio, só `@` ou nick fora da regra dá 400.
2. Para si mesmo dá 400 e ninguém encontrado dá 404.
3. Já são amigos dá 409, e já ter um pedido pendente meu para a pessoa também.
4. Pedido pendente **dela para mim** dá 409 com "Essa pessoa já te enviou um pedido. Aceite na sua lista." Não há aceite automático. Se os dois pedirem ao mesmo tempo, o índice único do par barra o segundo (Postgres 23505 em `ix_friendships_par`) e ele recebe esse mesmo 409.
5. Um pedido **recusado** pode ser reenviado na hora, por qualquer um dos dois. A linha recusada vira um pedido pendente novo, com o mesmo id, o novo sentido e o `created_at` do reenvio.
6. O destinatário recebe o aviso nº 5 ([Notificações](#notificações)): `amizade_recebida` no sininho e o e-mail com o nome e o @nick de quem pediu e o link `{Front:BaseUrl}/taberna`. O e-mail vai pela fila, então o 201 não depende do envio.

**Responder e desfazer**

- Só o destinatário aceita ou recusa, e só quem enviou cancela. Para qualquer outra pessoa, o pedido "não existe" (404).
- Só pedido pendente pode ser respondido ou cancelado (409).
- Aceitar e recusar preenchem o `responded_at`. Cancelar apaga a linha.
- Quem pediu recebe no sininho `amizade_aceita` (nº 6) ou `amizade_recusada` (nº 7), com o id, o nome e o nick de quem respondeu. Não há e-mail.
- Remover amigo apaga a linha e quem foi removido não recebe nada (aviso nº 8 = N). Qualquer um dos dois pode remover, e depois os dois podem pedir de novo.

**Listas**

- `GET /api/friends`: as amizades mais recentes primeiro (`amigosDesde` = `responded_at` do aceite, decrescente). `campanhasEmComum` conta as campanhas em que os dois participam, como mestre ou jogador (convite pendente não conta). Vem de Campanhas pela porta `ICampanhasEmComum`.
- `GET /api/friends/requests`: só os pendentes, os mais novos primeiro, separados em recebidos e enviados.
- **Nenhuma das duas listas é paginada.**
- Nenhuma resposta da Taberna tem e-mail. Mesmo num pedido feito por e-mail, o enviado mostra o @nick.
- O `avatarUrl` é a URL pública do avatar no storage (o mesmo `IStorageService.ObterUrlPublica` do perfil), ou null.

Nos testes, `TabernaDeTeste` cria vários usuários logados (`CriarUsuariosLogadosAsync`, `CriarDoisUsuariosLogadosAsync`) e deixa dois amigos direto no banco (`TornarAmigosAsync`, com o "amigos desde" controlável). `ClienteDaTaberna` faz as chamadas e monta as URLs.

## Campanhas

Todos os endpoints exigem `Authorization: Bearer` (401 sem token).

| Endpoint | Entrada | Saída |
| --- | --- | --- |
| `GET /api/systems` | — | 200 `[{slug, nome, ativo}]` |
| `POST /api/campaigns` | `{nome, descricao?, sistema, capaPath?, bannerPath?, convidados?}` | 201 detalhe · 400 |
| `GET /api/campaigns` | — | 200 `[card]`, na ordem da grade do usuário |
| `PUT /api/campaigns/order` | `{campaignIds[]}` | 204 · 400 |
| `GET /api/campaigns/{id}` | — | 200 detalhe · 404 |
| `GET /api/campaigns/{id}/members` | — | 200 `[membro]` · 404 |
| `PUT /api/campaigns/{id}` | `{nome, descricao?, capaPath?, bannerPath?}` | 200 detalhe · 400 · 403 · 404 |
| `DELETE /api/campaigns/{id}` | `{confirmacaoNome, confirmacaoFrase}` | 204 · 400 · 403 · 404 |

**Formatos** (nenhum tem e-mail)

- **Card:** `{id, nome, sistema: {slug, nome}, papel: "mestre"|"jogador", capaUrl, mestre: {userId, nome, nick, avatarUrl}, jogadores: [{userId, nome, nick, avatarUrl}], proximaSessao}`. `papel` é o de quem pediu; os jogadores vêm na ordem em que entraram.
- **Detalhe:** os campos do card, mais `descricao`, `bannerUrl`, `criadaEm`, `estatisticas: {sessoesJogadas, horasDeMesa, documentos}`, `ondeParamos` e `atividadeRecente`.
- **Membro:** `{userId, nome, nick, avatarUrl, papel, entrouEm, ficha}`. O mestre vem primeiro, com `entrouEm` = criação da campanha; depois os jogadores, na ordem em que entraram (`joined_at`).
- `capaUrl`, `bannerUrl` e `avatarUrl` são as URLs públicas do storage, ou null.

**Campos de outros módulos:** já vêm na resposta, vazios, com `TODO(<modulo>)` no `CampanhaService`:

| Campo | Valor agora | Módulo |
| --- | --- | --- |
| `proximaSessao` | `null` | Sessões |
| `estatisticas` | `{sessoesJogadas: 0, horasDeMesa: 0, documentos: 0}` | Sessões e Documentos |
| `ondeParamos` | `null` | Sessões |
| `atividadeRecente` | `[]` | Sessões e Documentos |
| `ficha` (membro) | `null` | Fichas |

**Regras**

1. Quem cria é o mestre (`owner_id`). Os jogadores ficam em `campaign_members`; o mestre não entra lá. Não há limite de jogadores. Por enquanto ninguém entra como jogador pela API: quem cria a linha é o aceite do convite (parte 2).
2. Nome obrigatório, até 80 caracteres (sem os espaços das pontas); descrição opcional, até 2000.
3. `sistema` é o slug de um sistema **ativo** (na v1, só `dnd2024`). Inativo ou inexistente dá 400. **O sistema não muda depois de criada:** o `PUT` nem tem o campo (se vier, é ignorado).
4. `game_systems` tem o seed na migration `AddCampanhas`: `dnd2024` (ativo), `ordem2`, `t20`, `coc` e `deadlands` (inativos).

**Acesso** (`AcessoACampanha`, reaproveitável pelos próximos módulos)

- `DeMembroAsync`: mestre ou jogador. Quem não participa recebe **404**, como se a campanha não existisse.
- `DeMestreAsync`: só o mestre. Jogador recebe **403** (`TipoErro.Proibido`); quem não participa, 404.
- Ler (detalhe e membros) é de membro; editar e excluir são de mestre.

**Grade** (`campaign_user_order`: `user_id`, `campaign_id`, `posicao`, PK composta, cascade nos dois)

1. Cada usuário (mestre ou jogador) tem a sua ordem; a de um não muda a de outro.
2. Campanha nova entra **no início** da grade de quem cria (e, na parte 2, de quem aceita o convite), e o resto desce uma posição. Por isso quem nunca reordenou vê a mais nova primeiro.
   - Na criação, a campanha e a posição na grade são gravadas **na mesma transação** (`ITransacao`, implementada por `TransacaoDoBanco`). Se a posição falhar, a campanha não é criada, nada sobra no banco e a resposta é 500 em ProblemDetails.
   - Como proteção, o `GET /api/campaigns` põe no início uma campanha que esteja sem posição.
3. `PUT /api/campaigns/order` recebe **exatamente** as campanhas do usuário: sem repetir, sem faltar e sem sobrar (campanha de outra pessoa ou inexistente conta como sobra); senão, 400. As posições são gravadas numa transação (`GradeDeCampanhas`).

**Imagens (capa e banner)**

1. Os paths vêm do [`POST /api/uploads/sign`](#uploads) com `tipo` `capa` ou `banner`.
2. No `POST` e no `PUT`, o path precisa estar na pasta de campanhas do próprio usuário (`campaigns/{userId}/...`, mesma regra do avatar: sem `..`, `.`, `\` ou segmentos vazios); senão, 400. `null` deixa a campanha sem a imagem (no `PUT`, remove).
3. **Trocar ou remover a capa ou o banner apaga a imagem antiga do bucket**, depois de salvar. Se a exclusão falhar, a falha vai para o log (`Warning`, com o path) e a requisição segue.

**Editar**

- Só o mestre. Responde o detalhe atualizado.
- Se algo mudou, os jogadores recebem o aviso nº 17 `campanha_editada` (só sininho), com `{campanhaId, campanhaNome}`. O mestre não recebe. Um `PUT` sem mudança nenhuma não avisa ninguém.

**Excluir**

1. Só o mestre, com `confirmacaoNome` igual ao nome da campanha e `confirmacaoFrase` igual a `desejo excluir a campanha`. As duas são **exatas, ignorando só os espaços nas pontas**: maiúsculas e acentos contam (`ConfirmacaoDeExclusao`). Se não bater, 400 e nada é apagado.
2. Apaga a campanha e tudo dela: os jogadores (`campaign_members`) e as posições nas grades (`campaign_user_order`), pelo cascade do banco. Capa e banner são apagados do bucket (falha vai para o log e a requisição segue com 204).
3. Os jogadores (lidos **antes** de apagar) recebem o aviso nº 18 `campanha_excluida` no sininho e por e-mail, com `{campanhaNome, mestreNome}`.

**Convites**

| Endpoint | Entrada | Saída |
| --- | --- | --- |
| `POST /api/campaigns` | `convidados?: [userId]` junto com o resto | 201 detalhe · 400 (nada é criado) |
| `POST /api/campaigns/{id}/invites` | `{userIds[]}` | 201 `[{id, convidado, criadoEm}]` · 400 · 403 · 404 · 409 |
| `GET /api/campaigns/{id}/invites` | — | 200 `[{id, convidado: {userId, nome, nick, avatarUrl}, criadoEm}]` · 403 · 404 |
| `GET /api/campaign-invites` | — | 200 `[{id, campanha: {id, nome, sistema: {slug, nome}, capaUrl}, mestre: {...}, criadoEm}]` |
| `GET /api/campaign-invites/{id}` | — | 200 card · 404 |
| `POST /api/campaign-invites/{id}/accept` | — | 204 · 404 · 409 |
| `POST /api/campaign-invites/{id}/decline` | — | 204 · 404 · 409 |
| `DELETE /api/campaign-invites/{id}` | — | 204 · 403 · 404 · 409 |

1. **Quem pode ser convidado:** só amigos do mestre (amizade aceita na Taberna). Cada usuário da lista é conferido e recebe um motivo:
   - `nao_amigo` e `mestre` (o próprio mestre) dão **400**;
   - `ja_jogador` e `ja_convidado` (já tem pendente) dão **409**;
   - com motivos dos dois grupos juntos, **400 vence**.
   - O ProblemDetails traz todos os recusados na extensão `convidados: [{userId, motivo}]`.
2. **Tudo ou nada:** com qualquer recusado, ninguém é convidado. Na criação, nem a campanha é criada. Lista vazia (no `/invites`) ou com usuário repetido dá 400 pela validação.
3. **Na criação**, campanha, posição na grade e convites são gravados na mesma transação (`ITransacao`), e os avisos nº 9 só saem depois.
4. **Um pendente por (campanha, usuário):** índice único parcial `ix_campaign_invites_pendente` (`status = 'pendente'`). Recusado ou cancelado vira histórico, e o mestre pode convidar de novo na hora (um convite novo).
5. **Listas:** só pendentes, os mais novos primeiro. A da campanha é só do mestre (jogador 403, quem não participa 404); ela alimenta o "Convite enviado" do modal, e o "Já na mesa" vem de `/members`.
6. **Card** (`GET /api/campaign-invites/{id}`, exige login; sem token dá 401 e o front mostra "Entre para responder"):
   - **Convidado e mestre:** `{id, status, podeResponder, campanha: {id, nome, descricao, sistema, capaUrl, bannerUrl}, mestre: {userId, nome, nick, avatarUrl}, naMesa: [mestre + jogadores], convidadoPor: {nome, nick}, para: {nick}, criadoEm}`.
   - **Qualquer outro usuário logado** (inclusive um jogador da campanha): só `{id, podeResponder: false, para: {nick}}`, sem nenhum dado da campanha ("Este convite é para outra pessoa").
   - `podeResponder` é true só para o convidado com o convite pendente. `status`: `pendente`, `aceito`, `recusado` ou `cancelado`. Id inexistente dá 404.
7. **Aceitar e recusar:** só o convidado (para qualquer outro, o convite "não existe": 404) e só pendente (409).
   - Aceitar põe o usuário em `campaign_members`, com a campanha **no início** da grade dele, tudo numa transação. O mestre recebe o nº 10, e os **outros** jogadores, o nº 13 (nem quem entrou, nem o mestre).
   - Recusar só muda o status; o mestre recebe o nº 11.
8. **Cancelar:** só o mestre. O convidado e os jogadores recebem 403; quem não participa, 404; não pendente, 409. O convidado recebe o nº 12.
9. **Amizade desfeita** (`DELETE /api/friends/{userId}`, de qualquer lado): os convites pendentes entre os dois (campanhas em que um é mestre e o outro é o convidado, nos dois sentidos) são cancelados na mesma transação da remoção, e cada convidado recebe o nº 12. Convites aceitos não mudam. A Taberna chama Campanhas pela porta `IConvitesEntreAmigos`.
10. **Campanha excluída:** quem tinha convite pendente recebe o nº 12 (lido antes de apagar; os convites vão junto pelo cascade). Os jogadores continuam recebendo o nº 18.

11. **Corridas** (no padrão da Taberna, pelo 23505 do Postgres):
    - Dois convites simultâneos para a mesma pessoa batem no índice `ix_campaign_invites_pendente` e dão **409** com `ja_convidado` na extensão `convidados`. Vem só quem colidiu; se o banco não mostrar ninguém, vem a lista toda.
    - Dois aceites simultâneos do mesmo convite batem na PK `pk_campaign_members` e dão **409** "não está mais pendente". A transação desfaz o status, o jogador e a grade, e nenhum aviso sai.

Modelo: `campaign_invites` (`id`, `campaign_id` com cascade, `invited_by`, `user_id`, `status`, `created_at`, `responded_at`), migration `AddConvitesDeCampanha`. Regras no `ConviteService`, com o `AcessoACampanha` para mestre e membro; o card é montado por `CardDoConvite`.

Nos testes, `ConvitesDeTeste` cria um mestre com amigos (`CriarMestreComAmigosAsync`), grava convites direto no banco com status e data controláveis (`ConvidarNoBancoAsync`) e chama os endpoints (`ConvidarAsync`, `AceitarAsync`, `RecusarAsync`, `CancelarAsync`).

Nos testes, `CampanhasDeTeste` cria campanhas pela API (`CriarCampanhaAsync`, `CriarUsuarioComCampanhasAsync`), põe um jogador direto no banco com a campanha no início da grade dele (`AdicionarJogadorAsync`, como o aceite do convite vai fazer) e lê a grade e as linhas da campanha no banco (`OrdemNoBancoAsync`, `LinhasDaCampanhaAsync`).

## Notificações

Um serviço central, o `Notificador` (`INotificador.NotificarAsync(userId, aviso, emailDestino?)`), decide os canais de cada tipo, grava o sininho e manda o e-mail. Contas, Taberna e Campanhas só chamam esse serviço; ninguém mais manda e-mail direto.

- Cada tipo é um record (`Aviso`): `BoasVindas`, `RedefinicaoDeSenha(link)`, `SenhaAlterada`, `EmailAlterado(emailNovo)`, `ConfirmacaoDeEmail(link)`, `AmizadeRecebida(...)`, `AmizadeAceita(...)`, `AmizadeRecusada(...)`, `ConviteRecebido(...)`, `ConviteAceito(...)`, `ConviteRecusado(...)`, `ConviteCancelado(...)`, `JogadorEntrou(...)`, `CampanhaEditada(...)` e `CampanhaExcluida(...)`.
- O `emailDestino` é para quando o e-mail não vai para o endereço atual do usuário: o antigo no nº 4 e o novo na confirmação.

**Matriz** (`MatrizDeNotificacoes`, o único lugar com os canais):

| Tipo | Nº | Sininho | E-mail | Para quem | `dados` |
| --- | --- | --- | --- | --- | --- |
| `boas_vindas` | 1 | sim | fila | o próprio usuário | `{}` |
| `redefinicao_senha` | 2 | — | **direto** | o próprio usuário | — |
| `senha_alterada` | 3 | — | fila | o próprio usuário | — |
| `email_alterado` | 4 | — | fila | o e-mail **antigo** | — |
| `confirmacao_email` | — | — | **direto** | o e-mail novo | — |
| `amizade_recebida` | 5 | sim | fila | destinatário do pedido | `{pedidoId, deUserId, deNome, deNick}` |
| `amizade_aceita` | 6 | sim | — | quem enviou o pedido | `{userId, nome, nick}` |
| `amizade_recusada` | 7 | sim | — | quem enviou o pedido | `{userId, nome, nick}` |
| `convite_recebido` | 9 | sim | fila | o convidado | `{conviteId, campanhaId, campanhaNome, mestreNome, mestreNick}` |
| `convite_aceito` | 10 | sim | — | o mestre | `{conviteId, campanhaId, campanhaNome, userId, nome, nick}` |
| `convite_recusado` | 11 | sim | — | o mestre | `{conviteId, campanhaId, campanhaNome, userId, nome, nick}` |
| `convite_cancelado` | 12 | sim | — | o convidado (cancelado pelo mestre, amizade desfeita ou campanha excluída) | `{campanhaId, campanhaNome}` |
| `jogador_entrou` | 13 | sim | fila | os outros jogadores | `{campanhaId, campanhaNome, userId, nome, nick}` |
| `campanha_editada` | 17 | sim | — | os jogadores (não o mestre) | `{campanhaId, campanhaNome}` |
| `campanha_excluida` | 18 | sim | fila | os jogadores (não o mestre) | `{campanhaNome, mestreNome}` |

- O amigo removido (nº 8) não tem tipo: ninguém é avisado.
- O front monta o texto: a API guarda e devolve só `tipo` e `dados`. Os `dados` levam os ids e os nomes **de quando a notificação foi criada** (não são atualizados se a pessoa mudar o nome ou o nick).
- Tabela `notifications`: `id`, `user_id` (FK para `users`, cascade), `tipo`, `dados` (JSONB), `lida_em`, `created_at`. Índice `(user_id, created_at desc, id)`.

**Endpoints** (todos com Bearer, 401 sem token; cada usuário só vê as dele, e a de outro usuário dá 404, sem revelar que existe):

| Endpoint | Saída |
| --- | --- |
| `GET /api/notifications?naoLidas=&cursor=&limite=` | 200 `{itens: [{id, tipo, dados, lida, criadoEm}], proximoCursor}` · 400 cursor ou limite inválido |
| `GET /api/notifications/unread-count` | 200 `{total}` |
| `POST /api/notifications/{id}/read` | 204 (idempotente) · 404 |
| `POST /api/notifications/read-all` | 204 |
| `DELETE /api/notifications/{id}` | 204 · 404 |

**Paginação** (`GET /api/notifications`)

1. As mais novas primeiro: `created_at` decrescente, com desempate pelo `id`, na mesma ordem do índice.
2. `limite`: padrão 20, máximo 50 (acima de 50 vira 50). Menor que 1 ou não numérico dá 400.
3. O cursor é opaco: `Base64Url("{ticks do created_at}:{id}")` do último item da página (`CursorDeNotificacoes`). A próxima página vem com `?cursor=<proximoCursor>`.
4. É paginação por chave (o que vem depois de `created_at`/`id`), então notificações novas chegando no topo não fazem a página seguinte repetir nem pular itens.
5. `proximoCursor` vem null na última página. Cursor que não decodifica dá 400.
6. `naoLidas=true` filtra na consulta (`lida_em` nulo).

Nos testes, `NotificacoesDeTeste` grava notificações direto no banco, com o `created_at` controlável (`CriarNotificacaoAsync`), e lê as do usuário (`NotificacoesNoBancoAsync`).

## Tempo real

SignalR (sem backplane: a API roda numa VM só). O hub `NotificationHub` fica em **`/hubs/notifications`**, com `[Authorize]`. O cliente não chama nenhum método: o hub só empurra os eventos do sininho.

**Autenticação**

- É o mesmo JWT da API. No navegador, o WebSocket não manda header, então o token vai na query: `/hubs/notifications?access_token=<token>` (no `@microsoft/signalr`, use o `accessTokenFactory`).
- A query `access_token` **só vale em caminhos que começam com `/hubs`** (`JwtBearerEvents.OnMessageReceived`). Num endpoint REST ela é ignorada, e sem header dá 401.
- Token ausente, inválido ou expirado: a conexão é recusada (401 no negotiate).
- O token não vai para o log. O único log que imprime a URL com a query é o de início e fim de requisição (`Microsoft.AspNetCore.Hosting.Diagnostics`). O `Program` trava esse log em `Warning`, mesmo que a configuração suba `Microsoft.AspNetCore` para Information.
- **O front precisa reconectar com um token novo depois do refresh:** o access token vale 15 min, e a conexão aberta não troca de token sozinha.

**Destino:** `Clients.User(userId)`, com o `UsuarioPeloSub` (`IUserIdProvider`) lendo a claim `sub`. Todas as conexões do usuário recebem: várias abas e vários aparelhos.

**Eventos** (payload em camelCase; saem só **depois** de gravar no banco):

| Evento | Quando | Payload |
| --- | --- | --- |
| `NotificationCreated` | o serviço central grava uma notificação no sininho | o item no mesmo formato do `GET /api/notifications`: `{id, tipo, dados, lida, criadoEm}` |
| `UnreadCount` | depois de criar, ler, ler todas ou apagar | `{total}` (recontado no banco) |
| `NotificationRead` | `POST /api/notifications/{id}/read` que mudou o estado | `{id}` |
| `NotificationsReadAll` | `POST /api/notifications/read-all` | `{}` |
| `NotificationDeleted` | `DELETE /api/notifications/{id}` | `{id}` |

- Ler de novo uma notificação já lida não manda nada: nem `NotificationRead` nem `UnreadCount`.
- Ação na notificação de outro usuário (404) não manda nada para ninguém.
- A emissão fica num lugar só, o `EventosDoSininho` (Application), usado pelo `Notificador` e pelo `NotificacoesService`. A porta é a `INotificacoesTempoReal`, implementada na Api por `HubNotificacoesTempoReal` (`IHubContext<NotificationHub>`).
- **Falha ao empurrar não quebra a requisição:** o `EventosDoSininho` loga um Warning (só o nome do evento, sem dados pessoais) e segue. O que foi gravado aparece no `GET`.
- CORS: o hub usa a mesma policy `Front`, com credenciais.

Nos testes, `ConexaoNoHub` abre a conexão pelo TestServer, com transporte LongPolling e o token na query, sem header. Ele guarda os eventos recebidos e tem `EsperarAsync`/`NaoRecebeAsync`, com timeout curto. `LogsCapturados` captura as linhas de log da API.

## Jobs (Hangfire)

Hangfire (`Hangfire.AspNetCore` + `Hangfire.PostgreSql`) com storage no mesmo Postgres, no schema `hangfire`, que a própria lib cria. Não há migration do EF para essas tabelas, e o Respawn dos testes não mexe nesse schema. O Hangfire puxa o `Newtonsoft.Json` 11.0.1, que tem vulnerabilidade conhecida; o `RPGuild.Infrastructure` fixa a 13.0.4.

- **Fila de e-mail** (`IFilaDeEmail`, implementada por `HangfireFilaDeEmail`): a requisição só enfileira e o job `EnvioDeEmailJob` envia. Se o envio falhar, o job loga o assunto e o destinatário mascarado (`MascaraDeEmail`, como no envio direto, sem o corpo) e deixa a exceção subir para o Hangfire tentar de novo (tentativas automáticas padrão). A mensagem inteira fica gravada nos argumentos do job, com o destinatário em claro, e aparece no painel.
- **Exceções com token:** `redefinicao_senha` e `confirmacao_email` **não passam pela fila**, porque o token não pode ficar nas tabelas do Hangfire. Eles saem na hora pelo `EnvioDeEmail`; se falhar, loga mascarado e não quebra (veja [E-mail](#e-mail)).
- **Limpeza:** o job recorrente diário `limpeza-de-notificacoes` (`LimpezaDeNotificacoes`) apaga as notificações com `created_at` de mais de **90 dias**, lidas ou não.
- **Painel** em `/hangfire`, **só em Development** (e, pelo padrão do Hangfire, só para requisições locais). Em produção o painel não é mapeado.
- **Liga/desliga:** o Hangfire sobe em todos os ambientes, menos no `Testing`. A chave `Hangfire:Habilitado` força um ou outro.
- **Testes:** no `Testing`, a `RPGuildApiFactory` usa uma `FakeFilaDeEmail`, que "executa o job" na hora para o `FakeEmailSender` e engole a falha, como um job faria. Com `usarHangfire: true`, a factory sobe o Hangfire de verdade no container; os testes da fila (`FilaHangfireTests`) usam isso para conferir os jobs gravados.

## E-mail

E-mails em texto simples, em PT-BR, enviados por SMTP com MailKit (`SmtpEmailSender`) pelo Gmail, em dev e em produção. Os textos ficam todos em `src/RPGuild.Application/Notificacoes/EmailTemplates.cs`. Nesta fase saem nove: boas-vindas (depois do register), redefinição de senha (no forgot-password), confirmação do novo e-mail (no `PUT /api/me/email`, para o endereço novo), e-mail alterado (no `confirm`, para o endereço **antigo**), senha alterada (no `PUT /api/me/password`), pedido de amizade (no `POST /api/friends/requests`, para o destinatário), convite de campanha (com o link `{Front:BaseUrl}/convites/{conviteId}`), jogador que entrou (com o link `{Front:BaseUrl}/campanhas/{campanhaId}`) e campanha excluída (no `DELETE /api/campaigns/{id}`, para os jogadores).

**Senha de app do Gmail**

1. Na conta Google que vai enviar, ative a verificação em duas etapas (myaccount.google.com/security).
2. Abra myaccount.google.com/apppasswords, crie uma senha de app (por exemplo "RPGuild") e copie os 16 caracteres.
3. Use essa senha em `Email__Senha`. A senha normal da conta não funciona por SMTP.

**Configuração**

| Chave | Valor | Onde |
| --- | --- | --- |
| `Email:Host` · `Email:Port` · `Email:UseStartTls` | `smtp.gmail.com` · `587` · `true` | `appsettings.json` |
| `Email:RemetenteNome` | `RPGuild` | `appsettings.json` |
| `Email:Usuario` | o endereço Gmail que envia | `.env` (dev) / `Email__Usuario` (produção) |
| `Email:Senha` | a senha de app | `.env` (dev) / `Email__Senha` (produção) |
| `Email:RemetenteEmail` | o endereço do remetente (o mesmo Gmail) | `.env` (dev) / `Email__RemetenteEmail` (produção) |

Em dev, as credenciais ficam no `.env` da raiz do monorepo, que está no `.gitignore`. Copie do `.env.example` e preencha `Email__Usuario`, `Email__Senha` e `Email__RemetenteEmail`. A API carrega o `.env` com o DotNetEnv **só em Development**. As variáveis de ambiente valem mais que o `.env`: ele só preenche o que o ambiente não definiu e não altera as variáveis do processo. Em produção, tudo vem de variáveis de ambiente. Nenhuma credencial vai para arquivo versionado.

**Comportamento**

- Sem `Email:Host`, `Email:Port`, `Email:Usuario`, `Email:Senha` ou `Email:RemetenteEmail`, a API **não sobe**, e a mensagem diz quais chaves faltam. Isso também vale para os comandos `dotnet ef`, que sobem a API em Development: o `.env` precisa estar preenchido.
- Os e-mails saem pelo [serviço central de notificações](#notificações): a maioria pela fila do [Hangfire](#jobs-hangfire), com novas tentativas, e os dois com token direto.
- Se o envio direto falhar (SMTP fora, senha errada...), a requisição **segue normalmente** (204 no forgot-password e no `PUT /api/me/email`). A falha vai para o log (`ILogger`) com o assunto e o destinatário mascarado (`ma***@gmail.com`; com 2 caracteres ou menos antes do `@`, `***@gmail.com`), sem o corpo, o token ou a senha. Isso fica em `EnvioDeEmail`.
- Nos testes, a `RPGuildApiFactory` troca o SMTP por um `FakeEmailSender`, que guarda as mensagens em memória (`factory.Emails.UltimaPara(email)`), e a fila por uma `FakeFilaDeEmail` (`factory.Fila`). O `SmtpEmailSender` não tem teste automatizado; ele é validado manualmente com o Gmail.

## Testes

- `RPGuild.UnitTests`: regras de Domain e Application, organizadas por módulo (`Contas/`, `Fichas/`, ...). `Arquitetura/` verifica a regra de dependência entre camadas (NetArchTest).
- `RPGuild.IntegrationTests`: endpoints, banco e EF Core. `Infra/` tem a `PostgresFixture` (container `postgres:17-alpine` reaproveitado entre execuções em dev, compartilhado pela coleção `Integration`, com as migrations já aplicadas e os dados limpos pelo Respawn) e a `RPGuildApiFactory` (ambiente `Testing`, `ConnectionStrings:Default` apontando para o container).
- Bibliotecas: xUnit, Shouldly, NSubstitute, NetArchTest, Testcontainers.

```bash
dotnet test RPGuild.sln                                       # tudo (precisa do Docker rodando)
dotnet test RPGuild.sln --filter FullyQualifiedName~UnitTests  # só os unitários, sem Docker
```

Convenções de TDD em [CONTRIBUTING.md](../CONTRIBUTING.md#tdd).
