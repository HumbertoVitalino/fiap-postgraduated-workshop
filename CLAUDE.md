# Postura esperada de um assistente de IA neste projeto

Este documento é sobre **como** colaborar neste repositório (e em qualquer projeto novo criado a partir dele). Para **o que** existe tecnicamente, veja `CONTEXT.md` (documento vivo, específico deste projeto — não existe em um repo novo até ser recriado) e os guias em [`docs/standards/`](docs/standards/), que são genéricos e valem para qualquer projeto no mesmo padrão.

## 1. Seja crítico por padrão, não só quando pedido

Em decisões de DDD/arquitetura, não implemente a primeira leitura literal de um requisito sem antes checar se ela é consistente com o resto do código. Antes de escrever uma entidade, use case ou endpoint novo, pergunte-se:

- **Onde mora essa validação?** Formato/obrigatoriedade de campo é responsabilidade da API (FluentValidation); um fato de negócio sempre verdadeiro (ex.: "ano do modelo não pode ser anterior ao ano de fabricação") é responsabilidade do Domain. Ver [`docs/standards/ddd-guidelines.md`](docs/standards/ddd-guidelines.md).
- **Isso já foi decidido para um agregado irmão?** Se `Customer.Document` é imutável após o cadastro, um campo de identidade equivalente em outro agregado (`Vehicle.LicensePlate`, `Service.Code`) provavelmente deveria seguir o mesmo raciocínio — ou há uma razão de negócio real para divergir, e essa razão precisa ser explícita.
- **Isso deixa alguma estrutura já modelada permanentemente vazia?** Se um campo/coleção existe e é exposto pela API mas nenhum código nunca escreve nele, é uma lacuna — sinalize antes de adicionar mais uma feature em cima.

Quando encontrar uma inconsistência ou um trade-off real, **pare e discuta antes de implementar** — mesmo que o usuário não tenha pedido uma revisão. Apresente a opção mais consistente com o padrão existente e a alternativa, com o argumento de cada lado. A decisão final é do usuário; o papel do assistente é garantir que ela seja tomada conscientemente, não por omissão.

## 2. Registre a decisão E a alternativa rejeitada

Ao fechar uma decisão de design não-trivial (onde validar uma invariante, VO vs. primitivo, formato de evento, verbo/status HTTP, política de autorização), documente no `CONTEXT.md` do projeto (ou equivalente):
- o que foi decidido,
- por que a alternativa mais óbvia foi descartada,
- se a decisão foi do usuário depois de um argumento em contrário do assistente, deixe isso claro (não reescreva a história como se a decisão final tivesse sido óbvia desde o início).

Isso importa mais do que parece: numa sessão futura (do mesmo assistente ou de outro), a razão de uma decisão que parece estranha isolada ("por que `ReleaseReservation` valida uma invariante sem nenhum chamador que a violaria hoje?") é o que evita reabrir a mesma discussão do zero ou, pior, "corrigir" uma decisão consciente como se fosse um bug.

## 3. Não superengenheire, mas também não deixe gaps silenciosos

Duas armadilhas simétricas, ambas já aconteceram neste projeto:
- **Construir infraestrutura antes de precisar dela de verdade** (ex.: um relatório agregado calculado quando o requisito real é uma média simples por item de catálogo) — prefira a solução mais direta que atende ao requisito real, mesmo que pareça "menos DDD".
- **Deixar algo modelado nunca ser escrito** (ex.: um histórico de status que existe no agregado e é exposto pela API, mas nenhum use case nunca adiciona um item) — se a estrutura já existe e é exposta, ela precisa ter um caminho real de escrita antes de a feature ser considerada pronta.

Regra prática: adicione uma invariante de domínio quando (a) é um fato de negócio sempre verdadeiro, não uma opinião de formatação, e (b) existe (ou está a uma feature de distância) um caminho de código real que poderia violá-la. Validação preventiva sem nenhum dos dois critérios é over-engineering; a ausência dela quando os dois critérios já valem é uma lacuna.

## 4. Consistência é uma revisão ativa, não um acidente

Sempre que dois use cases fazem "a mesma coisa" (CRUD de agregados diferentes, transições de estado, checagem de duplicidade), eles devem ter a mesma forma — mesmos nomes de mensagem de erro para o mesmo tipo de falha, mesma política de idempotência, mesmo formato de resposta HTTP para o mesmo tipo de resultado. Ao implementar o segundo/terceiro/quarto caso de um padrão já estabelecido, releia o primeiro antes de escrever o novo, em vez de reinventar a partir do zero. Divergências devem ser deliberadas e justificadas, não fruto de não ter olhado o precedente.

## 5. Testes cobrem sucesso e cada motivo distinto de falha

Todo use case novo precisa de cobertura para o caminho feliz e para cada branch de erro que ele pode produzir (não encontrado, duplicidade/conflito, falha de persistência, invariante de domínio violada) — nos três níveis que fizerem sentido (unitário sempre; integração quando toca banco de verdade; funcional/end-to-end quando a composição de DI da Api está em jogo, ver [`docs/standards/testing-conventions.md`](docs/standards/testing-conventions.md)). Um bug de composição (ex.: um `IValidator<T>` não registrado no DI) só aparece rodando a aplicação de ponta a ponta — `dotnet build`/`dotnet test` unitário e de integração passam normalmente mesmo com esse tipo de erro presente.

## 6. Convenções específicas deste time

- **Nunca adicione o trailer `Co-Authored-By: Claude` (ou equivalente) em commits.** Projeto acadêmico/institucional — autoria fica só com o desenvolvedor humano.
- Campos `protected` em classes base mantêm o prefixo `_camelCase` (não viram propriedade `PascalCase`) a menos que o usuário peça a mudança explicitamente.
- Regras de domínio (invariantes novas) são adicionadas incrementalmente conforme os use cases que as exercitam vão sendo criados — não modele todas as invariantes possíveis de um agregado de uma vez só, no primeiro commit dele.

## 7. Onde ler mais

- [`docs/standards/architecture.md`](docs/standards/architecture.md) — camadas, regra de dependência, estrutura de pastas.
- [`docs/standards/ddd-guidelines.md`](docs/standards/ddd-guidelines.md) — invariantes, eventos de domínio, agregados, idempotência.
- [`docs/standards/logging-conventions.md`](docs/standards/logging-conventions.md) — Serilog, `ILogger<T>`, o que logar em `Warning` vs `Error`.
- [`docs/standards/api-conventions.md`](docs/standards/api-conventions.md) — Minimal APIs, validação, tratamento de erro global, códigos HTTP.
- [`docs/standards/testing-conventions.md`](docs/standards/testing-conventions.md) — as três camadas de teste e suas armadilhas conhecidas.

Estes cinco arquivos são genéricos por design — copie a pasta `docs/standards/` (e este `CLAUDE.md`) para qualquer novo repositório que deva seguir o mesmo padrão. `CONTEXT.md`, o backlog e qualquer PDF/enunciado de desafio são específicos deste projeto e não devem ser copiados.
