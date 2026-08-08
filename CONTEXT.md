# Fiap.Workshop — Contexto do Projeto

> Documento vivo de contexto técnico. Atualize sempre que a arquitetura, os use cases ou as decisões de design mudarem. Última atualização: 2026-08-08.

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
- **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), policies `AdminOnly` e `UserOnly`
- **Hashing de senha**: `BCrypt.Net-Next` (work factor 12), via `IPasswordService` (`Application.Interfaces.Services`) / `PasswordService` (Infrastructure) — ver §10 para a diferença em relação a uma iteração anterior do projeto que tinha isso como Domain Service
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

### `UserRole` (enum): `User = 0`, `Admin = 1`

### `UserErrors` (constantes de mensagem, hoje **não usadas por nenhum use case** — sobras de uma iteração anterior)
`NameEmpty`, `NotFound`, `EmailAlreadyInUse`, `PasswordEmpty`, `PasswordTooShort`, `PasswordMissingUppercase`, `PasswordMissingLowercase`, `PasswordMissingDigit`, `PasswordHashEmpty`, `InvalidCredentials`.

### Eventos (`Events/`)
- `UserCreatedEvent(Guid UserId)` — disparado (sempre) no construtor de `User`, ver acima. Só carrega o `UserId` ("evento magro"). Ver §11 para a mecânica completa de domain events.

### Outros agregados (existem, ainda não documentados em detalhe aqui)
`Customer`, `Vehicle`, `Service`, `InventoryItem`, `ServiceOrder` (+ `ServiceOrderPart`/`ServiceOrderService`/`ServiceOrderStatusHistory` como filhos do mesmo agregado), com seus próprios `Errors/ServiceOrderErrors.cs` e enums (`UnitOfMeasure`, `ServiceOrderStatus`). Já têm `Model`/`Repository`/mapeamentos completos em Infrastructure (ver §5), mas ainda não são expostos via Application/Api — desatualiza o item 7 de §9, que precisa de uma revisão futura.

## 4. Camada de Aplicação (`Fiap.Workshop.Application`)

> ⚠️ Reescrita em 2026-08-08 junto com §3 — reflete só o que existe hoje. `GetUserByIdUseCase`/`LoginUseCase`/`UpdateEmailUseCase` descritos numa versão anterior deste documento **não existem no código** (só sobraram `Requests` HTTP scaffoldadas na Api sem use case/endpoint por trás, ver abaixo).

Padrão de use case: **Input (record) → UseCase (classe) → Output (classe genérica com Result/Errors)**, sem MediatR — injeção direta de interfaces de use case.

### `Commons/Output.cs`
Classe de retorno padrão de todos os use cases: `IsValid`, `Messages`, `ErrorMessages`, `Result` (object, lido via `GetResult<T>()`), métodos `AddResult`, `AddMessage`, `AddErrorMessage(s)`.

### Use case existente (`UseCases/CreateUser/`)

`CreateUserUseCase` (interface `ICreateUserUseCase`), input `CreateUserInput(CorrelationId, Name, Email, Password, Role)`:
1. Checa e-mail único via `IUserRepository.ExistsWithEmailAsync` — se já existe, loga `LogWarning` e retorna `Output` com erro (`"User with email {Email} already exists."`), sem tocar no repositório.
2. Hasheia a senha via `IPasswordService.Hash(input.Password)`.
3. Monta o `User` via `input.MapToDomain(passwordHash)` e persiste (`AddAsync` + `UnitOfWork.CommitAsync`).
4. Se o commit falhar (`CommitAsync` retorna `false`), loga `LogError` e retorna erro (`"Error saving user with email {Email}."`).
5. Em sucesso, `output.AddResult(user)` — **retorna a entidade `User` crua**, não um DTO (`UserResponse` existe mas não é usado aqui, ver DTOs abaixo).

