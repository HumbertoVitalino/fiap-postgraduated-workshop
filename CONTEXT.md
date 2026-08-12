# Fiap.Workshop — Contexto do Projeto

> Documento vivo de contexto técnico. Atualize sempre que a arquitetura, os use cases ou as decisões de design mudarem. Última atualização: 2026-08-11 (branch `feature/5-create-customer-uc`).

## 1. Visão geral

API .NET 10 seguindo **Clean Architecture** (Domain → Application → Infrastructure → Api), construída como template de workshop de pós-graduação FIAP. Expõe endpoints via **Minimal APIs** (não MVC controllers), com versionamento de API, autenticação JWT e autorização baseada em roles.

Solução: `Fiap.Workshop.slnx`

```
src/
  Fiap.Workshop.Domain          # Entidades, Value Objects, eventos de domínio — zero dependências externas
  Fiap.Workshop.Application     # Use cases, boundaries (Input/Output), interfaces (portas)
  Fiap.Workshop.Infrastructure  # EF Core, repositórios, serviços concretos (JWT, hashing, current user, dispatcher)
  Fiap.Workshop.Api             # Minimal API endpoints, DI composition root, validators, mappers HTTP↔Application
tests/
  Fiap.Workshop.UnitTests           # xUnit + Moq/NSubstitute + AutoFixture — Domain e Application isolados
  Fiap.Workshop.IntegrationTests    # xUnit + EF Core real contra SQL Server (docker-compose.tests.yml)
  Fiap.Workshop.FunctionalTests     # xUnit + Microsoft.AspNetCore.Mvc.Testing — projeto criado, SEM testes ainda
```

Regra de dependência: `Domain` não referencia nada; `Application` referencia `Domain`; `Infrastructure` referencia `Application`; `Api` referencia `Infrastructure` (e transitivamente todo o resto).

## 2. Stack e pacotes principais

