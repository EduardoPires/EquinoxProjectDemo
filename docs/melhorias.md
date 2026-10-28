# Melhorias: triagem

Revisão do projeto inteiro feita em 27/10/2026, só por leitura do código (nada foi executado). Os itens estão numerados do mais fácil para o mais arriscado, e a numeração original foi mantida na triagem. A migração de .NET ficou fora, porque é outra tarefa.

A coluna **Padrão** indica quando o item mexe em convenção do projeto (CQRS, bus, Identity, claims).

## Resumo

| Situação | Itens |
|---|---|
| Aceitas e aplicadas | 1, 2, 3 |
| **Próxima leva de correções** | **13 (prioridade máxima)**, depois 4, 5, 6 |
| Adiadas sem prazo | 7, 8, 9 |
| Recusadas | 10, 11, 12 |

## Aceitas

| # | Onde | Problema | O que fazer | Risco | Ganho | Padrão |
|---|---|---|---|---|---|---|
| 1 | `src/Equinox.UI.Web/Views/Customer/Index.cshtml:128` | O modal de histórico monta o HTML juntando nome e e-mail como texto e joga na página com `.html()`. Um cliente chamado `<img src=x onerror=...>` executa script para qualquer visitante, porque a lista e o histórico são anônimos. É um XSS gravado no banco. | **Aplicada em 27/10/2026.** Montar as células com `$('<td>').text(...)` ou escapar cada campo. | Muito baixo, só mexe na view | Alto: fecha o XSS | — |
| 2 | `src/Equinox.Application/EventSourcedNormalizers/CustomerHistory.cs:11` | `HistoryData` é estático e recriado a cada chamada (`:15`). Dois pedidos de histórico ao mesmo tempo misturam ou apagam a lista um do outro. | **Aplicada em 27/10/2026.** Usar uma lista local e passá-la ao desserializador. | Baixo | Médio: corrige um bug de concorrência | — |
| 3 | `src/Equinox.UI.Web/Areas/Identity/Pages/Account/Login.cshtml.cs:84` | Com `lockoutOnFailure: false`, o login do site aceita tentativas de senha sem limite. A API já usa `true` (`src/Equinox.Services.Api/Controllers/AccountController.cs:61`). | **Aplicada em 27/10/2026.** Passar `true`. O usuário de seed tem `LockoutEnabled = false` e continua sem bloqueio. | Baixo: bloqueia por 5 min depois de 5 erros | Médio: segura ataque de força bruta | Identity |

## Próxima leva de correções

A migração para o .NET 10 foi concluída em 27/10/2026 (`docs/migracao-net10.md`). Por isso os itens 4, 5 e 6, adiados até ela, entram nesta leva. O **item 13 tem prioridade máxima** e é o primeiro a ser feito.

### 13. A API não funciona com banco em Development (prioridade máxima)

Diferente do resto da triagem, este item foi **confirmado rodando a API** (`dotnet watch --no-launch-profile`, já em .NET 10) em 27/10/2026. Hoje, em Development, só o Swagger e as respostas 401 funcionam. A causa vem de antes da migração e não tem relação com o .NET 10. Os dois erros:

| Chamada | Resposta | Erro no log | Causa |
|---|---|---|---|
| `GET /customer` (e qualquer rota de clientes) | 500 | `Cannot open database "Equinox" requested by the login` | Clientes e eventos usam sempre SQL Server (`src/Equinox.Services.Api/Configurations/DatabaseConfig.cs:12-16`), com `(localdb)` e `Database=Equinox` (`src/Equinox.Services.Api/appsettings.Development.json:3`). Ninguém cria esse banco: a API não roda migrations, e o site, em Development, usa SQLite. No LocalDB desta máquina só existem os bancos de sistema e um `DevXpert`. |
| `POST /account/enter` (login) | 500 | `Connection string keyword 'server' is not supported` | Em Development, o Identity usa SQLite (`src/Equinox.Infra.CrossCutting.Identity/Configuration/AspNetIdentityConfig.cs:42-46`), mas recebe a mesma connection string de SQL Server da API. É o ponto 1 de "O que eu ainda não sei" em `docs/arquitetura.md`, agora confirmado. |

**Como resolver (recomendado):** na API, usar SQLite em Development, como o site já faz.