Recebe `ILogger<CreateUserUseCase>` no construtor. Cobertura de teste (adicionada em 2026-08-08): 3 cenários unitários (sucesso, e-mail duplicado, falha ao salvar) em `CreateUserUseCaseUnitTests`, usando o helper `LoggerTestBase<TCategory>` (`tests/Fiap.Workshop.UnitTests/Common/`) para verificar `ILogger.Log(...)` sem tropeçar no fato de `LogWarning`/`LogError` serem extension methods (Moq não consegue mockar/verificar extension methods diretamente — é preciso verificar o `ILogger.Log<TState>` real por trás). Mais 2 cenários de integração (sucesso, e-mail duplicado) em `CreateUserUseCaseIntegrationTests`, batendo no SQL Server real via `DatabaseFixture`.

Cada use case tem uma pasta `Boundaries/` (Input) e `Mapper/` (mapeamento entre camadas).

### Mapper de criação (`UseCases/CreateUser/Mapper/CreateUserMapper.cs`)
- `MapToDomain(this CreateUserInput input, string passwordHash)`: `new User(input.CorrelationId, input.Email, input.Name, passwordHash, input.Role, DateTime.Now, DateTime.Now)`. Usa **`input.CorrelationId` como `Id` do usuário** (não gera um novo `Guid`). **Bug corrigido em 2026-08-08**: a chamada passava `input.Name`/`input.Email` na ordem trocada em relação ao construtor de `User` (que espera `email` antes de `name`) — todo usuário criado ficava com `Email`/`Name` invertidos. Só foi pego pelo teste de integração contra o SQL Server real (violação do índice único em `Email`); os testes unitários não afirmavam sobre os valores dos campos, então não detectaram.

### Interfaces / portas (`Interfaces/`)
- `Repositories/IUnitOfWork.CommitAsync()`
- `Repositories/IUserRepository : IRepository<User>` — `GetByEmailAsync(string email, ...)`, `ExistsWithEmailAsync(string email, ...)`
- `Abstractions/IRepository<T> where T : AggregateRoot` — **sem segundo parâmetro `TId`** (assume `Guid` implicitamente): `UnitOfWork`, `GetByIdAsync`, `AddAsync`, `Update`, `Remove`. Outras portas de repositório já existem seguindo o mesmo molde: `ICustomerRepository`, `IVehicleRepository`, `IInventoryItemRepository`, `IServiceRepository`, `IServiceOrderRepository` (implementações em Infrastructure, ver §5, mas sem use case/endpoint consumindo ainda).
- `Services/ICurrentUserService` — `UserId` (`Guid`), `Email` (`string`), `Role` (`string`), `IsAuthenticated` (lido do `ClaimsPrincipal`)
- `Services/IDomainEventDispatcher.DispatchAsync(events, cancellationToken)`
- `Services/IJwtService.GenerateToken(string userId, string email, string role)` — parâmetros `string`, não fortemente tipados
- `Services/IPasswordService` — `Hash(string rawPassword)`, `Verify(string rawPassword, string hash)`. Mora em `Application.Interfaces.Services` (não em Domain) — ver nota em §10 sobre essa diferença em relação a uma iteração anterior do projeto.
- `Abstractions/IDomainEventHandler<TEvent>` — infraestrutura de handlers de evento de domínio (nenhum handler concreto implementado ainda — ver §11)

### DTOs (`DTOs/Users/`)
- `UserResponse(Id, Name, Email, Role)` com `FromUser(User)` estático — **existe mas não é usado** por `CreateUserUseCase` hoje (que devolve o `User` cru, ver acima).
- `LoginResponse(Token)` — existe sem nenhum `LoginUseCase` correspondente (só o `LoginRequest` foi scaffoldado na Api, ver §6).

### Composição (`IoC/DependencyInjection.cs`)
`AddApplication()` registra só `ICreateUserUseCase` como `Scoped`.

## 5. Camada de Infraestrutura (`Fiap.Workshop.Infrastructure`)

> ⚠️ Ajustada em 2026-08-08 junto com §3/§4 — nomes de tipo (`IPasswordHasher`→`IPasswordService`), comportamento do `CommitAsync` e o estado real dos repositórios dos demais agregados estavam desatualizados nesta seção.

