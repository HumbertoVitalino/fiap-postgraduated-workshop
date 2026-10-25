# Arquitetura — Clean Architecture + DDD (.NET)

Guia genérico de como este time estrutura uma API em Clean Architecture com DDD. Reutilizável em qualquer projeto novo no mesmo padrão — não descreve nenhum agregado específico, só a forma.

## Camadas e regra de dependência

```
Domain          →  entidades, value objects, eventos de domínio — zero dependências externas
Application     →  use cases, boundaries (Input/Output), interfaces (portas)
Infrastructure  →  EF Core, repositórios, serviços concretos (JWT, hashing, dispatcher de eventos)
Api             →  Minimal APIs, composição de DI, validadores, mapeadores HTTP ↔ Application
```

`Domain` não referencia nada. `Application` referencia `Domain`. `Infrastructure` referencia `Application`. `Api` referencia `Infrastructure` (e, transitivamente, todo o resto). Nunca o inverso — se `Domain` precisar de algo de `Infrastructure` (ex. gerar um id sequencial via banco), a resposta é expor uma porta em `Application`/`Domain` e implementá-la em `Infrastructure`, não importar a dependência para dentro.

```
src/
  {Solucao}.Domain
  {Solucao}.Application
  {Solucao}.Infrastructure
  {Solucao}.Api
tests/
  {Solucao}.UnitTests           # xUnit + Moq + AutoFixture — Domain e Application isolados, sem I/O
  {Solucao}.IntegrationTests    # xUnit + EF Core real contra o banco (via docker-compose de testes)
  {Solucao}.FunctionalTests     # xUnit + WebApplicationFactory — HTTP fim-a-fim, testa a composição de DI
```

## Domain

### `Abstractions/`
- `AggregateRoot` — classe abstrata **não genérica** (não `AggregateRoot<TId>`): construtor posicional `(Guid id, DateTime createdAt, DateTime updatedAt)`; `Id`/`CreatedAt`/`UpdatedAt` com setter `protected`; lista interna de `IDomainEvent`; `RaiseDomainEvent` (protected), `GetDomainEvents()`, `ClearDomainEvents()`, `SetUpdatedAt()` (protected).
- `ValueObject` — igualdade estrutural via `GetEqualityComponents()`. Use quando o valor tem comportamento/invariante própria; um `string`/`enum` simples é aceitável quando não tem (ver `docs/standards/ddd-guidelines.md`).
- `DomainException(string message)` — exceção de domínio simples, tratada globalmente na Api (ver `api-conventions.md`).
- `IDomainEvent` — contrato marcador. Não existe `IAggregateRoot` separado.

### Entidades
- Um construtor público, posicional, aceitando todos os campos incluindo `id`/`createdAt`/`updatedAt` — serve tanto para criar quanto para reidratar do banco (não existem fábricas `Create`/`Rehydrate` separadas, a menos que o agregado precise de lógica de criação distinta de reidratação).
- Mutação **sempre** via método de comportamento nomeado (`UpdateProfile`, `Approve`, `Reject`, ...), nunca reconstruindo a entidade com `new Entity(...)` para "atualizar" — ver `ddd-guidelines.md` para o porquê.
- Setter `private` em toda propriedade.

### `Errors/{Aggregate}Errors.cs`
Classe estática de constantes de mensagem por agregado (`InvalidX`, `NotFound`, ...), lançadas como `DomainException` dentro do próprio agregado. Só existe consumidor real quando a invariante correspondente é de fato validada em algum lugar — uma constante órfã (existe mas nada a referencia) é um sinal de invariante planejada e não implementada, não um problema em si.

### `Events/`
Um `{Aggregate}CreatedEvent`/`{Aggregate}{Ação}Event` por evento relevante, "evento magro" por padrão (só os ids envolvidos) — só enriqueça com mais dados quando um handler concreto precisar deles (evita re-consulta do agregado no handler). Ver `ddd-guidelines.md` para a mecânica completa de disparo/enfileiramento/dispatch.

## Application

Padrão de use case: **Input (record/struct) → UseCase (classe + interface) → Output (classe genérica de resultado)**, injeção direta de interfaces — sem MediatR.

```
UseCases/
  {Aggregates}/              # nome de pasta PLURAL — ver nota abaixo
    {UseCase}/
      Boundaries/             # Input
      Mapper/                 # extensões MapToDomain / MapToDto / MapToInput
      I{UseCase}UseCase.cs
      {UseCase}UseCase.cs
Interfaces/
  UseCases/                   # interfaces dos use cases, pasta singular (sem tipo de domínio pra colidir)
  Repositories/
  Services/
Commons/
  Output.cs                   # IsValid, Messages, ErrorMessages, Result (object, lido via GetResult<T>()),
                               # AddResult / AddMessage / AddErrorMessage(s)
```