1. Em `src/Equinox.Services.Api/Configurations/DatabaseConfig.cs`, repetir o padrão de `src/Equinox.UI.Web/Configurations/DatabaseConfig.cs:12-22`: `UseSqlite` para `EquinoxContext` e `EventStoreSqlContext` quando `builder.Environment.IsDevelopment()`, e `UseSqlServer` nos outros ambientes.
2. Em `src/Equinox.Services.Api/appsettings.Development.json`, trocar a `DefaultConnection` por `Data Source=../Equinox.UI.Web/EquinoxProject.db`. O caminho é relativo à pasta do projeto, que é onde o `dotnet watch` roda. Assim os três contextos (clientes, eventos e Identity) usam o mesmo arquivo do site, que já vem migrado e com o seed, e o site e a API mostram os mesmos dados.
3. Atualizar o `CLAUDE.md`, que hoje diz que a API "usa SQL Server `(localdb)`".
4. Testar: subir o site uma vez (ele cria o banco), subir a API, fazer login em `POST /account/enter` com o usuário de seed e chamar `GET /customer` e `POST /customer` com o token.

Não precisa de pacote NuGet novo: o `Microsoft.EntityFrameworkCore.Sqlite` já chega à API pelo projeto Identity (`src/Equinox.Infra.CrossCutting.Identity/Equinox.Infra.CrossCutting.Identity.csproj:16`). Staging e Production não mudam e continuam em SQL Server.

- **Risco:** baixo, só mexe na configuração de Development da API. Depende de o site ter subido antes pelo menos uma vez.
- **Ganho:** alto. A API passa a funcionar localmente, inclusive o fluxo com JWT, que hoje não dá para testar.

**Por que não as outras saídas:**
- **Criar o banco no LocalDB com as migrations:** não funciona. As migrations do repositório são de SQLite (colunas `TEXT`/`INTEGER` e `Sqlite:Autoincrement`, em `src/Equinox.Infra.Data/Migrations/` e `src/Equinox.Infra.CrossCutting.Identity/Migrations/`).
- **Criar o banco com `sql/GenerateDataBase.sql`:** o script é de 2016 e está em UTF-16. As tabelas batem com o modelo atual pelo nome, mas o script não foi testado. Além disso, só resolve o primeiro erro: o login continuaria quebrado, porque o Identity usa SQLite em Development. Para consertar isso, seria preciso mudar o `AddIdentityDbContext`, que é compartilhado com o site (que precisa de SQLite).

## Adiadas para depois da migração de .NET (entram na próxima leva, depois do 13)

| # | Onde | Problema | O que fazer | Risco | Ganho | Padrão |
|---|---|---|---|---|---|---|
| 4 | `src/Equinox.Domain/Commands/Validations/CustomerValidation.cs:23` | O e-mail não tem limite de tamanho em lugar nenhum, mas a coluna é `varchar(100)` (`src/Equinox.Infra.Data/Mappings/CustomerMap.cs:19-22`). O nome é validado até 150 caracteres (`:12`), e a ViewModel e a coluna só aceitam 100. No SQL Server, um e-mail com mais de 100 caracteres dá erro 500. | Colocar `MaximumLength(100)` em nome e e-mail no domínio e `[MaxLength(100)]` no e-mail da ViewModel. Traduzir as mensagens ao mexer, como pede o CLAUDE.md. | Baixo | Médio: uma regra só, e erro amigável em vez de 500 | Validação de comando |
| 5 | `src/Equinox.Domain/Commands/CustomerCommandHandler.cs:43` | O Update não confere se o cliente existe. Com um Id inexistente (PUT na API), o `CustomerUpdatedEvent` é gravado e depois o `SaveChanges` lança `DbUpdateConcurrencyException`: dá 500 e deixa um evento órfão no histórico. Dedução do código, ainda não confirmada com um PUT real. | Chamar `GetById` antes e devolver o erro "cliente não existe", como o Remove já faz (`:66-72`). | Baixo | Médio | CQRS |
| 6 | `src/Equinox.Services.Api/appsettings.Development.json:15` | A chave que assina os JWT está no Git e é a mesma em Staging (`src/Equinox.Services.Api/appsettings.Staging.json:15`). Se a Staging subir com esse arquivo, quem lê o repositório consegue criar um token com `Customers: Write,Remove`. | Tirar a chave de Staging e passá-la por variável de ambiente ou user-secrets. Gerar uma chave nova para Staging. | Baixo a médio: quem sobe Staging precisa configurar a variável | Alto | — |

## Adiadas sem prazo

