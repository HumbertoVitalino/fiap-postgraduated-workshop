# Regras de domínio (DDD) — convenções práticas

Este documento descreve **como decidir**, não uma lista fixa de invariantes — cada projeto novo tem seus próprios agregados. Ver `architecture.md` para a forma das classes (`AggregateRoot`, `ValueObject`, etc.).

## 1. Política de validação incremental

Não modele toda invariante possível de um agregado no primeiro commit dele. Adicione uma validação no construtor/método de comportamento (lançando `DomainException`) quando **os dois** critérios valem:

1. É um **fato de negócio sempre verdadeiro** — não uma opinião de formatação ou obrigatoriedade de campo. ("Ano do modelo não pode ser anterior ao ano de fabricação" é fato de negócio; "placa precisa ter esse regex" é formato.)
2. Existe (ou está a uma feature de distância) um **caminho de código real** que poderia violá-la.

Se só o critério 1 vale mas nenhum use case ainda constrói/muta a entidade de um jeito que possa violá-la, tudo bem esperar — adicionar a validação quando o primeiro use case real chegar, não antes.

**Onde cada tipo de checagem mora:**
- Formato de string, obrigatoriedade de campo, tamanho máximo → validator da Api (FluentValidation), nunca no Domain.
- Dígito verificador de documento, formato de identificador de negócio (placa, código) → validator da Api. Redundância proposital com uma checagem de domínio é aceitável quando o domínio também precisa da garantia (defesa em profundidade) — documente que é proposital, não um esquecimento.
- Fato de negócio sempre verdadeiro cruzando dois ou mais campos do mesmo agregado → construtor/método de comportamento do Domain, `DomainException`.
- "Já existe um registro com esse valor único" (duplicidade), "a referência para outro agregado existe" → use case da Application (repositório), não o Domain — o Domain não tem acesso a repositório.
- Violação de chave estrangeira / topologia de banco (ex. "não posso apagar um agregado que tem dependentes") → use case da Application, checagem explícita antes de remover — isso é uma preocupação de infraestrutura, não uma invariante de agregado; não force isso para dentro do Domain só para "ser mais DDD".

## 2. Mutação via método de comportamento, nunca reconstrução

Nunca escreva `new Entity(id, campo1, campo2Atualizado, ...)` para representar uma atualização. Escreva um método de comportamento nomeado no agregado (`UpdateProfile`, `ChangePassword`, `Approve`, `RecordExecution`, ...) que:
- muta os campos relevantes diretamente,
- revalida qualquer invariante que os novos valores possam violar,
- chama `SetUpdatedAt()`.

Motivo prático, não só estético: se o construtor dispara um domain event (ex. `EntityCreatedEvent`) — o que é comum quando o mesmo construtor serve para criar e para reidratar do banco — reconstruir a entidade para "atualizar" dispara esse evento de novo, de forma incorreta. Um método de comportamento dedicado nunca dispara o evento de criação.

## 3. Value Objects: use quando há comportamento/invariante, não por purismo

Um campo `string`/`enum` simples é uma escolha válida quando:
- a normalização (trim, uppercase, remoção de máscara) é feita de forma explícita e visível (no `init` do Input ou num Mapper), e
- a validação de formato mora no validator da Api.

Vale a pena promover para um Value Object quando o valor carrega **comportamento próprio** (métodos que operam sobre ele) ou uma invariante que faz sentido encapsular independente da entidade que o contém — não só porque "DDD pede VO para tudo". Decidir isso é uma escolha de design consciente por campo, documentada quando divergir do padrão do resto do projeto (ex.: todo outro identificador é `string` crua, menos um que virou VO — explique por quê).

## 4. Campos imutáveis por decisão de negócio

Para cada entidade que tem um "identificador de negócio" (documento, placa, código de catálogo), decida explicitamente, uma vez, se ele é imutável após a criação — normalmente sim, pelo mesmo motivo que um documento de identidade real não muda. Exclua esse campo do Input de update correspondente e documente a decisão (não é "esquecemos de incluir", é "decidimos que não muda").