- **.NET 10** / C# `latest`, `Nullable` habilitado, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true` (`Directory.Build.props`)
- **API**: Minimal APIs, `Asp.Versioning.Http` + `Asp.Versioning.Mvc.ApiExplorer` (versionamento via segmento de URL `/api/v{version}/...`; o `ApiExplorer` com `SubstituteApiVersionInUrl = true` é o que faz `{version}` virar `1` de verdade na doc gerada — sem ele, Swagger/OpenAPI mandavam o template cru e todo request pelo Swagger UI dava 404 de roteamento, corrigido em 2026-07-12), `Microsoft.AspNetCore.OpenApi` (gera o documento OpenAPI em `/openapi/v1.json`), `Swashbuckle.AspNetCore.SwaggerUI` (só a UI, em `/swagger` — trocado do Scalar em 2026-07-11, mantendo a geração do doc como estava)
- **Logging**: `Serilog.AspNetCore` (instalado em 2026-07-11) — substitui o provider padrão do `Microsoft.Extensions.Logging` via `builder.Host.UseSerilog(...)` no `Program.cs`; usa bootstrap logger + `try/catch/finally` em volta do host pra capturar falhas de startup (ex.: banco indisponível na migration automática) antes mesmo do DI terminar de montar; sink Console configurado via `appsettings.json` (`Serilog` section, `ReadFrom.Configuration`); `app.UseSerilogRequestLogging()` loga cada requisição HTTP. Todo `ILogger<T>` já injetado nos use cases continua funcionando sem mudança nenhuma — a troca foi só na composição, não no código de Application/Domain.
- **Validação**: FluentValidation (validators na camada Api, um por Request)
- **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), policies `AdminOnly`, `AttendantOnly` (roles `Attendant`/`Admin`) e `MechanicOnly` (roles `Mechanic`/`Admin`) — substituíram as antigas `AdminOnly`/`UserOnly` em algum ponto entre 2026-08-08 e 2026-08-11, junto com a mudança de `UserRole` (ver §3): não existe mais um role genérico `User`, só os três papéis de negócio da oficina.
- **Hashing de senha**: `BCrypt.Net-Next` (work factor 12), via `IPasswordService` (`Application.Interfaces.Services`) / `PasswordService` (Infrastructure) — ver §10 para a diferença em relação a uma iteração anterior do projeto que tinha isso como Domain Service
- **Validação de CPF/CNPJ**: pacote `Cpf.Cnpj` (namespace `CpfCnpjLibrary`), usado via `StringExtensions.StandardizeDocument`/`IsValidDocument` (`Application/Commons/StringExtensions.cs`) — ver §4 e §6
- **Persistência**: EF Core 10 + SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`)
- **Testes**: xUnit, FluentAssertions, Moq, NSubstitute (ambos presentes — inconsistência a resolver, ver §9), AutoFixture
- **Erros HTTP**: `AddProblemDetails()` + `UseExceptionHandler()`

## 3. Camada de Domínio (`Fiap.Workshop.Domain`)

> ⚠️ Esta seção foi reescrita em 2026-08-08 para refletir o código real da branch `feature/3-create-user-uc`. Uma iteração anterior do projeto (registrada em commits antigos) havia desenhado `User` com Value Objects (`HashedPassword`, `Email`) e fábricas nomeadas (`Create`/`Rehydrate`); esse desenho **não existe no código atual** — ver nota em §10.

### Abstrações (`Abstractions/`)
- `AggregateRoot` (classe abstrata, **não genérica**): construtor posicional `(Guid id, DateTime createdAt, DateTime updatedAt)`; `Id`/`CreatedAt`/`UpdatedAt` com setter `protected`; lista interna de `IDomainEvent`; `RaiseDomainEvent` (protected), `GetDomainEvents()`, `ClearDomainEvents()`, `SetUpdatedAt()` (protected, `UpdatedAt = DateTime.UtcNow`).
- `ValueObject`: igualdade estrutural via `GetEqualityComponents()` — hoje **sem nenhum VO concreto usando** (nenhuma classe do Domain herda dela).
- `DomainException(string message)`: exceção de domínio simples.
- `IDomainEvent`: contrato marcador (não existe `IAggregateRoot` separado).

### Agregado `User` (`Entities/User.cs`)
Propriedades: `Name`, `Password`, `Role` (`UserRole`), `Email` — todas `string`/`enum` simples com setter `private`. **Não há Value Objects.**

Um único construtor **público**, posicional: `User(Guid id, string email, string name, string password, UserRole role, DateTime createdAt, DateTime updatedAt)`. Não existem `Create`/`Rehydrate`/fábricas nomeadas, e não há validação de invariante nenhuma no construtor (nome vazio, formato de e-mail, força de senha — tudo isso, se acontece, acontece na Api, ver §6). O construtor **sempre** dispara `RaiseDomainEvent(new UserCreatedEvent(id))`, inclusive quando `DomainMappers.MapToDomain` (Infrastructure) chama `new User(...)` para reidratar um usuário lido do banco — na prática é inofensivo hoje porque só `UserRepository.AddAsync`/`Update` enfileiram os eventos pendentes no `AppDbContext` (`EnqueueEvents`); leituras (`GetByIdAsync`/`GetByEmailAsync`) descartam o evento junto com o objeto. É um ponto frágil a observar se um dia uma leitura passar a enfileirar eventos — todo `SELECT` de usuário reemitiria "usuário criado".

### `UserRole` (enum, **mudou entre 2026-08-08 e 2026-08-11**): `Admin = 0`, `Attendant = 1`, `Mechanic = 2`
Substituiu o desenho anterior (`User = 0`, `Admin = 1`) — não existe mais um role genérico `User`; os três valores mapeiam papéis reais da oficina (recepção/atendimento, mecânico, administrador). Todos os testes que fixavam `UserRole.User` foram migrados para `UserRole.Admin` (commit `14b2aac`, "test(refactor): fixing the enum"). Ver policies novas em §2/§6.

### `UserErrors` (constantes de mensagem, hoje **não usadas por nenhum use case** — sobras de uma iteração anterior)
`NameEmpty`, `NotFound`, `EmailAlreadyInUse`, `PasswordEmpty`, `PasswordTooShort`, `PasswordMissingUppercase`, `PasswordMissingLowercase`, `PasswordMissingDigit`, `PasswordHashEmpty`, `InvalidCredentials`.

### Eventos (`Events/`)
- `UserCreatedEvent(Guid UserId)` — disparado (sempre) no construtor de `User`, ver acima. Só carrega o `UserId` ("evento magro"). Ver §11 para a mecânica completa de domain events.

### Agregado `Customer` (`Entities/Customer.cs`) — primeiro agregado de negócio com pilha completa (2026-08-11)
Propriedades: `Name`, `Document`, `Email`, `Phone` — todas `string` simples, setter `private`. **Sem Value Objects, sem validação de invariante no construtor** (nem formato de CPF/CNPJ, nem unicidade) — consistente com a decisão de validação incremental (ver §9/§10): a checagem de formato/dígito verificador de CPF/CNPJ mora na Api (`CreateCustomerRequestValidator`, via `IsValidDocument()`), não no Domain. Construtor único, posicional: `Customer(Guid id, string name, string document, string email, string phone, DateTime createdAt, DateTime updatedAt)`. **Não dispara nenhum domain event** — diferente de `User` (ver §11), `Customer` não tem um `CustomerCreatedEvent` equivalente hoje.

### Outros agregados (existem, ainda não documentados em detalhe aqui)
`Vehicle`, `Service`, `InventoryItem`, `ServiceOrder` (+ `ServiceOrderPart`/`ServiceOrderService`/`ServiceOrderStatusHistory` como filhos do mesmo agregado), com seus próprios `Errors/ServiceOrderErrors.cs` e enums (`UnitOfMeasure`, `ServiceOrderStatus`). Já têm `Model`/`Repository`/mapeamentos completos em Infrastructure (ver §5), mas ainda não são expostos via Application/Api — desatualiza o item 7 de §9, que precisa de uma revisão futura.

## 4. Camada de Aplicação (`Fiap.Workshop.Application`)

> ⚠️ Reescrita em 2026-08-08 (junto com §3) e atualizada em 2026-08-11 com a chegada de `LoginUser` e `CreateCustomer`. `GetUserByIdUseCase`/`UpdateEmailUseCase` continuam sem existir no código (só sobrou `UpdateEmailRequest` HTTP scaffoldado na Api sem use case/endpoint por trás) — mas `LoginUseCase` (`LoginUserUseCase`) **já existe**, ver abaixo.

Padrão de use case: **Input (record) → UseCase (classe) → Output (classe genérica com Result/Errors)**, sem MediatR — injeção direta de interfaces de use case.

### `Commons/Output.cs`
Classe de retorno padrão de todos os use cases: `IsValid`, `Messages`, `ErrorMessages`, `Result` (object, lido via `GetResult<T>()`), métodos `AddResult`, `AddMessage`, `AddErrorMessage(s)`.

### Use cases existentes (três hoje: `CreateUser`, `LoginUser`, `CreateCustomer`)

Cada use case tem uma pasta `Boundaries/` (Input) e `Mapper/` (mapeamento entre camadas).

#### `UseCases/CreateUser/`

`CreateUserUseCase` (interface `ICreateUserUseCase`), input `CreateUserInput(CorrelationId, Name, Email, Password, Role)`:
1. Checa e-mail único via `IUserRepository.ExistsWithEmailAsync` — se já existe, loga `LogWarning` e retorna `Output` com erro (`"User with email {Email} already exists."`), sem tocar no repositório.
2. Hasheia a senha via `IPasswordService.Hash(input.Password)`.
3. Monta o `User` via `input.MapToDomain(passwordHash)` (usando `input.Role` direto, sem lógica extra) e persiste (`AddAsync` + `UnitOfWork.CommitAsync`).
4. Se o commit falhar (`CommitAsync` retorna `false`), loga `LogError` e retorna erro (`"Error saving user with email {Email}."`).
5. Em sucesso, `output.AddResult(user.MapToDto())` — **mudou em relação à versão anterior deste documento**: hoje devolve `UserResponse` (DTO), não a entidade `User` crua.

Recebe `ILogger<CreateUserUseCase>` no construtor. Cobertura de teste: 3 cenários unitários (sucesso, e-mail duplicado, falha ao salvar) em `CreateUserUseCaseUnitTests`, usando o helper `LoggerTestBase<TCategory>` (`tests/Fiap.Workshop.UnitTests/Common/`) para verificar `ILogger.Log(...)` sem tropeçar no fato de `LogWarning`/`LogError` serem extension methods (Moq não consegue mockar/verificar extension methods diretamente — é preciso verificar o `ILogger.Log<TState>` real por trás). Mais cenários de integração (sucesso, e-mail duplicado) em `CreateUserUseCaseIntegrationTests`, batendo no SQL Server real via `DatabaseFixture`.

**Mapper (`UseCases/CreateUser/Mapper/CreateUserMapper.cs`)**
- `MapToDomain(this CreateUserInput input, string passwordHash)`: `new User(input.CorrelationId, input.Email, input.Name, passwordHash, input.Role, DateTime.Now, DateTime.Now)`. Usa **`input.CorrelationId` como `Id` do usuário** (não gera um novo `Guid`) — isso não mudou. O bug de ordem `Name`/`Email` trocada (registrado numa versão anterior deste documento) já estava corrigido.
- `MapToDto(this User user)`: `new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString())` — hoje é usado por `CreateUserUseCase` (ver acima).

#### `UseCases/LoginUser/` (novo desde a última reescrita deste documento)

`LoginUserUseCase` (interface `ILoginUserUseCase`), input `LoginUserInput(CorrelationId, Email, Password)`:
1. Busca o usuário por e-mail (`IUserRepository.GetByEmailAsync`) — se não existir, loga `LogWarning` e retorna erro `"User not found."`.
2. Verifica a senha via `IPasswordService.Verify(input.Password, user.Password)` — se inválida, loga `LogWarning` e retorna erro `"Invalid password."`.
3. Gera o token via `IJwtService.GenerateToken(user)` (assinatura mudou, ver Interfaces abaixo) e devolve o token cru (`string`) via `output.AddResult(token)` — não há um `LoginResponse`/DTO envolvido aqui, apesar de `LoginResponse` existir em `DTOs/Users/`.

Sem mapper próprio na Application (o `LoginUserInput` é montado direto pelo `LoginUserMapper` da Api, ver §6). Cobertura: unitários (`LoginUserUseCaseUnitTests`) e de integração (`LoginUserUseCaseIntegrationTests`, seedando um usuário via `ICreateUserUseCase` de verdade antes de logar).

#### `UseCases/CreateCustomer/` (novo, branch atual `feature/5-create-customer-uc`)

`CreateCustomerUseCase` (interface `ICreateCustomerUseCase`), input `CreateCustomerInput(CorrelationId, Name, Document, Email, Phone)` — o próprio `init` de `Document` já normaliza o valor via `StringExtensions.StandardizeDocument()` (remove pontuação, formata conforme CPF/11 dígitos ou CNPJ/14 dígitos; lança `ArgumentException` para outro tamanho):
1. Checa documento único via `ICustomerRepository.AnyAsync(input.Document, ...)` — se já existe, loga `LogWarning` e retorna erro `"Customer with the provided document already exists."`, sem tocar no repositório.
2. Monta o `Customer` via `input.MapToDomain()` (gera um novo `Guid` para o `Id` — diferente do `CreateUser`, que reusa `CorrelationId`) e persiste (`AddAsync` + `UnitOfWork.CommitAsync`).
3. Se o commit falhar, loga `LogWarning` e retorna erro `"Failed to save customer to the database."`.
4. Em sucesso, `output.AddResult(customer.MapToDto())` — devolve `CustomerResponse(Id, Name, Phone)` (**não inclui `Document`/`Email`** no DTO de resposta).

Validação de **formato/dígito verificador de CPF/CNPJ não acontece aqui** — só a normalização de pontuação. A validação de fato (`IsValidDocument()`) mora em `CreateCustomerRequestValidator` na Api (ver §6); o use case em si aceita qualquer string cujo tamanho normalizado seja 11 ou 14 caracteres. Cobertura: unitários (`CreateCustomerUseCaseUnitTests` — sucesso, documento duplicado, falha ao salvar) e de integração (`CreateCustomerUseCaseIntegrationTests`, adicionada em 2026-08-11 — sucesso persistindo e lendo de volta via `ICustomerRepository.GetByIdAsync`, e falha por documento duplicado; usa um gerador de CPF com dígito verificador válido em `TestData.Document()` para não depender de string fixa).

**Mapper (`UseCases/CreateCustomer/Mapper/CreateCustomerMapper.cs`)**
- `MapToDomain(this CreateCustomerInput input)`: `new Customer(Guid.NewGuid(), input.Name, input.Document, input.Email, input.Phone, DateTime.Now, DateTime.Now)`.
- `MapToDto(this Customer customer)`: `new CustomerResponse(customer.Id, customer.Name, customer.Phone)`.

### Interfaces / portas (`Interfaces/`)
- `Repositories/IUnitOfWork.CommitAsync()`
- `Repositories/IUserRepository : IRepository<User>` — `GetByEmailAsync(string email, ...)`, `ExistsWithEmailAsync(string email, ...)`
- `Repositories/ICustomerRepository : IRepository<Customer>` — `AnyAsync(string document, ...)` (adicionado com o `CreateCustomer` use case; antes a interface era vazia, seguindo a política incremental de repositório — ver §9)
- `Abstractions/IRepository<T> where T : AggregateRoot` — **sem segundo parâmetro `TId`** (assume `Guid` implicitamente): `UnitOfWork`, `GetByIdAsync`, `AddAsync`, `Update`, `Remove`. Outras portas de repositório já existem seguindo o mesmo molde, ainda vazias (sem método extra): `IVehicleRepository`, `IInventoryItemRepository`, `IServiceRepository`, `IServiceOrderRepository` (implementações em Infrastructure, ver §5, mas sem use case/endpoint consumindo ainda).
- `Services/ICurrentUserService` — `UserId` (`Guid`), `Email` (`string`), `Role` (`string`), `IsAuthenticated` (lido do `ClaimsPrincipal`)
- `Services/IDomainEventDispatcher.DispatchAsync(events, cancellationToken)`
- `Services/IJwtService.GenerateToken(User user)` — **mudou desde a última reescrita**: antes recebia três `string` soltos (`userId`, `email`, `role`); hoje recebe a entidade `User` inteira e extrai os claims internamente (ver implementação em §5).
- `Services/IPasswordService` — `Hash(string rawPassword)`, `Verify(string rawPassword, string hash)`. Mora em `Application.Interfaces.Services` (não em Domain) — ver nota em §10 sobre essa diferença em relação a uma iteração anterior do projeto.
- `Abstractions/IDomainEventHandler<TEvent>` — infraestrutura de handlers de evento de domínio (nenhum handler concreto implementado ainda — ver §11)
- `UseCases/ICreateUserUseCase`, `ILoginUserUseCase`, `ICreateCustomerUseCase` — um por use case, todos só com `Handle(TInput, CancellationToken) : Task<Output>`.

### Commons (`Commons/`)
- `Output.cs` — ver acima.
- `StringExtensions.cs` (novo): `StandardizeDocument(this string document)` (formata CPF/CNPJ sem pontuação via `CpfCnpjLibrary`, baseado no tamanho — 11 = CPF, 14 = CNPJ, outro tamanho lança `ArgumentException`) e `IsValidDocument(this string document)` (valida dígito verificador; tamanho fora de 11/14 retorna `false` em vez de lançar). Usado por `CreateCustomerInput` (normalização) e por `CreateCustomerRequestValidator` na Api (validação de fato, ver §6).

### DTOs
- `DTOs/Users/UserResponse(Id, Name, Email, Role)` — **hoje é usado** por `CreateUserUseCase.MapToDto` (mudou desde a última reescrita, ver acima); `Role` é `string` (`user.Role.ToString()`).
- `DTOs/Users/LoginResponse(Token)` — segue existindo como DTO, mas `LoginUserUseCase` devolve o token como `string` cru (`output.AddResult(token)`), não empacotado num `LoginResponse` — parece scaffold ainda não conectado.
- `DTOs/Customer/CustomerResponse(Id, Name, Phone)` (novo) — usado por `CreateCustomerUseCase.MapToDto`. Note que **não inclui `Document` nem `Email`** na resposta.

### Composição (`IoC/DependencyInjection.cs`)
`AddApplication()` registra `ICreateUserUseCase`, `ILoginUserUseCase` e `ICreateCustomerUseCase`, todos como `Scoped`.

## 5. Camada de Infraestrutura (`Fiap.Workshop.Infrastructure`)

> ⚠️ Ajustada em 2026-08-08 junto com §3/§4 — nomes de tipo (`IPasswordHasher`→`IPasswordService`), comportamento do `CommitAsync` e o estado real dos repositórios dos demais agregados estavam desatualizados nesta seção.

### Persistência (EF Core, SQL Server)
- `AppDbContext` (implementa `IUnitOfWork`): `DbSet` para todos os agregados (`Users`, `Customers`, `Vehicles`, `InventoryItems`, `Services`, `ServiceOrders`); `CommitAsync` roda `SaveChangesAsync` num `try/catch` **próprio** (se falhar, loga `LogError` e retorna `false` direto); só depois, **num `try/catch` separado**, entrega os eventos pendentes a `IDomainEventDispatcher.DispatchAsync` — se o dispatch falhar, só loga (não derruba o resultado do commit). O bug descrito numa versão anterior deste documento (um único `try/catch` em volta dos dois, fazendo falha de dispatch parecer falha de persistência) **já está corrigido** — ver §11.
- `UserModel` (`Repositories/Models/`): `Id`, `Email`, `Name`, `Password` (**`string`**, não `varbinary`/`byte[]` — refatorado em 2026-08-07), `Role`, `CreatedAt` (`DateTime`, não nulo), `UpdatedAt` (`DateTime?`). Vêm do domínio via `MapToModel`/`MapToDomain` — sem default de banco (`GETUTCDATE()`); o `User` é a fonte de verdade para essas datas.
- Mapeamento tabela `Users`: `Email` único (`HasIndex().IsUnique()`), `MaxLength(256)`; `Name` `MaxLength(100)`; `Password` `MaxLength(60)` (tamanho fixo de um hash BCrypt); `Role` `HasConversion<string>().HasMaxLength(20)`.
- `UserRepository : IUserRepository` — usa `AsNoTracking()` para leituras; mapeia Model↔Domain via `DomainMappers.MapToDomain`/`ModelMappers.MapToModel`, que chamam `new User(...)`/`new UserModel(...)` diretamente (**não existe `User.Rehydrate`**, ver §3 sobre o efeito colateral disso no `UserCreatedEvent`); enfileira eventos de domínio no `AppDbContext` (`EnqueueEvents`) só em `AddAsync`/`Update`, nunca em leituras.
- `CustomerRepository : ICustomerRepository` — mesmo molde de `UserRepository` (`AsNoTracking()` em leituras), mais `AnyAsync(string document, ...)` (`_context.Customers.AsNoTracking().AnyAsync(x => x.Document == document, ...)`), adicionado junto com o `CreateCustomer` use case (2026-08-11) — **primeiro repositório de negócio (fora `User`) a ganhar um método além do `IRepository<T>` base**, confirmando a política incremental (ver §9).
- **Os demais agregados já têm repositório completo implementado**, seguindo o mesmo molde de `UserRepository` (`GetByIdAsync`/`AddAsync`/`Update`/`Remove`, herdando de `Repository<T>`), mas ainda sem método extra: `VehicleRepository`, `InventoryItemRepository`, `ServiceRepository`, `ServiceOrderRepository` — todos registrados no DI (ver Composição abaixo). O que falta pra esses agregados é a camada de Application/Api por cima (use cases, endpoints), não a Infrastructure.
- **(2026-08-04)** Migrations antigas (`InitialCreate`, `AddRoleToUsers`, `AddPasswordToUsers`, `AddUpdatedAtToUsers`) apagadas; schema passou a nascer via `db/init.sql` (script SQL idempotente), executado pelo serviço `sqlserver-init` no `docker-compose.yml` **antes** da `api` subir (`depends_on: condition: service_completed_successfully`). `Program.cs` **não chama mais** `dbContext.Database.MigrateAsync()`. O mesmo `db/init.sql` é reaproveitado pelos testes de integração (`DatabaseFixture`, via `Link` no `.csproj`) — variável `$(DatabaseName)` (sintaxe `sqlcmd`) é substituída por `Fiap_Workshop` no compose e por `IntegrationTestsDb` via `string.Replace` no `DatabaseFixture`.
- **(2026-08-04)** Modelos de infra criados para todos os agregados do domínio (`CustomerModel`, `VehicleModel`, `InventoryItemModel`, `ServiceModel`, `ServiceOrderModel` + `ServiceOrderPartModel`/`ServiceOrderServiceModel`/`ServiceOrderStatusHistoryModel` como filhos, com `HasMany().WithOne().OnDelete(Cascade)`), com `ModelMappers`/`DomainMappers` e configuração completa em `AppDbContext.OnModelCreating` (índices únicos em `Document/Email/Phone` do Customer, `LicensePlate`, `Code`, `Number`; FKs explícitas entre agregados sem navegação — ex. `Vehicle.CustomerId → Customers` — via `HasOne<T>().WithMany().HasForeignKey().OnDelete(Restrict)`).
- **(2026-08-07)** Migration baseline **regenerada do zero** (`20260807031339_InitialCreate`, via `dotnet ef migrations add --project src/Fiap.Workshop.Infrastructure --startup-project src/Fiap.Workshop.Api`) cobrindo todos os agregados de uma vez — a pasta `Migrations/` existe só como apoio de ferramenta (gera SQL a partir do modelo real, útil pra conferir/gerar o `db/init.sql`), não é a fonte de verdade em runtime.
- `Fiap.Workshop.Api.csproj` também referencia `Microsoft.EntityFrameworkCore.Design` (necessário porque a Api é o startup project usado pela ferramenta `dotnet ef`).

### Serviços
- `JwtService : IJwtService` — assinatura mudou para `GenerateToken(User user)` (antes recebia `string userId, email, role` soltos, ver §4); gera token HS256 com claims `ClaimTypes.NameIdentifier` (`user.Id`), `ClaimTypes.Email`, `ClaimTypes.Role` (`user.Role.ToString()`); lê `Jwt:SecretKey/Issuer/Audience/ExpirationInMinutes` da configuração (expiração com fallback de 60 min se a config não for um `int` válido).
- `CurrentUserService : ICurrentUserService` — lê claims do `IHttpContextAccessor`.
- `PasswordService : IPasswordService` — `BCrypt.Net.BCrypt.HashPassword`/`Verify`, work factor 12. Registrado como `Singleton` (stateless). Ver §10 — não é um Domain Service aqui, é um serviço técnico de Application/Infrastructure.
- `DomainEventDispatcher : IDomainEventDispatcher` — resolve `IDomainEventHandler<TEvent>` via reflection (`serviceProvider.GetServices` + `MakeGenericType`) e invoca `HandleAsync` em cada handler registrado. **Nenhum handler concreto existe ainda no DI** — hoje o dispatch é um no-op. Ver §11.

### Composição (`IoC/DependencyInjection.cs`)
`AddInfrastructure()`:
- `AddRepositories`: registra `AppDbContext` (SqlServer via `ConnectionStrings:DefaultConnection`) e os 6 repositórios (`IUserRepository`, `ICustomerRepository`, `IVehicleRepository`, `IInventoryItemRepository`, `IServiceRepository`, `IServiceOrderRepository`) como `Scoped`.
- `AddServices`: `IPasswordService` (`Singleton`), `ICurrentUserService` (`Scoped`), `IJwtService` (`Scoped`).
- `IDomainEventDispatcher` (`Scoped`), `IHttpContextAccessor`.

## 6. Camada de API (`Fiap.Workshop.Api`)

> ⚠️ Reescrita em 2026-08-11 — desde a última passada (2026-08-08) ganhou o endpoint de login (`Auth`) e o primeiro endpoint de um agregado de negócio (`Customers`, criação). `GetUserById`/`UpdateEmail` continuam só como scaffold (sem endpoint/mapper/validator/use case).

Minimal APIs organizadas por feature em `Endpoints/{Feature}/*Endpoints.cs`, registradas em `Endpoints/EndpointsExtensions.cs` → `app.MapMinimalApisV1()`.

### Endpoints existentes

**Users** (`/api/v{version}/users`)
- `POST /` — `CreateUser`. Tem `.RequireAuthorization("AdminOnly")` — **exige um token JWT válido com role `Admin`**. Não há `.AllowAnonymous()`. Na prática isso continua sendo um problema de bootstrap real: sem nenhum Admin pré-existente (não há seed no `db/init.sql`) e sem `AllowAnonymous` em `CreateUser`, **não há caminho pela API pra criar o primeiro usuário** — o endpoint de login (abaixo) resolve "logar depois de ter um usuário", não "criar o primeiro usuário". Ver §9. Valida com `CreateUserRequestValidator` via `.WithValidation<CreateUserRequest>()`, chama `ICreateUserUseCase`. Retorna `201 Created` com o `Output` (contendo o `UserResponse` em `Result`) ou `400 BadRequest` com o `Output` (lista de erros).

**Auth** (`/api/v{version}/auth`) — novo desde a última reescrita
- `POST /login` — `LoginUser`. Tem `.AllowAnonymous()` — não exige autenticação (faz sentido, é o próprio ponto de entrada). Valida com `LoginRequestValidator` via `.WithValidation<LoginRequest>()`, chama `ILoginUserUseCase`. Retorna `200 OK` com o `Output` (token cru em `Result`) se válido, ou `401 Unauthorized` **sem corpo** se inválido (diferente do padrão `400 BadRequest` + `Output` usado nos outros endpoints — aqui o `Output`/lista de erros do use case é descartado).

**Customers** (`/api/v{version}/customers`) — novo, branch atual `feature/5-create-customer-uc`
- `POST /` — `CreateCustomer`. Tem `.RequireAuthorization("AttendantOnly")` (roles `Attendant`/`Admin`) — **primeiro endpoint a usar uma policy diferente de `AdminOnly`**. Valida com `CreateCustomerRequestValidator` via `.WithValidation<CreateCustomerRequest>()`, chama `ICreateCustomerUseCase`. Retorna `201 Created` (`Location: /api/v1/customers/{id}`, lendo o `Id` via `result.GetResult<Guid>()` — **atenção**: `Result` na verdade guarda um `CustomerResponse`, não um `Guid` cru; `GetResult<Guid>()` faz um cast direto que vai falhar em runtime se algum dia for exercitado — não coberto pelos testes de integração atuais, que chamam o use case direto, não o endpoint) ou `400 BadRequest` com o `Output`.

`CorrelationId` (`Guid`, `NotEmpty`) é um **campo do corpo** em todos os três requests (`CreateUserRequest`, `LoginRequest`, `CreateCustomerRequest`), não um header — não existe leitura de `x-correlation-id` em lugar nenhum do código atual. A inconsistência descrita em versões anteriores deste documento não existe mais: os três requests seguem o mesmo padrão.

Não existe grupo próprio nem rota para `UpdateEmailRequest` (`Requests/Users/`) — segue como record órfão, sem mapper, validator, use case ou rota.

### Requests / Validators / Mappers
- `CreateUserRequest(CorrelationId, Name, Email, Password, PasswordConfirmation, Role)` — validado por `CreateUserRequestValidator`: `CorrelationId` obrigatório; `Name` obrigatório (≤200); `Email` obrigatório + `.EmailAddress()` (sem limite de tamanho explícito no validator); `Password` obrigatório, mínimo 8 caracteres (**sem** checar maiúscula/minúscula/dígito); `PasswordConfirmation` igual a `Password`; `Role` dentro do enum (`Admin`/`Attendant`/`Mechanic`, ver §3). Não há nenhuma validação equivalente no Domain (ver §10) — hoje é só a API que garante isso, sem "defesa em profundidade". Mapper: `CreateUserMapper.MapToInput` (`Api/Mappers/`).
- `LoginRequest(CorrelationId, Email, Password)` — validado por `LoginRequestValidator`: `CorrelationId` obrigatório; `Email` obrigatório + `.EmailAddress()`; `Password` obrigatório, mínimo 8 caracteres. Mapper: `LoginUserMapper.MapToInput` (`Api/Mappers/`).
- `CreateCustomerRequest(CorrelationId, Name, Document, Email, Phone)` — validado por `CreateCustomerRequestValidator`: `CorrelationId` obrigatório; `Name` obrigatório (≤200); `Document` obrigatório, `Cascade(CascadeMode.Stop)` + tamanho 11 ou 14 + `IsValidDocument()` (dígito verificador real de CPF/CNPJ, via `CpfCnpjLibrary` — ver §2/§4); `Email` obrigatório + `.EmailAddress()`. **`Phone` não tem nenhuma regra de validação** (nem `NotEmpty`) — só existe validação de CPF/CNPJ, não de telefone, hoje. Mapper: `CreateCustomerMapper.MapToInput` (`Api/Mappers/`).
- `ValidationFilter<TRequest>` (`Filters/`) — `IEndpointFilter` genérico: resolve `IValidator<TRequest>` do DI, roda `ValidateAsync`, e se inválido retorna `400 BadRequest` com um `Output` (`Application.Commons`) preenchido via `AddErrorMessages`. Aplicado via `.WithValidation<TRequest>()` (`EndpointFilterExtensions`).

### Composição (`IoC/DependencyInjection.cs`)
`AddApi()`: `AddValidators` (registra `IValidator<CreateUserRequest>`, `IValidator<LoginRequest>`, `IValidator<CreateCustomerRequest>`, todos `Scoped`), `AddRoles` (`AddAuthorizationBuilder` com as três policies `AdminOnly`/`AttendantOnly`/`MechanicOnly`, ver §2 — hoje só `AdminOnly` (`CreateUser`) e `AttendantOnly` (`CreateCustomer`) são de fato exercitadas por um endpoint; `MechanicOnly` ainda não tem consumidor), `AddJwtConfig` (JWT Bearer lendo `Jwt:Issuer/Audience/SecretKey`, `AddApiVersioning` com URL segment reader e versão default 1 + `AddApiExplorer` com `SubstituteApiVersionInUrl = true`, `AddProblemDetails`, OpenAPI com security scheme Bearer). **Fix aplicado em 2026-08-11** (commit `fe845b2`, "fix(ioc): dependency injection bearer fix"): o documento OpenAPI declarava o `SecurityScheme` "Bearer" nos `Components`, mas não adicionava o `document.Security` global exigindo esse scheme — o botão "Authorize" do Swagger UI aparecia, mas o token informado não era de fato anexado às requisições de teste feitas pela UI. Corrigido adicionando `document.Security.Add(new OpenApiSecurityRequirement { [Bearer] = [] })` no mesmo `AddDocumentTransformer`.

### `Program.cs`
Pipeline: `AddControllers` (não usado, pois tudo é Minimal API — resquício de template), `AddEndpointsApiExplorer`, `AddApplication → AddInfrastructure → AddApi`, `MapOpenApi + UseSwaggerUI` (docs em `/swagger`, aponta pro JSON em `/openapi/v1.json`), `UseExceptionHandler`, `UseHttpsRedirection`, `UseAuthentication`, `UseAuthorization`, `UseSerilogRequestLogging`, `MapMinimalApisV1()`.

## 7. Docker / Deploy

- **Dockerfile**: multi-stage (`sdk:10.0` restore → publish → `aspnet:10.0` runtime), copia apenas os `.csproj` primeiro (cache de layers), expõe porta `8080`.
- **docker-compose.yml**: serviços `api` (build local) + `sqlserver` (`mssql/server:2022-latest`) + `sqlserver-init` (2026-08-03, novo), healthcheck via `sqlcmd`, volume nomeado `sqlserver-data`. Variáveis vêm do `.env`.
- **`sqlserver-init`** (2026-08-03): serviço descartável (`restart: "no"`) que roda a mesma imagem `mssql/server:2022-latest` com `entrypoint` sobrescrito pra executar `/opt/mssql-tools18/bin/sqlcmd -i /init.sql` (monta `db/init.sql` como volume) contra o serviço `sqlserver` assim que ele fica `healthy`. `api` agora depende de `sqlserver-init` com `condition: service_completed_successfully` (não mais de `sqlserver: service_healthy` diretamente) — o banco já está com schema pronto antes da API subir.
- **docker-compose.tests.yml**: SQL Server isolado (`integration-sql`) para os testes de integração, senha fixa `Integration@Test123`. **Não** tem serviço de init próprio — quem aplica o schema é a `DatabaseFixture` (C#), lendo o mesmo `db/init.sql` compartilhado.
- **.env / .env.example** (variáveis, sem valores sensíveis aqui): `ASPNETCORE_ENVIRONMENT`, `API_PORT`, `SA_PASSWORD`, `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationInMinutes`. ⚠️ `.env` está versionado com uma senha/segredo de exemplo — confirmar se deveria estar no `.gitignore`. Nome do database (`Fiap_Workshop`) está hardcoded tanto na connection string quanto no `-v DatabaseName=Fiap_Workshop` do `sqlserver-init` — se um dia virar variável, precisa mudar nos dois lugares.
- `db/init.sql` (2026-08-03, movido de `tests/Fiap.Workshop.IntegrationTests/sql/init.sql`): schema completo (todas as tabelas, criado nesta sessão) e única fonte de verdade, usado tanto pelo `sqlserver-init` do compose normal quanto pelos testes de integração (via `<None Include="..\..\db\init.sql" Link="sql\init.sql">` no `.csproj`, copiado pro output). Precisa ser mantido manualmente em sincronia com `AppDbContext.OnModelCreating` sempre que o modelo mudar (não há geração automática a partir das migrations).

## 8. CI/CD (GitHub Actions)

- `ci-feature.yml` — dispara em push para `feature/**`: build → unit tests → integration tests (sobe `docker-compose.tests.yml`) → functional tests (projeto sem testes reais ainda, portanto sempre "passa" vazio).
- `ci-develop.yml` — mesmo pipeline de `ci-feature.yml` + job final `open-release-pr` que abre PR automático `develop → release` se a branch `release` existir.
- `ci-release.yml` — dispara em push para `release`: build → job `sonarqube-cloud` (ver abaixo).
- `codeql.yml` — análise de segurança estática (não inspecionado em detalhe).
- Não há workflow para `main` ainda.

### SonarQube Cloud (job `sonarqube-cloud`, em `ci-release.yml`) — adicionado em 2026-07-27, movido de `develop` para `release` no mesmo dia

Decisão: SonarQube Cloud (ex-SonarCloud — Sonar renomeou o produto em 2024, mesmo serviço, mesmo domínio `sonarcloud.io`), plano gratuito de repositório público, em vez de SonarQube Server self-hosted (exigiria manter um container/servidor rodando).

- **Por que só em `release` e não em `develop`/`feature/**`**: o **Free plan** do SonarQube Cloud só analisa a *main branch* configurada no projeto (que aqui é `release`, a branch principal do repo no GitHub) — tentar rodar em `develop` deu o erro "Organization is not allowed to access data from non main branches". Existe um **Free plan OSS** com branch analysis ilimitado pra organizações open source (repo é público, se qualificaria via `Administration → Billing` no SonarQube Cloud), mas decidiu-se não perseguir isso agora e só rodar a análise quando o código chega em `release`.
- **Escopo da análise**: restrito a `src/Fiap.Workshop.Domain/**` e `src/Fiap.Workshop.Application/**` via `/d:sonar.inclusions=...` no `dotnet sonarscanner begin` — Infrastructure/Api/tests ficam fora de propósito, já que o objetivo é vigiar a qualidade das camadas de regra de negócio (alinhado ao requisito de SAST/cobertura do Tech Challenge, ver §9 item 7).
- **Cobertura**: gerada com `coverlet.console` (global tool) envolvendo `dotnet test` do projeto `Fiap.Workshop.UnitTests` (é o projeto que cobre Domain e Application isoladamente, ver §1), formato OpenCover, reportado via `/d:sonar.cs.opencover.reportsPaths="coverage.xml"`. Integration/Functional tests não entram na cobertura reportada ao Sonar.
- **Projeto no SonarQube Cloud**: criado com "Use existing CI configuration" (não "Automatic Analysis") — necessário pra manter o `sonar.inclusions` funcionando; análise automática do Sonar ignora esse tipo de configuração custom.
- **Project key** (`/k:`): `HumbertoVitalino_fiap-postgraduated-workshop`; **organization** (`/o:`): `humbertovitalino` — hardcoded no step `Begin analysis`, não são secrets (só o token é sensível).
- **Secret necessário**: `SONAR_TOKEN` (GitHub → Settings → Secrets and variables → Actions), gerado em SonarQube Cloud (`My Account → Security`). Sem esse secret o job falha no `begin`/`end`.
- Requer `actions/setup-java` (JDK 17, Temurin) além do `.NET 10.x` já usado no resto da pipeline, porque o `dotnet-sonarscanner` roda sobre JVM.

## 9. Pendências conhecidas

> ⚠️ Lista revisada em 2026-08-11 contra o código real (branch `feature/5-create-customer-uc`) — login já existe, `Customer` já tem use case/endpoint, mas o problema de bootstrap de Admin continua sem solução.

1. **Bootstrap de Admin (ainda sem solução)**: `POST /users` exige `RequireAuthorization("AdminOnly")`, sem `AllowAnonymous`, e não há seed de usuário Admin em `db/init.sql`. O endpoint de login (`POST /api/v1/auth/login`) já existe (desde a última reescrita) e funciona para autenticar um usuário **que já existe**, mas não resolve o problema original: **ainda não há nenhum caminho pela API para criar o primeiro usuário** (login pressupõe um usuário previamente criado por outro meio — direto no banco, hoje). Precisa de uma decisão: seed de Admin no `init.sql`, endpoint de bootstrap protegido por secret, ou tornar `CreateUser` público e restringir `Role: Admin` na validação/regra de negócio.
2. **`GetResult<Guid>()` no endpoint `CreateCustomer` provavelmente quebra em runtime** (novo, 2026-08-11): `CustomersEndpoints.MapPost` monta o `Location` do `201 Created` com `result.GetResult<Guid>()`, mas `Output.Result` guarda um `CustomerResponse` (não um `Guid` cru) — `GetResult<T>()` faz um cast direto (`(T?)Result`), que deveria lançar `InvalidCastException` nesse caminho. Não foi pego porque não há teste de integração/funcional batendo no endpoint HTTP (só no use case direto, ver `CreateCustomerUseCaseIntegrationTests`) — `Fiap.Workshop.FunctionalTests` continua vazio (ver item 5). Vale conferir se `CreateUser`/`Auth` têm o mesmo problema antes de assumir que é só do `Customer` (`UsersEndpoints` também usa `GetResult<Guid>()` do mesmo jeito).
3. **`.env` versionado no git** com segredos de exemplo (SA_PASSWORD, Jwt SecretKey) — confirmar se é intencional para o workshop ou se deveria ir para `.gitignore`.
4. **Mocking duplicado nos testes**: `Fiap.Workshop.UnitTests.csproj` referencia tanto `Moq` quanto `NSubstitute`; os testes atuais usam apenas `Moq`. Vale decidir um padrão único.
5. **`Fiap.Workshop.FunctionalTests`** existe como projeto mas não tem nenhum arquivo de teste ainda — scaffold vazio. Sem ele, bugs de composição HTTP (ver item 2) não são pegos por nenhuma camada de teste.
6. **Domain events sem consumidor**: mecânica pronta e correta (ver §11), mas nenhum `IDomainEventHandler` está registrado no DI — o dispatch acontece e não aciona nada. E-mail de boas-vindas planejado como primeiro handler, ver roadmap em §11. `Customer` nem dispara evento próprio ainda (ver §3).
7. **Escopo do Tech Challenge (Fase 1) — progresso desde a última passada**: o enunciado (`15SOAT - Fase 1 - Tech Challenge (1).pdf`, na raiz) pede um sistema de oficina mecânica — CRUD de clientes/veículos/serviços/peças (com controle de estoque), Ordem de Serviço com máquina de estados, orçamento automático, validação de CPF/CNPJ e placa, cobertura de teste mínima de 80% nos domínios críticos, relatório de vulnerabilidades (SAST) e documentação DDD.
   - **Cliente**: agora tem pilha completa até a criação (`CreateCustomer` — Domain→Application→Infrastructure→Api, ver §3/§4/§6), incluindo validação de CPF/CNPJ (dígito verificador via `CpfCnpjLibrary`, ver §2/§6). Ainda falta consulta/atualização/remoção de cliente.
   - **Veículo, Serviço, Peça/Insumo, Ordem de Serviço**: continuam só com Domain e Infrastructure prontos (entidades, models, repositórios, mapeamentos, migration) — falta a camada de Application (use cases) e Api (endpoints) por cima deles.
   - **Ainda faltando por completo**: validação de placa, máquina de estados de OS, orçamento automático, controle de estoque, cobertura de 80%, relatório de vulnerabilidades (SAST), documentação DDD.
   - Ver seção "Roadmap / pendências do desafio" do `README.md` para a lista cobrada pelo enunciado — **está desatualizada** (ainda diz que login e `Customer` não existem); vale revisar numa próxima passada.

## 10. Senha em `User` — desenho atual (reescrito em 2026-08-08)

> Esta seção descrevia anteriormente um desenho DDD (VO `HashedPassword`, `IPasswordHasher` como Domain Service, fábricas `Create`/`Rehydrate`) concluído numa iteração anterior do projeto. **Esse desenho não está presente no código desta branch** (`feature/3-create-user-uc`) — o que segue é o que existe de fato hoje.

- `User.Password` é uma `string` comum — sem Value Object, sem política de força aplicada no Domain (`UserErrors.PasswordTooShort`/`PasswordMissingUppercase`/etc. existem mas não são referenciadas por nenhum código).
- `IPasswordService` (não `IPasswordHasher`) mora em `Application.Interfaces.Services` — hashear senha é tratado como serviço técnico, não como invariante do agregado `User`. Implementação: `PasswordService` (Infrastructure), `BCrypt.Net-Next`, work factor 12, registrada como `Singleton`.
- `User` não hasheia nem valida a própria senha. Quem orquestra é `CreateUserMapper.MapToDomain` (Application): recebe o hash já pronto (`IPasswordService.Hash(input.Password)` calculado no use case) e só então constrói o `User`.
- Não existem `Create`/`Rehydrate`: o único construtor de `User` serve tanto para criar quanto para reidratar do banco, e sempre dispara `UserCreatedEvent` (ver §3).
- Validação de senha hoje só existe na Api (`CreateUserRequestValidator`/`LoginRequestValidator`: `NotEmpty().MinimumLength(8)`, sem checar maiúscula/minúscula/dígito) — não há "defesa em profundidade" replicada no Domain como a antiga versão deste documento descrevia.
- **`LoginUserUseCase` já existe** (desde a última reescrita deste documento, ver §4/§6) e usa `IPasswordService.Verify` para checar a senha contra o hash guardado — `LoginResponse` (DTO) segue existindo mas não é usado; o use case devolve o token JWT como `string` cru.

Se o time decidir reintroduzir Value Objects/fábricas nomeadas para `User`, é um redesenho consciente a ser feito — não é o estado atual.

## 11. Domain Events — mecânica atual (importante entender antes de adicionar handlers)

> ⚠️ Ajustada em 2026-08-08: a versão anterior descrevia um bug no `CommitAsync` que **já foi corrigido** no código atual, e referenciava `AggregateRoot<TId>`/`User.Create`, que não existem mais (ver §3).

Como funciona hoje, de ponta a ponta:

1. O construtor de `User(...)` chama `RaiseDomainEvent(new UserCreatedEvent(id))` — isso só adiciona o evento a uma `List<IDomainEvent>` **em memória**, dentro do próprio objeto `User` (`AggregateRoot._domainEvents`). Isso acontece **toda vez** que um `User` é instanciado — criação de verdade (`CreateUserUseCase`) ou reidratação a partir do banco (`DomainMappers.MapToDomain`) — não existe um caminho separado para "isso já existia" (ver §3).
2. `UserRepository.AddAsync`/`Update` movem esses eventos para dentro do `AppDbContext` (`EnqueueDomainEvents`) e limpam a lista do `User` (`EnqueueEvents`, em `Repository<T>`). **Leituras (`GetByIdAsync`/`GetByEmailAsync`) não chamam `EnqueueEvents`** — o `UserCreatedEvent` gerado ao reidratar um usuário existente é descartado junto com o objeto, nunca chega a ser enfileirado. Frágil (ver §3), mas inofensivo hoje.
3. `AppDbContext.CommitAsync` roda `SaveChangesAsync` num `try/catch` **próprio** (grava o `UserModel` no SQL Server; se falhar, loga e retorna `false` direto). **Só depois**, num `try/catch` **separado**, entrega os eventos pendentes ao `IDomainEventDispatcher.DispatchAsync` — uma falha aqui só é logada, não afeta o `bool` retornado.
4. `DomainEventDispatcher` resolve via reflection quais `IDomainEventHandler<TEvent>` estão registrados no DI e invoca `HandleAsync` em cada um, **de forma síncrona**, ainda dentro da mesma requisição/antes de responder ao cliente.

**Conclusões importantes:**
- **Eventos de domínio NÃO são persistidos em lugar nenhum.** Não existe tabela de eventos, fila ou log. É puramente um mecanismo de notificação em memória, vivo só durante aquela unit of work.
- Isso **não é Event Sourcing** (o estado não é reconstruído replayando eventos — vive direto na tabela `Users`) nem **Transactional Outbox** (não há garantia de entrega; se o processo cair entre o `SaveChangesAsync` e o `DispatchAsync`, o evento se perde silenciosamente).
- **Hoje não existe nenhum `IDomainEventHandler` registrado no DI** — o dispatch acontece mas não aciona nada. Toda a infraestrutura existe, mas está inerte.
- **O bug de "falha no dispatch derruba o resultado do commit", descrito numa versão anterior deste documento, já está corrigido**: `SaveChangesAsync` e `DispatchAsync` têm `try/catch` independentes (passo 3 acima) — não é mais um pré-requisito bloqueando a adição de handlers.

### Plano para a próxima sessão: e-mail de boas-vindas ao criar usuário

Discutido e ainda não implementado — retomar aqui (já foi prototipado uma vez como exercício de compreensão e revertido de propósito; o desenho abaixo continua válido, com o item 1 original já resolvido):

1. ~~Corrigir o bug do `CommitAsync`~~ — **já corrigido** (ver acima), não bloqueia mais nada.
2. **Enriquecer `UserCreatedEvent`**: hoje só tem `UserId`; para mandar e-mail é preciso `Email`/`Name`. Decisão tomada: preferir "evento gordo" (`UserCreatedEvent(Guid UserId, string Email, string Name)`) em vez de o handler re-consultar o `IUserRepository` — mais barato e captura o estado exatamente como era no momento da criação.
3. **`IEmailService`** — nova interface em `Application/Interfaces/Services/`, algo como `SendWelcomeEmailAsync(string email, string name)`.
4. **Implementação em Infrastructure**: como não há provedor de e-mail configurado (`.env` não tem SMTP/SendGrid), começar com um `LoggingEmailService` que só loga o envio — ponto de extensão pronto para um provedor real depois.
5. **Handler**: `SendWelcomeEmailOnUserCreated : IDomainEventHandler<UserCreatedEvent>` — colocar em **Application** (não Infrastructure), já que só orquestra uma chamada a uma abstração (`IEmailService`), sem detalhe técnico nenhum. Registrar no DI de `AddApplication()`.
6. Ponderar (não decidido ainda): o dispatch síncrono faz o `POST /users` esperar o "envio" do e-mail antes de responder. Para um provedor de e-mail real (chamada HTTP externa), isso adiciona latência à resposta. Para o workshop, começar síncrono é aceitável — mas vale registrar que, se algum dia isso incomodar, a evolução natural é official Outbox pattern + worker em background, não o dispatcher atual.
7. **Testar em 3 camadas**: unit test em `User` (evento carrega os dados certos), unit test no handler (mock de `IEmailService`), integration test ponta a ponta (spy de `IEmailService` sobrescrevendo `LoggingEmailService` via DI no `DatabaseFixture`, criando usuário via `ICreateUserUseCase` de verdade). Esse desenho de teste em camadas já foi validado no protótipo revertido.

## 12. `Email` em `User` — nunca foi Value Object nesta branch (nota reescrita em 2026-08-08)

> Esta seção descrevia anteriormente a remoção de um VO `Email` (`Domain/Users/Email.cs`) numa iteração anterior do projeto. Nesta branch (`feature/3-create-user-uc`), `Email` **nunca existiu como Value Object** — sempre foi `string` simples em `User`, sem normalização (sem `Trim()`/`ToLowerInvariant()` em nenhum lugar do código atual). Registrado aqui só para não perder o contexto de que essa decisão (VO vs. string simples) já foi discutida antes; ver §10 para o desenho atual completo de `User`.

Validação de formato/tamanho de e-mail hoje só existe em `CreateUserRequestValidator` (Api): `NotEmpty().EmailAddress()`, sem limite de tamanho explícito (o `MaxLength(256)` é só na coluna do banco, ver §5). Duplicidade é garantida pelo índice único `IX_Users_Email` no SQL Server + `ExistsWithEmailAsync` checado antes do insert — mas como não há normalização, `"joao@x.com"` e `"JOAO@x.com"` são tratados como e-mails diferentes hoje.