| # | Onde | Problema | O que fazer | Risco | Ganho | Padrão |
|---|---|---|---|---|---|---|
| 7 | `src/Equinox.Services.Api/Program.cs:18` | O CORS (acesso de outros sites) aceita qualquer origem, método e header. O Swagger fica ligado em todos os ambientes (`src/Equinox.Services.Api/Configurations/SwaggerConfig.cs:84-92`). | Ligar o Swagger só em Development e criar uma política de CORS nomeada, com as origens no appsettings. | Médio: front-ends de outra origem param até serem configurados | Médio: menos pontos de ataque | — |
| 8 | `src/Equinox.Infra.CrossCutting.Identity/API/JwtBuilder.cs:38` | O `.Result` em três chamadas assíncronas do `UserManager` (`:38`, `:61`, `:69`) prende uma thread a cada login. | Tornar o builder assíncrono ou buscar o usuário e as claims antes, no controller. | Médio: muda a interface do builder (só o `AccountController` usa) | Médio, com carga alta | Identity |
| 9 | `src/Equinox.Services.Api/Program.cs:28` | O `MapIdentityApi` publica um segundo conjunto de endpoints anônimos (`/register`, `/login`, `/forgotPassword`, `/manage/*`), escondidos do Swagger e com outro formato de token, duplicando `/account/new` e `/account/enter`. É o ponto 2 de "O que eu ainda não sei" em `docs/arquitetura.md`. | Escolher um caminho: tirar o `MapIdentityApi` ou adotá-lo e aposentar o `AccountController`. | Médio: algum cliente pode estar usando | Médio: uma única porta de login | Identity |

## Recusadas

Motivo da recusa não registrado.

| # | Onde | Problema | O que fazer | Risco | Ganho | Padrão |
|---|---|---|---|---|---|---|
| 10 | `src/Equinox.UI.Web/Areas/Identity/Pages/Account/Register.cshtml.cs:75` | Todo cadastro no site recebe `Customers: Write`, então qualquer visitante cria e edita clientes. É por aí que o item 1 vira ataque. | Cadastrar sem a claim e liberar `Write` por um admin ou pelo seed. É decisão de produto. | Médio: muda o fluxo da demonstração | Alto em segurança | Claims |
| 11 | `src/Equinox.Infra.Data/Mappings/CustomerMap.cs:19` | O e-mail único depende só do `GetByEmail` no handler. Dois cadastros ao mesmo tempo criam duplicatas, e no SQLite `A@x.com` e `a@x.com` contam como e-mails diferentes. | Criar um índice único em `Email`, normalizado, com migration, e tratar a violação como erro de validação. | Médio a alto: migration nos dois bancos, que falha se já houver duplicatas. Hoje as migrations são só `*_SQLite` (ponto 3 de "O que eu ainda não sei") | Médio | — |
| 12 | `src/Equinox.Infra.Data/Context/EquinoxContext.cs:49` | Os eventos vão para o event store com um `SaveChanges` próprio (`src/Equinox.Infra.Data/Repository/EventSourcing/EventStoreSQLRepository.cs:28`) antes de o cliente ser salvo (`:53`), em outra transação. Se o save falhar, o histórico registra algo que não aconteceu (o item 5 é um caso). Além disso, `Task.WhenAll` (`:74-79`) publica os eventos em paralelo. | Gravar o `StoredEvent` e o `Customer` na mesma transação (transação compartilhada ou outbox) e publicar um evento por vez, em sequência. | Alto: mexe no núcleo do fluxo comando → evento, e o comentário no código diz que a escolha foi de propósito | Alto: consistência dos dados | Bus / event store |

## Fora da triagem

Pontos vistos na revisão que não entraram na lista dos 12:

- Os Dockerfiles usavam imagens `6.0` (`src/Equinox.UI.Web/Dockerfile:3,8`). Passaram para `10.0` na migração de 27/10/2026.
- Não há testes de unidade, só de arquitetura. Um projeto de testes com mocks precisaria de pacote NuGet novo, o que pede autorização.
- Excluir um cliente que não existe quebra a view (`src/Equinox.UI.Web/Controllers/CustomerController.cs:101-102`).
- `GetUserEmail` procura a claim `sub` antes do e-mail (`src/Equinox.Infra.CrossCutting.Identity/Extensions/ClaimsPrincipalExtensions.cs:29`). Hoje funciona por acaso, porque o JwtBearer renomeia o `sub`.
