# Fiap.Workshop — Contexto do Projeto

> Documento vivo de contexto técnico. Atualize sempre que a arquitetura, os use cases ou as decisões de design mudarem. Última atualização: 2026-07-06.

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
- **API**: Minimal APIs, `Asp.Versioning.Http` (versionamento via segmento de URL `/api/v{version}/...`), `Scalar.AspNetCore` (UI de docs, substitui Swagger UI), `Microsoft.AspNetCore.OpenApi`
- **Validação**: FluentValidation (validators na camada Api, um por Request)
- **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), policies `AdminOnly` e `UserOnly`
- **Hashing de senha**: `BCrypt.Net-Next` (work factor 12), via `IPasswordHasher` (Domain) / `BCryptPasswordHasher` (Infrastructure)
- **Persistência**: EF Core 10 + SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`)
- **Testes**: xUnit, FluentAssertions, Moq, NSubstitute (ambos presentes — inconsistência a resolver, ver §9), AutoFixture
- **Erros HTTP**: `AddProblemDetails()` + `UseExceptionHandler()`

## 3. Camada de Domínio (`Fiap.Workshop.Domain`)

### Abstrações (`Abstractions/`)
- `AggregateRoot<TId>`: base com `Id`, lista interna de `IDomainEvent`, `RaiseDomainEvent`, `GetDomainEvents`, `ClearDomainEvents`.
- `ValueObject`: igualdade estrutural via `GetEqualityComponents()`.
- `DomainException(string message)`: exceção de domínio simples.
- `IAggregateRoot`, `IDomainEvent`: contratos marcadores.

### Agregado `User` (`Users/User.cs`)
Propriedades: `Email` (VO), `Name`, `Password` (VO `HashedPassword`), `Role` (`UserRole`).

Um único construtor **privado**; toda criação passa por fábricas nomeadas:
- `User.Create(string email, string name, HashedPassword password, UserRole role = UserRole.User)` — valida `Name` não vazio, monta o agregado e dispara `UserCreatedEvent`. **Não conhece `IPasswordHasher`** — recebe o `HashedPassword` já pronto (quem hasheia é o mapper da Application, ver §4).
- `User.Rehydrate(Guid id, string email, string name, string passwordHash, UserRole role)` — reconstrói um usuário já existente (vindo do banco) a partir do hash já persistido. Não dispara eventos de domínio (evento = "acabei de nascer", não "fui carregado do banco").
- `VerifyPassword(string rawPassword, IPasswordHasher passwordHasher)` — delega para `Password.Matches(...)`.

### Value Object `Email` (`Users/Email.cs`)
- `MaxLength = 256`
- `Email.Create(string)`: valida vazio, tamanho máximo e presença de `@`; normaliza para lowercase/trim.
- Lança `DomainException` com mensagens de `UserErrors`.

### Value Object `HashedPassword` (`Users/HashedPassword.cs`)
- `MinLength = 10`.
- `CreateFromRaw(string rawPassword, IPasswordHasher passwordHasher)`: valida política de força (não vazia, ≥10 chars, 1 maiúscula, 1 minúscula, 1 dígito) e delega o hash para `IPasswordHasher.Hash`. **Único lugar do Domain que conhece `IPasswordHasher`.**
- `FromHash(string hash)`: empacota um hash já existente (vindo do banco), só valida "não vazio" — não reaplica a política de força (não faz sentido para um hash).
- `Matches(string rawPassword, IPasswordHasher passwordHasher)`: delega para `IPasswordHasher.Verify`.

### `IPasswordHasher` (`Users/IPasswordHasher.cs`)
Domain Service (interface): `Hash(string rawPassword)`, `Verify(string rawPassword, string hash)`. Implementado em Infrastructure (`BCryptPasswordHasher`). Deliberadamente **não** fica em `Application.Interfaces.Services` (onde estão `IJwtService`/`ICurrentUserService`) — proteger a senha é um invariante do próprio agregado `User`, diferente de JWT (mecanismo de sessão, sem significado para o domínio).

### `UserRole` (enum): `User = 0`, `Admin = 1`

### `UserErrors` (constantes de mensagem)
`NameEmpty`, `EmailEmpty`, `EmailTooLong`, `EmailInvalidFormat`, `NotFound`, `EmailAlreadyInUse`, `PasswordEmpty`, `PasswordTooShort`, `PasswordMissingUppercase`, `PasswordMissingLowercase`, `PasswordMissingDigit`, `PasswordHashEmpty`, `InvalidCredentials`.

### Eventos (`Users/Events/`)
- `UserCreatedEvent(Guid UserId)` — disparado em `User.Create`. Hoje só carrega o `UserId` ("evento magro"). Ver §11 para a mecânica completa de domain events e uma discussão sobre enriquecer esse evento.

## 4. Camada de Aplicação (`Fiap.Workshop.Application`)

Padrão de use case: **Input (record) → UseCase (classe) → Output (classe genérica com Result/Errors)**, sem MediatR — injeção direta de interfaces de use case.

### `Commons/Output.cs`
Classe de retorno padrão de todos os use cases: `IsValid`, `Messages`, `ErrorMessages`, `Result` (object, lido via `GetResult<T>()`), métodos `AddResult`, `AddMessage`, `AddErrorMessage(s)`.

### Use cases existentes (`UseCases/Users/`)

| Use Case | Input | Interface | Descrição |
|---|---|---|---|
| `CreateUserUseCase` | `CreateUserInput(CorrelationId, Name, Email, Password, Role)` | `ICreateUserUseCase` | Valida e-mail único via `IUserRepository.ExistsWithEmailAsync`; monta o `User` via `input.MapToDomain(_passwordHasher)`; persiste; retorna `UserResponse` |
| `GetUserByIdUseCase` | `GetUserByIdInput(Id, CorrelationId)` | `IGetUserByIdUseCase` | Busca por id, 404 lógico via `UserErrors.NotFound` |
| `LoginUseCase` | `LoginInput(Email, Password, CorrelationId)` | `ILoginUseCase` | Busca usuário por e-mail, valida a senha via `user.VerifyPassword(...)` e gera JWT via `IJwtService`. Erro genérico (`UserErrors.InvalidCredentials`) tanto para e-mail inexistente quanto senha errada, para não permitir enumeração de usuários |

Cada use case tem uma pasta `Boundaries/` (Input) e `Mapper/` (mapeamento entre camadas).

### Mapper de criação (`UseCases/Users/CreateUser/Mapper/CreateUserMapper.cs`)
- `MapToDomain(this CreateUserInput input, IPasswordHasher passwordHasher)`: primeiro chama `HashedPassword.CreateFromRaw(input.Password, passwordHasher)`, depois `User.Create(input.Email, input.Name, password, input.Role)`. É o único ponto da Application que orquestra as duas etapas (hash → criação do agregado) — mantém `User.Create` livre de conhecer o hasher.
- `MapToOutput(this User user)`: `User → UserResponse`.

### Interfaces / portas (`Interfaces/`)
- `IUnitOfWork.CommitAsync()`
- `Repositories/IUserRepository : IRepository<User, Guid>` — `GetByEmailAsync`, `ExistsWithEmailAsync`
- `Abstractions/IRepository<T, TId>` — CRUD genérico + `UnitOfWork`
- `Services/ICurrentUserService` — `UserId`, `Email`, `Role`, `IsAuthenticated` (lido do `ClaimsPrincipal`)
- `Services/IDomainEventDispatcher.DispatchAsync(events)`
- `Services/IJwtService.GenerateToken(userId, email, role)`
- `Abstractions/IDomainEventHandler<TEvent>` — infraestrutura de handlers de evento de domínio (nenhum handler concreto implementado ainda — ver §11)

### DTOs (`DTOs/Users/`)
- `UserResponse(Id, Name, Email, Role)`
- `LoginResponse(Token)`

### Composição (`IoC/DependencyInjection.cs`)
`AddApplication()` registra os 3 use cases como `Scoped`.

## 5. Camada de Infraestrutura (`Fiap.Workshop.Infrastructure`)

### Persistência (EF Core, SQL Server)
- `AppDbContext` (implementa `IUnitOfWork`): `DbSet<UserModel> Users`; `CommitAsync` faz `SaveChangesAsync`, e se houver dispatcher e eventos pendentes, dispara `IDomainEventDispatcher.DispatchAsync` **após** o commit; captura exceções (inclusive as do dispatch) e retorna `false` — ver §11 para o problema que isso causa quando houver handlers registrados.
- `UserModel` (`Repositories/Models/`): `Id`, `Email`, `Name`, `Password` (hash, `nvarchar(200)`), `Role`, `CreatedAt` (default `GETUTCDATE()` no banco).
- Mapeamento tabela `Users`: `Email` único (`HasIndex().IsUnique()`), `MaxLength(256)`; `Name` `MaxLength(100)`; `Password` `MaxLength(200)`; `Role` `MaxLength(20)`.
- `UserRepository : IUserRepository` — usa `AsNoTracking()` para leituras, mapeia Model↔Domain via `DomainMappers.MapToDomain` (usa `User.Rehydrate`) / `ModelMappers.MapToModel`; enfileira eventos de domínio no `AppDbContext` antes de limpar.
- Migrations: `InitialCreate` (2026-06-28), `AddRoleToUsers` (2026-06-29), `AddPasswordToUsers` (2026-07-06, coluna `Password nvarchar(200)` default `""`, gerada via `dotnet ef migrations add`).
- `Program.cs` roda `dbContext.Database.MigrateAsync()` automaticamente no startup (não usa migrations manuais em produção — atenção ao usar em cenário real).
- `Fiap.Workshop.Api.csproj` também referencia `Microsoft.EntityFrameworkCore.Design` (necessário porque a Api é o startup project usado pela ferramenta `dotnet ef` para gerar migrations).

### Serviços
- `JwtService : IJwtService` — gera token HS256 com claims `NameIdentifier`, `Email`, `Role`; lê `Jwt:SecretKey/Issuer/Audience/ExpirationInMinutes` da configuração.
- `CurrentUserService : ICurrentUserService` — lê claims do `IHttpContextAccessor`.
- `BCryptPasswordHasher : IPasswordHasher` — `BCrypt.Net.BCrypt.HashPassword`/`Verify`, work factor 12. Registrado como `Singleton` (stateless).
- `DomainEventDispatcher : IDomainEventDispatcher` — resolve `IDomainEventHandler<TEvent>` via reflection (`serviceProvider.GetServices` + `MakeGenericType`) e invoca `HandleAsync` em cada handler registrado. **Nenhum handler concreto existe ainda no DI** — hoje o dispatch é um no-op. Ver §11.

### Composição (`IoC/DependencyInjection.cs`)
`AddInfrastructure()`: registra `AppDbContext` (SqlServer via `ConnectionStrings:DefaultConnection`), `IUserRepository`, `ICurrentUserService`, `IJwtService`, `IPasswordHasher` (Singleton), `IDomainEventDispatcher`, `IHttpContextAccessor`.

## 6. Camada de API (`Fiap.Workshop.Api`)

Minimal APIs organizadas por feature em `Endpoints/{Feature}/*Endpoints.cs`, registradas em `Endpoints/EndpointsExtensions.cs` → `app.MapEndpoints()`.

### Endpoints existentes

**Users** (`/api/v{version}/users`, grupo com `RequireAuthorization("UserOnly")`)
- `POST /` — `CreateUser`. Marcado `.AllowAnonymous()` (sobrescreve a policy do grupo — cadastro público). Valida com `CreateUserRequestValidator`, chama `ICreateUserUseCase`. Retorna `201 Created` ou `400 BadRequest`.
- `GET /{id:guid}` — `GetUserById`. Exige autenticação (`UserOnly`). Retorna `200 Ok` ou `404 NotFound`.

**Auth** (`/api/v{version}/auth`, `AllowAnonymous`)
- `POST /login` — valida `LoginRequest`, chama `ILoginUseCase`. Retorna `200 Ok` com token ou `401 Unauthorized`.

Ambos os grupos usam header `x-correlation-id` (obrigatório em `CreateUser`, opcional em `GetUserById`/`Login` — inconsistência ainda não resolvida, ver §9).

### Requests / Validators / Mappers
- `CreateUserRequest(Name, Email, Password, PasswordConfirmation, Role)` — validado por `CreateUserRequestValidator`: nome obrigatório (≤100), e-mail obrigatório/formato/≤256, senha obrigatória (mín. 10 chars, 1 maiúscula, 1 minúscula, 1 número — mensagens já corrigidas), confirmação de senha igual à senha. Essa validação é redundante de propósito com `HashedPassword.CreateFromRaw` no Domain: a API dá feedback rápido/detalhado (400 com lista de erros), o Domain garante o invariante para qualquer chamador (defesa em profundidade).
- `LoginRequest(Email, Password)` — validado por `LoginRequestValidator` (e-mail obrigatório/formato/≤256; senha só `NotEmpty`, sem repetir a política de força — força só faz sentido na criação).
- Mappers (`CreateUserRequestMapper`, `LoginRequestMapper`) fazem `Request → Input` da Application.

### Composição (`IoC/DependencyInjection.cs`)
`AddApi()`: JWT Bearer, `AddApiVersioning` (URL segment reader, versão default 1), `AddAuthorizationBuilder` com policies `AdminOnly` (role `Admin`) e `UserOnly` (roles `User`/`Admin`), `AddProblemDetails`, registra os `*RequestValidator` como `Scoped`, configura OpenAPI com security scheme Bearer.

### `Program.cs`
Pipeline: `AddControllers` (não usado, pois tudo é Minimal API — resquício de template), `AddApplication → AddInfrastructure → AddApi`, migração automática do banco no boot, `MapOpenApi + MapScalarApiReference` (docs em `/scalar`), `UseExceptionHandler`, `UseHttpsRedirection`, `UseAuthentication`, `UseAuthorization`, `MapEndpoints()`.

## 7. Docker / Deploy

- **Dockerfile**: multi-stage (`sdk:10.0` restore → publish → `aspnet:10.0` runtime), copia apenas os `.csproj` primeiro (cache de layers), expõe porta `8080`.
- **docker-compose.yml**: serviços `api` (build local) + `sqlserver` (`mssql/server:2022-latest`), healthcheck via `sqlcmd`, volume nomeado `sqlserver-data`. Variáveis vêm do `.env`.
- **docker-compose.tests.yml**: SQL Server isolado (`integration-sql`) para os testes de integração, senha fixa `Integration@Test123`.
- **.env / .env.example** (variáveis, sem valores sensíveis aqui): `ASPNETCORE_ENVIRONMENT`, `API_PORT`, `SA_PASSWORD`, `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationInMinutes`. ⚠️ `.env` está versionado com uma senha/segredo de exemplo — confirmar se deveria estar no `.gitignore`.
- `tests/Fiap.Workshop.IntegrationTests/sql/init.sql`: schema usado pelos testes de integração (a `DatabaseFixture` roda esse script diretamente, **não** chama `MigrateAsync`). Precisa ser mantido manualmente em sincronia com as migrations do EF — hoje já inclui a coluna `Password` (default `''`, seguindo o mesmo padrão idempotente usado para `Role`).

## 8. CI/CD (GitHub Actions)

- `ci-feature.yml` — dispara em push para `feature/**`: build → unit tests → integration tests (sobe `docker-compose.tests.yml`) → functional tests (projeto sem testes reais ainda, portanto sempre "passa" vazio).
- `ci-develop.yml` — mesmo pipeline de `ci-feature.yml` + job final `open-release-pr` que abre PR automático `develop → release` se a branch `release` existir.
- `codeql.yml` — análise de segurança estática (não inspecionado em detalhe).
- Não há workflow para `main` nem para `release` ainda.

## 9. Pendências conhecidas

1. **Correlation-id inconsistente**: obrigatório (`[FromHeader]` sem `?`) no endpoint `CreateUser`, opcional (`Guid?`) em `GetUserById` e `Login`.
2. **`.env` versionado no git** com segredos de exemplo (SA_PASSWORD, Jwt SecretKey) — confirmar se é intencional para o workshop ou se deveria ir para `.gitignore`.
3. **Mocking duplicado nos testes**: `Fiap.Workshop.UnitTests.csproj` referencia tanto `Moq` quanto `NSubstitute`; os testes atuais usam apenas `Moq`. Vale decidir um padrão único.
4. **`Fiap.Workshop.FunctionalTests`** existe como projeto mas não tem nenhum arquivo de teste ainda — scaffold vazio.
5. **Domain events sem consumidor** e com um bug latente no tratamento de erro do dispatch — ver §11, é a pendência mais "quente" no momento (feature de e-mail de boas-vindas planejada para a próxima sessão).
6. **Testes de integração não foram executados de ponta a ponta** na sessão em que a feature de senha foi implementada (Docker indisponível no ambiente de trabalho) — só foram validados por compilação + pela suíte unitária (40/40 passando), que cobre a mesma lógica de domínio. Rodar `docker compose -f docker-compose.tests.yml up -d` e `dotnet test tests/Fiap.Workshop.IntegrationTests` antes de dar como 100% validado.

## 10. Feature de senha (Password) — concluída em 2026-07-06

Contexto histórico: a branch `feature/1-project-start` chegou com uma refatoração incompleta que adicionava `Password` ao fluxo de criação de usuário mas deixava o projeto sem compilar (`User.Create` removido mas ainda referenciado, sem coluna no banco, sem hashing, `LoginUseCase` sem verificar senha). Essa feature foi fechada nesta sessão com o seguinte desenho (DDD):

- **`HashedPassword`** (Value Object, Domain) concentra a política de força da senha e delega o hashing/verificação para `IPasswordHasher`.
- **`IPasswordHasher`** (Domain Service, interface no Domain) — deliberadamente **não** fica ao lado de `IJwtService` em `Application.Interfaces.Services`, porque proteger a senha é um invariante do agregado `User`, não um detalhe de sessão/entrega como o JWT.
- **`User`** nunca conhece `IPasswordHasher` diretamente — só recebe um `HashedPassword` já pronto. Quem orquestra as duas etapas (hash → criação) é o `CreateUserMapper.MapToDomain` na Application.
- **Construtor de `User` sempre privado**; criação e reconstituição (`Create`/`Rehydrate`) são fábricas nomeadas — `Rehydrate` não dispara `UserCreatedEvent` (evento = "nasceu agora", não "foi lido do banco").
- Implementação concreta do hasher (`BCryptPasswordHasher`, BCrypt.Net-Next) fica isolada em Infrastructure.
- `LoginUseCase` agora verifica a senha e usa a mesma mensagem de erro genérica (`UserErrors.InvalidCredentials`) tanto para e-mail inexistente quanto senha errada, para não vazar quais e-mails estão cadastrados.
- Migration `AddPasswordToUsers` gerada via `dotnet ef migrations add` (foi necessário adicionar `Microsoft.EntityFrameworkCore.Design` também ao projeto `Api`, que é o startup project usado pela ferramenta).
- Todos os testes afetados foram corrigidos; `HashedPasswordTests.cs` foi criado do zero para cobrir a política de força. 40/40 testes unitários passando.

## 11. Domain Events — mecânica atual (importante entender antes de adicionar handlers)

Como funciona hoje, de ponta a ponta:

1. `User.Create(...)` chama `RaiseDomainEvent(new UserCreatedEvent(user.Id))` — isso só adiciona o evento a uma `List<IDomainEvent>` **em memória**, dentro do próprio objeto `User` (`AggregateRoot<TId>._domainEvents`).
2. `UserRepository.AddAsync` move esses eventos para dentro do `AppDbContext` (`EnqueueDomainEvents`) e limpa a lista do `User`.
3. `AppDbContext.CommitAsync` primeiro roda `SaveChangesAsync` (grava o `UserModel` no SQL Server), e **só depois** de confirmar sucesso, entrega os eventos pendentes ao `IDomainEventDispatcher.DispatchAsync`.
4. `DomainEventDispatcher` resolve via reflection quais `IDomainEventHandler<TEvent>` estão registrados no DI e invoca `HandleAsync` em cada um, **de forma síncrona**, ainda dentro da mesma requisição/antes de responder ao cliente.

**Conclusões importantes:**
- **Eventos de domínio NÃO são persistidos em lugar nenhum.** Não existe tabela de eventos, fila ou log. É puramente um mecanismo de notificação em memória, vivo só durante aquela unit of work.
- Isso **não é Event Sourcing** (o estado não é reconstruído replayando eventos — vive direto na tabela `Users`) nem **Transactional Outbox** (não há garantia de entrega; se o processo cair entre o `SaveChangesAsync` e o `DispatchAsync`, o evento se perde silenciosamente).
- **Hoje não existe nenhum `IDomainEventHandler` registrado no DI** — o dispatch acontece mas não aciona nada. Toda a infraestrutura existe, mas está inerte.
- **Bug latente a corrigir antes de adicionar o primeiro handler**: `CommitAsync` tem um `try/catch` genérico em volta de `SaveChangesAsync` **e** do `DispatchAsync`. Se um handler lançar exceção, o método retorna `false`, e o use case chamador (ex.: `CreateUserUseCase`) reporta **"Failed to persist user."** — mesmo o `User` já tendo sido gravado com sucesso antes do dispatch. Precisa separar: falha ao salvar ≠ falha ao notificar (o dispatch deveria ter seu próprio `try/catch`, sem contaminar o resultado do commit).

### Plano para a próxima sessão: e-mail de boas-vindas ao criar usuário

Discutido e ainda não implementado — retomar aqui:

1. **Corrigir o bug do `CommitAsync`** (separar `try/catch` do `SaveChangesAsync` do `try/catch` do `DispatchAsync`) — pré-requisito antes de qualquer handler ir para produção.
2. **Enriquecer `UserCreatedEvent`**: hoje só tem `UserId`; para mandar e-mail é preciso `Email`/`Name`. Decisão tomada: preferir "evento gordo" (`UserCreatedEvent(Guid UserId, string Email, string Name)`) em vez de o handler re-consultar o `IUserRepository` — mais barato e captura o estado exatamente como era no momento da criação.
3. **`IEmailService`** — nova interface em `Application/Interfaces/Services/` (mesmo padrão de `IJwtService`), algo como `SendWelcomeEmailAsync(string email, string name)`.
4. **Implementação em Infrastructure**: como não há provedor de e-mail configurado (`.env` não tem SMTP/SendGrid), começar com um `LoggingEmailService` que só loga o envio — ponto de extensão pronto para um provedor real depois.
5. **Handler**: `SendWelcomeEmailOnUserCreated : IDomainEventHandler<UserCreatedEvent>` — colocar em **Application** (não Infrastructure), já que só orquestra uma chamada a uma abstração (`IEmailService`), sem detalhe técnico nenhum. Registrar no DI de `AddApplication()`.
6. Ponderar (não decidido ainda): o dispatch síncrono faz o `POST /users` esperar o "envio" do e-mail antes de responder. Para um provedor de e-mail real (chamada HTTP externa), isso adiciona latência à resposta. Para o workshop, começar síncrono é aceitável — mas vale registrar que, se algum dia isso incomodar, a evolução natural é official Outbox pattern + worker em background, não o dispatcher atual.
