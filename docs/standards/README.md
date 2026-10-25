# Padrões do time — reutilizáveis em outros repositórios

Estes arquivos descrevem **como** este time constrói APIs em Clean Architecture + DDD — não o que existe neste projeto específico. São genéricos por design: nenhum menciona um agregado, ticket ou branch deste repositório.

## Como usar em um projeto novo

1. Copie esta pasta (`docs/standards/`) e o `CLAUDE.md` da raiz para o novo repositório.
2. **Não copie** `CONTEXT.md`, o backlog ou qualquer PDF/enunciado de desafio — esses são específicos deste projeto. Um projeto novo deveria começar seu próprio `CONTEXT.md` vazio (ou com uma seção 1 mínima), a ser preenchido conforme as decisões reais daquele projeto forem tomadas.
3. Se o novo projeto usa uma stack diferente (outro banco, outro framework de log), ajuste o conteúdo de `architecture.md`/`logging-conventions.md` — o valor destes documentos está na **forma das decisões** (onde validar o quê, como nomear, como testar), não nas ferramentas específicas citadas como exemplo.

Este repositório também é um template `dotnet new` (`.template.config/template.json`, `shortName: clean-arch`) — como estes arquivos não estão na lista de exclusão do template, um projeto gerado via `dotnet new clean-arch` já nasce com eles.

## Índice

| Arquivo | Cobre |
|---|---|
| [`architecture.md`](architecture.md) | Camadas, regra de dependência, estrutura de pastas por camada, padrão Input/UseCase/Output |
| [`ddd-guidelines.md`](ddd-guidelines.md) | Onde validar o quê, mutação via comportamento vs. reconstrução, Value Objects, eventos de domínio, idempotência |
| [`logging-conventions.md`](logging-conventions.md) | Setup do Serilog, o que logar em `Warning` vs `Error`, segurança de log, como testar log |
| [`api-conventions.md`](api-conventions.md) | Minimal APIs, validação, tratamento global de erro, códigos HTTP, convenções de rota para transições de estado |
| [`testing-conventions.md`](testing-conventions.md) | As três camadas de teste, armadilhas conhecidas (AutoFixture + invariante cruzada, fixture de banco compartilhado) |

Ver também `CLAUDE.md` na raiz — a postura esperada de um assistente de IA colaborando neste padrão (crítica ativa em decisões de design, não só quando pedido).