### Persistência (EF Core, SQL Server)
- `AppDbContext` (implementa `IUnitOfWork`): `DbSet` para todos os agregados (`Users`, `Customers`, `Vehicles`, `InventoryItems`, `Services`, `ServiceOrders`); `CommitAsync` roda `SaveChangesAsync` num `try/catch` **próprio** (se falhar, loga `LogError` e retorna `false` direto); só depois, **num `try/catch` separado**, entrega os eventos pendentes a `IDomainEventDispatcher.DispatchAsync` — se o dispatch falhar, só loga (não derruba o resultado do commit). O bug descrito numa versão anterior deste documento (um único `try/catch` em volta dos dois, fazendo falha de dispatch parecer falha de persistência) **já está corrigido** — ver §11.
- `UserModel` (`Repositories/Models/`): `Id`, `Email`, `Name`, `Password` (**`string`**, não `varbinary`/`byte[]` — refatorado em 2026-08-07), `Role`, `CreatedAt` (`DateTime`, não nulo), `UpdatedAt` (`DateTime?`). Vêm do domínio via `MapToModel`/`MapToDomain` — sem default de banco (`GETUTCDATE()`); o `User` é a fonte de verdade para essas datas.
- Mapeamento tabela `Users`: `Email` único (`HasIndex().IsUnique()`), `MaxLength(256)`; `Name` `MaxLength(100)`; `Password` `MaxLength(60)` (tamanho fixo de um hash BCrypt); `Role` `HasConversion<string>().HasMaxLength(20)`.
- `UserRepository : IUserRepository` — usa `AsNoTracking()` para leituras; mapeia Model↔Domain via `DomainMappers.MapToDomain`/`ModelMappers.MapToModel`, que chamam `new User(...)`/`new UserModel(...)` diretamente (**não existe `User.Rehydrate`**, ver §3 sobre o efeito colateral disso no `UserCreatedEvent`); enfileira eventos de domínio no `AppDbContext` (`EnqueueEvents`) só em `AddAsync`/`Update`, nunca em leituras.
- **Todos os demais agregados já têm repositório completo implementado**, seguindo o mesmo molde de `UserRepository` (`GetByIdAsync`/`AddAsync`/`Update`/`Remove`, herdando de `Repository<T>`): `CustomerRepository`, `VehicleRepository`, `InventoryItemRepository`, `ServiceRepository`, `ServiceOrderRepository` — todos registrados no DI (ver Composição abaixo). O que falta pra esses agregados é a camada de Application/Api por cima (use cases, endpoints), não a Infrastructure.
- **(2026-08-04)** Migrations antigas (`InitialCreate`, `AddRoleToUsers`, `AddPasswordToUsers`, `AddUpdatedAtToUsers`) apagadas; schema passou a nascer via `db/init.sql` (script SQL idempotente), executado pelo serviço `sqlserver-init` no `docker-compose.yml` **antes** da `api` subir (`depends_on: condition: service_completed_successfully`). `Program.cs` **não chama mais** `dbContext.Database.MigrateAsync()`. O mesmo `db/init.sql` é reaproveitado pelos testes de integração (`DatabaseFixture`, via `Link` no `.csproj`) — variável `$(DatabaseName)` (sintaxe `sqlcmd`) é substituída por `Fiap_Workshop` no compose e por `IntegrationTestsDb` via `string.Replace` no `DatabaseFixture`.
- **(2026-08-04)** Modelos de infra criados para todos os agregados do domínio (`CustomerModel`, `VehicleModel`, `InventoryItemModel`, `ServiceModel`, `ServiceOrderModel` + `ServiceOrderPartModel`/`ServiceOrderServiceModel`/`ServiceOrderStatusHistoryModel` como filhos, com `HasMany().WithOne().OnDelete(Cascade)`), com `ModelMappers`/`DomainMappers` e configuração completa em `AppDbContext.OnModelCreating` (índices únicos em `Document/Email/Phone` do Customer, `LicensePlate`, `Code`, `Number`; FKs explícitas entre agregados sem navegação — ex. `Vehicle.CustomerId → Customers` — via `HasOne<T>().WithMany().HasForeignKey().OnDelete(Restrict)`).
- **(2026-08-07)** Migration baseline **regenerada do zero** (`20260807031339_InitialCreate`, via `dotnet ef migrations add --project src/Fiap.Workshop.Infrastructure --startup-project src/Fiap.Workshop.Api`) cobrindo todos os agregados de uma vez — a pasta `Migrations/` existe só como apoio de ferramenta (gera SQL a partir do modelo real, útil pra conferir/gerar o `db/init.sql`), não é a fonte de verdade em runtime.
- `Fiap.Workshop.Api.csproj` também referencia `Microsoft.EntityFrameworkCore.Design` (necessário porque a Api é o startup project usado pela ferramenta `dotnet ef`).

