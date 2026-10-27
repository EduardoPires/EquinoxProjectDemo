# Continuidade

Situação em 26/10/2026, no branch `estudo/pagina-sobre`.

## Onde a gente parou

A página de erro **500** ganhou o design novo:

- cartão centralizado com o título "Algo deu errado";
- mensagem "Não conseguimos concluir a sua solicitação. Tente de novo em instantes.";
- linha "Código da requisição: <id>";
- botão "Voltar para a página inicial".

Arquivos alterados, ainda sem commit:

- `src/Equinox.UI.Web/Views/Shared/Error.cshtml`
- `src/Equinox.UI.Web/Controllers/HomeController.cs` (textos do `id == 500` na action `Errors`, rota `error/{id}`)

## O que ficou pendente

- [ ] **Commit da 500.** O branch atual não combina com o tema. Criar um `estudo/<tema>` (por exemplo, `estudo/paginas-de-erro`) antes de commitar.
- [ ] **404.** Fica para depois, por decisão própria. Continua com o layout antigo e o texto em inglês.
- [ ] **403.** O título ainda é "Access Denied", em inglês, o que contraria a regra de interface em português. Decidir se entra no escopo.
- [ ] **Arquivos fora do tema no `git status`.** `AGENTS.md` aparece apagado e `CLAUDE.md`, `docs/arquitetura.md` e `.claude/` estão sem versionar. Decidir o que vai para qual commit, sem misturar com as páginas de erro.

Cuidados que continuam valendo:

- não incluir `EquinoxProject.db-shm` nem `-wal` em commit;
- parar o `dotnet watch` ao terminar, para não prender a porta 61666.

## Próximo passo

Criar o branch `estudo/paginas-de-erro` e commitar só `Error.cshtml` e `HomeController.cs`, com uma mensagem como "Traduz e redesenha a página de erro 500". Depois disso, decidir se a 403 entra agora ou junto com a 404.