**Por que a subpasta do agregado é plural** (`Users/`, `Customers/`, não `User/`, `Customer/`): um arquivo dentro de `UseCases.Customer.CreateCustomer.Mapper` que referencia o tipo `Customer` (`Domain.Entities.Customer`) sem qualificar dá erro de compilação — o compilador prioriza o namespace irmão de mesmo nome sobre o tipo trazido por `using`. Plural evita a colisão de propósito.

Cada `UseCase`:
1. Recebe `ILogger<TUseCase>` no construtor (ver `logging-conventions.md`).
2. Busca/checa pré-condições via repositório — falha de negócio (não encontrado, duplicado) vira `LogWarning` + `AddErrorMessage`, sem persistir nada.
3. Constrói/muta a entidade via um Mapper de extensão (`input.MapToDomain()` / `entity.UpdateProfile(...)`) — nunca monta a entidade inline no corpo do use case.
4. Persiste + `UnitOfWork.CommitAsync()`. Falha no commit → `LogError` + `AddErrorMessage`.
5. Sucesso → `output.AddResult(entity.MapToDto())`.

Mappers de leitura (`GetX`) reaproveitam o `MapToDto` do `CreateX` por extensão em vez de duplicar o mapeamento campo a campo — só ganham um Mapper próprio quando o DTO de leitura difere de fato do de criação.

## Infrastructure

- `AppDbContext` implementa `IUnitOfWork`. `CommitAsync` roda `SaveChangesAsync` num `try/catch` próprio (falha → `LogError` + retorna `false`); **num `try/catch` separado**, entrega eventos de domínio pendentes ao dispatcher — falha no dispatch é só logada, não derruba o resultado do commit.
- Um `{Aggregate}Model` por agregado, desacoplado da entidade de Domain — mapeado via `ModelMappers`/`DomainMappers` (extensões, chamando `new Entity(...)`/`new Model(...)` diretamente).
- `{Aggregate}Repository : I{Aggregate}Repository` — herda operações genéricas de um `Repository<T>` base (`GetByIdAsync`, `AddAsync`, `Update`, `Remove`) e ganha métodos extras (`ExistsWithXAsync`, `GetAllAsync`, `GetByXAsync`) só quando um use case real precisa deles — não adicione um método de repositório especulativo.
- Repositórios usam `AsNoTracking()` em toda leitura.
- Um agregado com coleções-filhas (ex. um agregado rico com várias entidades internas) frequentemente precisa de um `UpdateAsync` dedicado que reconcilie cada coleção filha (diff de ids existentes vs. incoming) em vez de confiar no `Update()` genérico do EF Core sobre um grafo desconectado — o `Update()` genérico marca filhos com id explícito como `Modified` mesmo quando são novos, silenciosamente descartando inserções. Ver `ddd-guidelines.md`.
- Schema de banco: escolha **uma** fonte de verdade (migrations do EF Core **ou** um script SQL idempotente aplicado por um serviço de init) e não deixe as duas divergirem sem registrar a divergência. Se optar por script SQL, todo `ALTER TABLE` precisa ser guardado por uma checagem de existência de coluna (`IF NOT EXISTS (SELECT * FROM sys.columns ...)`), não só o `CREATE TABLE` inicial.

## Api

- Minimal APIs organizadas por feature: `Endpoints/{Feature}/*Endpoints.cs`, registradas centralmente (`app.MapMinimalApisV1()` ou equivalente).
- Um `{Action}Request` por endpoint com corpo, validado por FluentValidation, mapeado para o `Input` da Application via um Mapper de extensão dedicado (`Api/Mappers/`).
- Composição de DI centralizada (`IoC/DependencyInjection.cs`): registro de repositórios, serviços, validadores e policies de autorização em métodos de extensão nomeados (`AddRepositories`, `AddServices`, `AddValidators`, `AddRoles`, ...).

Ver `api-conventions.md` para o detalhe de validação, tratamento de erro e convenções HTTP, e `logging-conventions.md` para o pipeline de logging.

## Stack de referência

| Categoria | Escolha |
|---|---|
| Runtime | .NET (LTS mais recente disponível) / C# `latest`, `Nullable` habilitado, `TreatWarningsAsErrors=true` |
| API | Minimal APIs + versionamento por segmento de URL |
| Documentação | OpenAPI + Swagger UI |
| Autenticação | JWT Bearer, policies nomeadas por combinação de papel que importa ao domínio |
| Hash de senha | BCrypt (ou equivalente), via uma interface de serviço técnico — não é Domain Service |
| Persistência | EF Core + banco relacional, quando o domínio é fortemente relacional |
| Validação | FluentValidation, um validator por Request |
| Logging | Serilog (bootstrap logger + request logging) |
| Testes | xUnit, FluentAssertions, **uma** lib de mock (não duas), AutoFixture |
| Erros HTTP | `AddProblemDetails()` + `IExceptionHandler` global para exceções de domínio |