### Serviços
- `JwtService : IJwtService` — gera token HS256 com claims `NameIdentifier`, `Email`, `Role`; lê `Jwt:SecretKey/Issuer/Audience/ExpirationInMinutes` da configuração.
- `CurrentUserService : ICurrentUserService` — lê claims do `IHttpContextAccessor`.
- `PasswordService : IPasswordService` — `BCrypt.Net.BCrypt.HashPassword`/`Verify`, work factor 12. Registrado como `Singleton` (stateless). Ver §10 — não é um Domain Service aqui, é um serviço técnico de Application/Infrastructure.
- `DomainEventDispatcher : IDomainEventDispatcher` — resolve `IDomainEventHandler<TEvent>` via reflection (`serviceProvider.GetServices` + `MakeGenericType`) e invoca `HandleAsync` em cada handler registrado. **Nenhum handler concreto existe ainda no DI** — hoje o dispatch é um no-op. Ver §11.

### Composição (`IoC/DependencyInjection.cs`)
`AddInfrastructure()`:
- `AddRepositories`: registra `AppDbContext` (SqlServer via `ConnectionStrings:DefaultConnection`) e os 6 repositórios (`IUserRepository`, `ICustomerRepository`, `IVehicleRepository`, `IInventoryItemRepository`, `IServiceRepository`, `IServiceOrderRepository`) como `Scoped`.
- `AddServices`: `IPasswordService` (`Singleton`), `ICurrentUserService` (`Scoped`), `IJwtService` (`Scoped`).
- `IDomainEventDispatcher` (`Scoped`), `IHttpContextAccessor`.

## 6. Camada de API (`Fiap.Workshop.Api`)

> ⚠️ Reescrita em 2026-08-08 — só existe hoje o endpoint de criação de usuário. `GetUserById`/`UpdateEmail`/`Login` descritos numa versão anterior deste documento não têm endpoint, mapper, validator nem use case implementados; só sobraram os records `LoginRequest`/`UpdateEmailRequest` órfãos.

Minimal APIs organizadas por feature em `Endpoints/{Feature}/*Endpoints.cs`, registradas em `Endpoints/EndpointsExtensions.cs` → `app.MapMinimalApisV1()`.

### Endpoint existente

**Users** (`/api/v{version}/users`)
- `POST /` — `CreateUser`. Tem `.RequireAuthorization("AdminOnly")` — **exige um token JWT válido com role `Admin`**. Não há `.AllowAnonymous()` em lugar nenhum (diferente do que uma versão anterior deste documento descrevia como "cadastro público" com policy de grupo sobrescrita — hoje não existe grupo com policy própria, a checagem é só no endpoint mesmo). Na prática isso é um problema de bootstrap real: sem nenhum Admin pré-existente (não há seed no `db/init.sql`) e sem endpoint de login implementado, **não há caminho pela API pra criar o primeiro usuário** — ver §9. Valida com `CreateUserRequestValidator` via `.WithValidation<CreateUserRequest>()`, chama `ICreateUserUseCase`. Retorna `201 Created` (sem corpo) ou `400 BadRequest` com o `Output` (lista de erros).

