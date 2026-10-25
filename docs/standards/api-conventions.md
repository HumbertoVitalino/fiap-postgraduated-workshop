# Api — convenções de Minimal APIs

## Organização

- Endpoints agrupados por feature/agregado: `Endpoints/{Feature}/*Endpoints.cs`, registrados centralmente (ex. `app.MapMinimalApisV1()`).
- Versionamento por segmento de URL (`/api/v{version}/...`), com o gerador de doc (OpenAPI/Swagger) configurado para substituir `{version}` de verdade no documento gerado — sem isso, a UI de doc manda o template cru e todo request feito por ela dá 404 de roteamento.
- Um `{Action}Request` por endpoint com corpo, validado por FluentValidation, mapeado para o `Input` da Application por um Mapper de extensão dedicado em `Api/Mappers/`.

## Validação

- `IEndpointFilter` genérico (`ValidationFilter<TRequest>`) resolve `IValidator<TRequest>` do DI, roda a validação e devolve `400 BadRequest` com o corpo de erro padrão se inválido. Aplicado via um método de extensão (`.WithValidation<TRequest>()`).
- **Use a constraint `where TRequest : notnull`, não `where TRequest : class`** — um Request pode legitimamente ser um `record struct` (payload pequeno, ex. só um `CorrelationId`), e `class` não é satisfeita por tipo valor.
- **Registre todo `IValidator<TRequest>` novo em DI no mesmo commit que cria o Request.** Uma omissão aqui não vira erro de validação — vira `500 Internal Server Error` em runtime (`GetRequiredService<IValidator<T>>()` lança se não encontrar o serviço), e nem `dotnet build` nem os testes unitários/de integração pegam isso, porque nenhum dos dois exercita a composição de DI da Api. Só um teste que sobe a aplicação de ponta a ponta (`WebApplicationFactory`) ou um teste manual contra a aplicação rodando pega esse tipo de bug — por isso a camada de testes funcionais/end-to-end não é opcional assim que esse padrão de validação existe.

## Tratamento global de erro de domínio

Um `IExceptionHandler` (contrato nativo do framework, não o filtro de validação do FluentValidation) intercepta qualquer exceção de domínio não capturada localmente por um use case, converte para `400 BadRequest` com o corpo de erro padrão da aplicação, e é registrado **antes** de `AddProblemDetails()`. Qualquer outra exceção não tratada continua caindo no comportamento padrão (geralmente `500`).

Isso existe para que nenhum use case precise de `try/catch` local só para converter uma invariante de domínio violada em uma resposta HTTP amigável — o handler cobre isso de uma vez para todos os agregados presentes e futuros.

## `CorrelationId`

Convenção: `CorrelationId` é um **campo do corpo** (`Guid`, obrigatório) em todo Request que tem corpo — não um header. Em rotas sem corpo (`GET`, `DELETE` sem payload), o próprio endpoint gera um `Guid.NewGuid()` inline ao chamar o use case. Se a convenção mudar para header (ex. `X-Correlation-Id`), mude de uma vez em todos os Requests — não misture os dois estilos no mesmo projeto.

## Códigos de status HTTP

- **Criação bem-sucedida** → `201 Created`, com `Location` apontando para o recurso (lendo o id do DTO devolvido pelo use case — cuidado com o tipo genérico ao ler `Output.GetResult<T>()`, o `T` errado ali não quebra a build, só falha em runtime).
- **Leitura/mutação em recurso existente, sucesso** → `200 OK` com o corpo padrão de resultado.
- **Remoção idempotente** → `204 No Content`, tanto quando o recurso existia e foi removido quanto quando já não existia (ver `ddd-guidelines.md` §7) — só uma falha real de persistência vira `400`. Como o sucesso aqui não tem `Result`/mensagens para devolver, não faz sentido embrulhar num `200` — é a exceção deliberada ao padrão "sempre `Ok(result)`".
- **Busca por id com um único motivo de falha possível** → `404 NotFound` com o corpo de erro.
- **Mutação que pode falhar por mais de um motivo, sem um código de erro estruturado no `Output`** → colapse para um único `400 BadRequest` em vez de inspecionar a mensagem de erro para decidir o status (ver `ddd-guidelines.md` §8).
- **Autenticação inválida** → `401 Unauthorized`, tipicamente sem corpo (não vaze detalhe de qual parte da credencial falhou).
- **Autorização de identidade, não de papel** (ex. "só o próprio dono pode fazer isso") → `403 Forbidden`, checado no próprio endpoint via um serviço de usuário atual — essa regra vive na Api, não no use case, porque é sobre "quem está chamando", não uma regra de negócio do agregado.

## Endpoints de transição de estado / comando

Uma rota que representa uma transição de estado de uma máquina de estados (aprovar, rejeitar, concluir, entregar, ...) deve ser modelada como `POST /{id}/{substantivo}` (ex. `POST /orders/{id}/approval`), não `PATCH` + verbo na URL (`PATCH /orders/{id}/approve`) — especialmente quando o corpo da requisição não carrega nenhum campo sendo parcialmente atualizado (às vezes só um `CorrelationId`). Semanticamente, cada transição costuma ser a criação de um novo registro de histórico/evento no agregado, não a atualização de um campo — o verbo HTTP e o formato da URL devem refletir isso. Os nomes internos (UseCase, Input, Request, Mapper) continuam com o verbo original (`ApproveOrderUseCase`, `ApproveOrderRequest`) — só a superfície HTTP (rota) precisa virar substantivo.

## Autorização

- Nomeie policies pela combinação de papel que realmente importa ao domínio (ex. `AttendantOnly`, `MechanicOnly`), não uma policy por role isoladamente se elas nunca são usadas sozinhas.
- Quando a regra é "qualquer usuário autenticado, independente do papel", use `.RequireAuthorization()` sem policy nomeada em vez de inventar uma policy nova só para expressar "todos os papéis conhecidos".
- Corpos de request enviados por um cliente não confiável nunca devem carregar quem fez a ação (`CreatedBy`, `ChangedBy`) — esse valor vem do usuário autenticado (claims do token), lido por um serviço de usuário atual injetado direto no delegate do endpoint, nunca do corpo da requisição.

## Um corpo cru (`Guid`/primitivo) como payload é frágil

Evite modelar o corpo de um Request como um tipo primitivo cru (`[FromBody] Guid request`) mesmo quando ele carrega só um campo — na prática isso se mostra frágil para alguns clientes HTTP (proxies, alguns SDKs) que esperam um objeto JSON. Prefira sempre um tipo dedicado, mesmo que seja um `record`/`record struct` de um campo só (`ApproveOrderRequest(Guid CorrelationId)`) — mais barato do que descobrir o problema em produção.
