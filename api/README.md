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

1. O front chama `POST /api/uploads/sign` com o `tipo` e o `contentType` do arquivo. Por enquanto só `tipo = "avatar"`; capa, banner e retrato respondem 400 ("ainda não suportado").
2. `contentType` permitido: `image/jpeg`, `image/png`, `image/webp`, `image/gif`. A API escolhe o path, `avatars/{userId}/{guid}.{ext}`, com a extensão vinda do `contentType` (`jpg`, `png`, `webp`, `gif`).
3. A resposta traz a `uploadUrl` (assinada, com `?token=`; o Supabase a deixa valer por 2 horas), o `path` e a `publicUrl`.
4. O front sobe o arquivo com `PUT` na `uploadUrl`, corpo binário e `Content-Type` do arquivo. Não precisa de chave nem de Bearer: o token da URL autoriza só aquele path.
5. Com o upload feito, o front manda o `path` em `PUT /api/me` (`avatarPath`). O avatar antigo é apagado do bucket nessa hora.

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
4. Pedido pendente **dela para mim** dá 409 com "Essa pessoa já te enviou um pedido. Aceite na sua lista." Não há aceite automático.
5. Um pedido **recusado** pode ser reenviado na hora, por qualquer um dos dois. A linha recusada vira um pedido pendente novo, com o mesmo id, o novo sentido e o `created_at` do reenvio.
6. O destinatário recebe o e-mail do aviso nº 5, com o nome e o @nick de quem pediu e o link `{Front:BaseUrl}/taberna`. Se o envio falhar, segue a regra de [E-mail](#e-mail): o 201 sai do mesmo jeito.

**Responder e desfazer**

- Só o destinatário aceita ou recusa, e só quem enviou cancela. Para qualquer outra pessoa, o pedido "não existe" (404).
- Só pedido pendente pode ser respondido ou cancelado (409).
- Aceitar e recusar preenchem o `responded_at`. Cancelar apaga a linha.
- Os avisos nº 6 e 7 (aceito e recusado) são só sininho e entram no módulo de Notificações.
- Remover amigo apaga a linha e quem foi removido não recebe nada (aviso nº 8 = N). Qualquer um dos dois pode remover, e depois os dois podem pedir de novo.

**Listas**

- `GET /api/friends`: as amizades mais recentes primeiro (`amigosDesde` = `responded_at` do aceite, decrescente). `campanhasEmComum` vale sempre 0 por enquanto (`TODO(campanhas)` no `AmizadeService`).
- `GET /api/friends/requests`: só os pendentes, os mais novos primeiro, separados em recebidos e enviados.
- **Nenhuma das duas listas é paginada.**
- Nenhuma resposta da Taberna tem e-mail. Mesmo num pedido feito por e-mail, o enviado mostra o @nick.
- O `avatarUrl` é a URL pública do avatar no storage (o mesmo `IStorageService.ObterUrlPublica` do perfil), ou null.

Nos testes, `TabernaDeTeste` cria vários usuários logados (`CriarUsuariosLogadosAsync`, `CriarDoisUsuariosLogadosAsync`) e deixa dois amigos direto no banco (`TornarAmigosAsync`, com o "amigos desde" controlável). `ClienteDaTaberna` faz as chamadas e monta as URLs.

## E-mail

E-mails em texto simples, em PT-BR, enviados por SMTP com MailKit (`SmtpEmailSender`) pelo Gmail, em dev e em produção. Os textos ficam todos em `src/RPGuild.Application/Notificacoes/EmailTemplates.cs`. Nesta fase saem seis: boas-vindas (depois do register), redefinição de senha (no forgot-password), confirmação do novo e-mail (no `PUT /api/me/email`, para o endereço novo), e-mail alterado (no `confirm`, para o endereço **antigo**), senha alterada (no `PUT /api/me/password`) e pedido de amizade (no `POST /api/friends/requests`, para o destinatário).

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
- Se o envio falhar (SMTP fora, senha errada...), a requisição **segue normalmente** (201 no register, 204 no forgot-password, e assim por diante). A falha vai para o log (`ILogger`) com o assunto e o destinatário mascarado (`ma***@gmail.com`; com 2 caracteres ou menos antes do `@`, `***@gmail.com`), sem o corpo, o token ou a senha. Isso fica em `EnvioDeEmail`, o ponto único de envio dos casos de uso. Ainda não há reenvio; está anotado `TODO(hangfire)` para mover o envio para uma fila.
- Nos testes, a `RPGuildApiFactory` troca o SMTP por um `FakeEmailSender`, que guarda as mensagens em memória (`factory.Emails.UltimaPara(email)`). O `SmtpEmailSender` não tem teste automatizado; ele é validado manualmente com o Gmail.

## Testes

- `RPGuild.UnitTests`: regras de Domain e Application, organizadas por módulo (`Contas/`, `Fichas/`, ...). `Arquitetura/` verifica a regra de dependência entre camadas (NetArchTest).
- `RPGuild.IntegrationTests`: endpoints, banco e EF Core. `Infra/` tem a `PostgresFixture` (container `postgres:17-alpine` reaproveitado entre execuções em dev, compartilhado pela coleção `Integration`, com as migrations já aplicadas e os dados limpos pelo Respawn) e a `RPGuildApiFactory` (ambiente `Testing`, `ConnectionStrings:Default` apontando para o container).
- Bibliotecas: xUnit, Shouldly, NSubstitute, NetArchTest, Testcontainers.

```bash
dotnet test RPGuild.sln                                       # tudo (precisa do Docker rodando)
dotnet test RPGuild.sln --filter FullyQualifiedName~UnitTests  # só os unitários, sem Docker
```

Convenções de TDD em [CONTRIBUTING.md](../CONTRIBUTING.md#tdd).