Não existe grupo `Auth` nem qualquer outro endpoint — `LoginRequest` (`Requests/Auth/`) e `UpdateEmailRequest` (`Requests/Users/`) são records sem mapper, validator, use case ou rota.

`CorrelationId` (`Guid`, `NotEmpty`) é um **campo do corpo** de `CreateUserRequest`, não um header — não existe leitura de `x-correlation-id` em lugar nenhum do código atual.

### Requests / Validators / Mappers
- `CreateUserRequest(CorrelationId, Name, Email, Password, PasswordConfirmation, Role)` — validado por `CreateUserRequestValidator`: `CorrelationId` obrigatório; `Name` obrigatório (≤200); `Email` obrigatório + `.EmailAddress()` (sem limite de tamanho explícito no validator); `Password` obrigatório, mínimo 8 caracteres (**sem** checar maiúscula/minúscula/dígito); `PasswordConfirmation` igual a `Password`; `Role` dentro do enum. Não há nenhuma validação equivalente no Domain (ver §10) — hoje é só a API que garante isso, sem "defesa em profundidade".
- `CreateUserMapper.MapToInput(this CreateUserRequest request)` (`Api/Mappers/`) — `CreateUserRequest → CreateUserInput`.
- `ValidationFilter<TRequest>` (`Filters/`) — `IEndpointFilter` genérico: resolve `IValidator<TRequest>` do DI, roda `ValidateAsync`, e se inválido retorna `400 BadRequest` com um `Output` (`Application.Commons`) preenchido via `AddErrorMessages`. Aplicado via `.WithValidation<TRequest>()` (`EndpointFilterExtensions`).

### Composição (`IoC/DependencyInjection.cs`)
`AddApi()`: JWT Bearer (lendo `Jwt:Issuer/Audience/SecretKey`), `AddApiVersioning` (URL segment reader, versão default 1) + `AddApiExplorer` (`SubstituteApiVersionInUrl = true`), `AddAuthorizationBuilder` com policies `AdminOnly` (role `Admin`) e `UserOnly` (roles `User`/`Admin`) — **nenhuma das duas é de fato exercitada por um fluxo completo hoje**, já que o único endpoint (`CreateUser`) exige `AdminOnly` mas não há como conseguir um token de Admin (ver observação de bootstrap acima), `AddProblemDetails`, registra só `IValidator<CreateUserRequest>` como `Scoped` (não "os `*RequestValidator`" — só existe esse um), configura OpenAPI com security scheme Bearer.

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

> ⚠️ Lista revisada em 2026-08-08 contra o código real — vários itens abaixo tinham premissas desatualizadas (endpoints que não existem, bug já corrigido, testes já rodados).