Ao decidir isso para um agregado novo, releia como foi decidido para agregados irmãos com um campo equivalente — mesma categoria de decisão merece o mesmo critério, a menos que haja uma razão de negócio específica para divergir.

## 5. Um agregado não muta outro diretamente

Quando uma operação de negócio envolve dois agregados (ex. aprovar um pedido debita estoque de um item de catálogo), a orquestração vive no **use case da Application**, chamando um método de comportamento em cada agregado envolvido, dentro da mesma unidade de trabalho/commit. Nenhum dos dois agregados referencia ou muta o outro diretamente — eles só se conhecem por Id.

## 6. Eventos de domínio: mecânica e limites reais

Pipeline típico, de ponta a ponta:
1. O construtor/método de comportamento do agregado chama `RaiseDomainEvent(...)` — isso só adiciona o evento a uma lista **em memória** dentro do próprio objeto.
2. O repositório move esses eventos para o `DbContext`/UnitOfWork **só em escrita** (`AddAsync`/`Update`) — leituras nunca enfileiram eventos, senão todo `SELECT` reemitiria o evento de criação.
3. O `CommitAsync` primeiro persiste (try/catch próprio); **depois**, num try/catch separado, despacha os eventos pendentes — uma falha no dispatch não deve derrubar o resultado do commit.
4. O dispatcher invoca os handlers registrados, tipicamente de forma síncrona, ainda dentro da mesma requisição.

**Isso não é Event Sourcing** (o estado não é reconstruído replayando eventos) **nem Transactional Outbox** (não há garantia de entrega — se o processo cair entre persistir e despachar, o evento se perde silenciosamente). É só notificação em memória, válida dentro de uma única unidade de trabalho. Documente esse limite antes de alguém assumir garantias que a mecânica não oferece.

Mantenha eventos "magros" (só os ids envolvidos) até que um handler concreto precise de mais dados — nesse ponto, prefira um "evento gordo" (campos extras direto no evento) a fazer o handler reconsultar o repositório: mais barato e captura o estado exatamente como era no momento do evento.

Não adicione infraestrutura de evento (handler, dispatcher, fila) especulativamente. Se não há nenhum handler registrado, isso é esperado e deve ficar registrado como tal (não é um bug, é uma mecânica pronta esperando o primeiro consumidor real).

## 7. Idempotência como decisão de design, não acidente

- Uma operação de remoção (`Delete`) que já não encontra o registro deve, por padrão, ser tratada como **sucesso silencioso** (não um erro "não encontrado") — isso é o que torna `DELETE` idempotente de verdade para retries de rede/duplo clique. Só uma falha real de persistência deve marcar a operação como erro.
- Uma atualização que checa duplicidade de um campo único (email, telefone, ...) só deve rechecar quando o valor **muda** em relação ao estado atual persistido — checar incondicionalmente faz um segundo `PUT` idêntico falhar contra o próprio valor que ele mesmo gravou da primeira vez.

## 8. Quando um use case pode falhar por mais de um motivo

Não force o endpoint HTTP a inspecionar o texto da mensagem de erro para decidir o código de status. Se o `Output`/resultado do use case não carrega um código de erro estruturado, colapse todos os motivos de falha para um único status (geralmente 400) em vez de tentar adivinhar pela string. Se a distinção por código HTTP importar de verdade no futuro, a mudança correta é o `Output` passar a carregar um código de erro — não o endpoint interpretando mensagem.

## 9. Sinais de que algo ficou inconsistente (releia antes de implementar o próximo caso igual)

- Uma constante de erro (`{Aggregate}Errors.X`) existe mas nenhum código a referencia — invariante planejada e nunca implementada, ou implementada inline sem usar a constante.
- Uma coleção/campo modelado no agregado e exposto pela API, mas nenhum use case nunca escreve nele.
- Dois use cases equivalentes (CRUD do mesmo tipo, transições de estado) com política de idempotência ou formato de resposta diferentes sem uma razão de negócio documentada para a diferença.
- Um teste usando geração aleatória (AutoFixture) para campos que um construtor valida de forma cruzada (ex. `CampoA >= CampoB`) — vai falhar de forma intermitente; use valores fixos e consistentes para esses campos.
