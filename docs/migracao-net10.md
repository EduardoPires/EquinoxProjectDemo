# Migração do Equinox para o .NET 10

Executada em 27/10/2026, na branch `net10` (criada a partir da `estudo/pagina-sobre`).

O escopo foi **só a migração**: trocar o alvo de `net9.0` para `net10.0` e alinhar o que era obrigatório. Nenhuma biblioteca foi trocada e nada foi melhorado. Nenhum pacote mudou de licença: todos os que mudaram de versão são MIT antes e depois, conferido no nuspec de cada um no NuGet.

## Fontes oficiais

- Guia 9 → 10: https://learn.microsoft.com/aspnet/core/migration/90-to-100. Pede três passos: `global.json` (se existir), TFM `net10.0` e pacotes `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*` e `Microsoft.Extensions.*` em 10.0.x.
- Breaking changes do .NET 10: https://learn.microsoft.com/dotnet/core/compatibility/10
- Breaking changes do ASP.NET Core 10: https://learn.microsoft.com/aspnet/core/breaking-changes/10/overview
- Breaking changes do EF Core 10: https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/breaking-changes
- NU1510: https://learn.microsoft.com/dotnet/core/compatibility/sdk/10.0/nu1510-pruned-references

## O que mudou

### Alvo

`<TargetFramework>net9.0</TargetFramework>` → `net10.0` nos 10 `.csproj` (`src/*/*.csproj` e `tests/Equinox.Tests.Architecture`). O repositório não tem `global.json`, e o guia só manda atualizá-lo quando ele já existe, então nenhum foi criado.

### Pacotes