1. **Bootstrap de Admin (novo, 2026-08-08)**: `POST /users` exige `RequireAuthorization("AdminOnly")`, sem `AllowAnonymous`, e não há seed de usuário Admin em `db/init.sql` nem endpoint de login implementado — hoje **não existe nenhum caminho pela API para criar o primeiro usuário**. Precisa de uma decisão: seed de Admin no `init.sql`, endpoint de bootstrap protegido por secret, ou tornar `CreateUser` público e restringir `Role: Admin` na validação/regra de negócio.
2. **Correlation-id**: hoje só existe em `CreateUserRequest`, como campo do corpo (`Guid CorrelationId`, `NotEmpty`) — não é header. A inconsistência descrita numa versão anterior deste documento (header obrigatório vs. opcional entre `CreateUser`/`GetUserById`/`Login`) não se aplica mais porque esses outros dois endpoints não existem; vale decidir o padrão (header vs. corpo) quando eles forem implementados.
3. **`.env` versionado no git** com segredos de exemplo (SA_PASSWORD, Jwt SecretKey) — confirmar se é intencional para o workshop ou se deveria ir para `.gitignore`.
4. **Mocking duplicado nos testes**: `Fiap.Workshop.UnitTests.csproj` referencia tanto `Moq` quanto `NSubstitute`; os testes atuais (incluindo `CreateUserUseCaseUnitTests`, `LoggerTestBase<TCategory>`) usam apenas `Moq`. Vale decidir um padrão único.
5. **`Fiap.Workshop.FunctionalTests`** existe como projeto mas não tem nenhum arquivo de teste ainda — scaffold vazio.
6. **Domain events sem consumidor**: mecânica pronta e correta (ver §11 — o bug de dispatch que existia numa versão anterior já foi corrigido), mas nenhum `IDomainEventHandler` está registrado no DI — o dispatch acontece e não aciona nada. E-mail de boas-vindas planejado como primeiro handler, ver roadmap em §11.
7. **Escopo do Tech Challenge (Fase 1) parcialmente implementado**: o enunciado (`15SOAT - Fase 1 - Tech Challenge (1).pdf`, na raiz) pede um sistema de oficina mecânica — CRUD de clientes/veículos/serviços/peças (com controle de estoque), Ordem de Serviço com máquina de estados, orçamento automático, validação de CPF/CNPJ e placa, cobertura de teste mínima de 80% nos domínios críticos, relatório de vulnerabilidades (SAST) e documentação DDD. **Domain e Infrastructure já existem para todos os agregados de negócio** (`Customer`, `Vehicle`, `Service`, `InventoryItem`, `ServiceOrder` — entidades, models, repositórios, mapeamentos, migration; ver §3 e §5) — falta a camada de Application (use cases) e Api (endpoints) por cima deles, além da máquina de estados de OS, orçamento automático, validações de CPF/CNPJ/placa, cobertura de 80% e documentação DDD. O único módulo com a pilha completa (Domain→Application→Infrastructure→Api) é `User`, e mesmo esse só tem o fluxo de criação — login e atualização de e-mail ficaram só como scaffold (ver §4/§6). Ver seção "Roadmap / pendências do desafio" do `README.md` para a lista cobrada pelo enunciado (também desatualizada nesse ponto — vale revisar numa próxima passada).

## 10. Senha em `User` — desenho atual (reescrito em 2026-08-08)

> Esta seção descrevia anteriormente um desenho DDD (VO `HashedPassword`, `IPasswordHasher` como Domain Service, fábricas `Create`/`Rehydrate`) concluído numa iteração anterior do projeto. **Esse desenho não está presente no código desta branch** (`feature/3-create-user-uc`) — o que segue é o que existe de fato hoje.

- `User.Password` é uma `string` comum — sem Value Object, sem política de força aplicada no Domain (`UserErrors.PasswordTooShort`/`PasswordMissingUppercase`/etc. existem mas não são referenciadas por nenhum código).
- `IPasswordService` (não `IPasswordHasher`) mora em `Application.Interfaces.Services` — hashear senha é tratado como serviço técnico, não como invariante do agregado `User`. Implementação: `PasswordService` (Infrastructure), `BCrypt.Net-Next`, work factor 12, registrada como `Singleton`.
- `User` não hasheia nem valida a própria senha. Quem orquestra é `CreateUserMapper.MapToDomain` (Application): recebe o hash já pronto (`IPasswordService.Hash(input.Password)` calculado no use case) e só então constrói o `User`.
- Não existem `Create`/`Rehydrate`: o único construtor de `User` serve tanto para criar quanto para reidratar do banco, e sempre dispara `UserCreatedEvent` (ver §3).
- Validação de senha hoje só existe na Api (`CreateUserRequestValidator`: `NotEmpty().MinimumLength(8)`, sem checar maiúscula/minúscula/dígito) — não há "defesa em profundidade" replicada no Domain como a antiga versão deste documento descrevia.
- Não há `LoginUseCase` implementado — `LoginRequest`/`LoginResponse` existem como scaffold na Api/Application, mas sem use case, endpoint ou verificação de senha por trás ainda.

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
