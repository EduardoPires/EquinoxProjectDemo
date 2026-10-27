# CLAUDE.md

Este arquivo orienta o Claude Code (claude.ai/code) neste repositório.

## Comandos

```bash
dotnet build
dotnet test                                                # testes de arquitetura (NetArchTest)
dotnet test --filter "FullyQualifiedName~DomainTests"      # uma classe ou um teste
```

**Site** (https://localhost:61666): sempre em segundo plano com `dotnet watch`.
Rode com `run_in_background`, gravando o log no scratchpad (não na raiz do repo), e espere a linha `Now listening on` com Monitor (filtre também `Unhandled|Exited with error|error CS`: em caso de falha o watch não sai, fica esperando mudança):

```bash
DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=1 dotnet watch --project src/Equinox.UI.Web --non-interactive > site.log 2>&1
```

- Em Development usa SQLite (`src/Equinox.UI.Web/EquinoxProject.db`) e aplica migrations e seed sozinho. Login de teste: `teste@teste.com`.
- Pare o processo ao terminar. Uma instância esquecida prende a porta e a próxima não sobe.
- Parar à força deixa `EquinoxProject.db-shm` e `-wal`, que o `.gitignore` (`*.db`) não cobre. Nunca os inclua num commit.

**API** (https://localhost:5001/swagger): mesmo esquema, mas sem o perfil de launch.
O `launchSettings.json` dela tem `"dotnetRunMessages": "true"` (string), o SDK 10 descarta o perfil e a API cai com NullReference no JWT.

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="https://localhost:5001;http://localhost:5000" \
  dotnet watch --project src/Equinox.Services.Api --non-interactive --no-launch-profile > api.log 2>&1
```

Usa SQL Server `(localdb)` e não roda migrations.

## Convenções

- Textos da interface sempre em português.
- Não adicione pacotes NuGet sem autorização.
- Não faça commits sem pedido explícito. Quando pedido, mensagem e branch em português (`estudo/<tema>`, "Adiciona ..."). PRs vão para `master`.
- Os projetos usam `net10.0` (migração em `docs/migracao-net10.md`). Rodar exige o runtime ASP.NET Core 10. Não mude o target sem pedido.

## Antes de alterar

- Com o site rodando em segundo plano, não rode `dotnet build` nem `dotnet run` do site. O `dotnet watch` já compila, e um build concorrente trava arquivo (erro MSB3027).
- Comando ou evento novo: registre o handler à mão em `src/Equinox.Infra.CrossCutting.IoC/NativeInjectorBootStrapper.cs`. Não há varredura de assembly.
- Mensagens de validação do domínio (`src/Equinox.Domain/Commands/Validations`) aparecem na tela e ainda estão em inglês. Ao mexer nelas, traduza.
- Permissões são claims (`Customers`: `Write`, `Remove`), não roles.
- Rode `dotnet test` depois de mexer em referências entre projetos. Projeto novo entra em `TestsSupport.GetAllProjectAssemblies()`.

## Arquitetura

Consulte `docs/arquitetura.md` quando a tarefa envolver:
- camadas ou dependências entre projetos;
- o fluxo comando → evento → banco;
- Identity ou JWT;
- bancos e migrations.

A seção "O que eu ainda não sei" lista comportamentos não confirmados, por exemplo o login da API em Development.