| Pacote | De | Para | Por quê |
|---|---|---|---|
| `Microsoft.AspNetCore.*` (JwtBearer, Identity.EntityFrameworkCore, Identity.UI, Authentication.Facebook/Google, Diagnostics.EntityFrameworkCore, Mvc.Razor.RuntimeCompilation) | 9.0.3 | 10.0.14 | guia oficial |
| `Microsoft.EntityFrameworkCore.*` (base, Sqlite, SqlServer, Design, Tools) | 9.0.3 | 10.0.14 | guia oficial |
| `Azure.Identity` (Identity e Infra.Data) | 1.13.2 | 1.17.1 | O EF SqlServer 10.0.14 depende do Microsoft.Data.SqlClient 6.1.7, que exige Azure.Identity ≥ 1.17.1. Com a 1.13.2, o restore dá NU1605 (downgrade), que é erro. |
| `System.Security.Cryptography.Pkcs` e `.Xml` (Infra.Data) | 9.0.3 | 10.0.14 | Com a 9.0.3, o restore dá NU1605: o SqlClient 6.1.7 exige Pkcs ≥ 9.0.11. Não foram marcados com NU1510, então ficaram e subiram de versão. |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` (UI.Web) | 9.0.0 | 10.0.2 | Com a 9.0.0, o restore dá NU1608: Roslyn 4.8 contra 5.0, que vem com as dependências 10.x. |
| `System.ComponentModel`, `System.Security.Claims` (Domain) | 4.3.0 | **removidos** | NU1510: já fazem parte do framework. A doc recomenda remover. |
| `System.Formats.Asn1` (Infra.Data) | 9.0.3 | **removido** | NU1510, mesmo motivo. |

Ficaram **como estavam**, porque rodam em net10 e a migração não exige mudança: NetDevPack 8.0.2 (usa o NetDevPack.SimpleMediator, não o MediatR), FluentValidation 11.11.0, Swashbuckle 8.1.0, Newtonsoft.Json 13.0.3, System.ComponentModel.Annotations 5.0.0 (não marcado com NU1510), Containers.Tools.Targets, xunit, NetArchTest, coverlet e Microsoft.NET.Test.Sdk.

### CI e Docker (só a versão)

- `.github/workflows/dotnet-core.yml`: `dotnet-version: 9.0.x` → `10.0.x`.
- `src/Equinox.Services.Api/Dockerfile` e `src/Equinox.UI.Web/Dockerfile`: `aspnet:6.0`/`sdk:6.0` → `aspnet:10.0`/`sdk:10.0`. No .NET 10, as imagens padrão são Ubuntu.

### Textos

- `CLAUDE.md` e `docs/arquitetura.md`: as notas de versão passam a dizer `net10.0`.

## Breaking changes conferidos no código, sem alteração

- **ASP.NET Core 10:** não há uso de `WebHostBuilder`, `IWebHost` ou `WebHost`. O `IdentityHostingStartup.cs` usa só a interface `IWebHostBuilder`, que não ficou obsoleta. Também não há `IActionContextAccessor`, `IPNetwork`/`KnownNetworks`, `WithOpenApi` nem `IExceptionHandler`. O site usa `UseExceptionHandler("/error/500")` com caminho.
- **Razor runtime compilation obsoleta:** o pacote continua referenciado no UI.Web, mas `AddRazorRuntimeCompilation` não é chamado em lugar nenhum. O build não gerou aviso.
- **Cookie redirect desligado em endpoints de API** (mudança de comportamento): na API, requisição não autenticada a `[ApiController]` recebe 401 em vez de redirect. O `CustomAuthorize` já devolvia 401/403 por conta própria.
- **EF Core 10:** o projeto não usa `ExecuteUpdate`, complex types, colunas JSON nem convenções customizadas. As mudanças de `DateTimeOffset` e de `DateTime` com offset no Microsoft.Data.Sqlite não se aplicam: `Customer.BirthDate` e `StoredEvent.Timestamp` são `DateTime` sem offset. Os nomes de parâmetro no SQL gerado mudaram, mas nada no código depende deles. As migrations existentes aplicaram sem `PendingModelChangesWarning`.
- **SDK 10:** `dotnet watch` agora escreve o log em stderr. O comando do `CLAUDE.md` já redireciona com `2>&1`.

## Verificação

- `dotnet restore`: sem erros.
- `dotnet build`: sem erros e sem avisos de compilação (CS/ASP). Os avisos que restam são de auditoria do NuGet; veja "Pendências".
- `dotnet test`: 7 de 7 testes de arquitetura passando em net10.0.
- Site (`dotnet watch`, SQLite): subiu em https://localhost:61666, e o seed aplicou as migrations. `/`, `/sobre`, a lista de clientes, o login e `/error/404` responderam 200. Com um usuário registrado só para o teste, funcionaram cadastro, edição, detalhes e histórico de cliente (eventos `Registered` e `Updated`).
- API (`dotnet watch --no-launch-profile`, SQL Server LocalDB): subiu em https://localhost:5001. `/swagger` e `swagger.json` responderam 200, e `POST /customer` sem token respondeu 401. `GET /customer` respondeu 500 com `Cannot open database "Equinox" requested by the login`: o banco não existe no LocalDB desta máquina, porque a API não roda migrations (ver `docs/arquitetura.md`, "O que eu ainda não sei", item 3). Isso não foi corrigido e não tem relação com a migração. Está documentado, com a solução, no item 13 de `docs/melhorias.md`, com prioridade máxima.

## Pendências (fora do escopo, não alteradas)

- **Avisos de vulnerabilidade (NuGet audit):**
  - `System.Security.Cryptography.Pkcs` 9.0.11 chega por transitividade ao `Equinox.Infra.CrossCutting.Identity`, pelo Microsoft.Data.SqlClient 6.1.7 (NU1902/NU1903). Corrigir exige uma referência direta nova no projeto Identity.
  - `NuGet.Packaging`/`NuGet.Protocol` 6.12.1 (NU1901, baixa) vêm do `Microsoft.VisualStudio.Web.CodeGeneration.Design` 10.0.2.
  - Antes da migração, o build tinha 52 avisos desse tipo, por causa da Pkcs/Xml 9.0.3 diretas. Agora tem 24.
- `src/Equinox.Services.Api/Properties/launchSettings.json`: `"dotnetRunMessages": "true"` está como string, e o SDK 10 descarta o perfil inteiro.
- Dockerfiles: o `COPY` do site usa um caminho que não bate com o contexto `..\..`. Nenhum build de imagem foi feito. (O `EXPOSE 80/443` virou `EXPOSE 8080`, a porta padrão da imagem desde o .NET 8.)
- CI com `actions/checkout@v2` e `actions/setup-dotnet@v1`, que estão desatualizadas.
- `Nullable` e `ImplicitUsings` variam entre os projetos.
