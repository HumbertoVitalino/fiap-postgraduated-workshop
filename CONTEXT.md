# Fiap.Workshop — Contexto do Projeto

> Documento vivo de contexto técnico. Atualize sempre que a arquitetura, os use cases ou as decisões de design mudarem. Última atualização: 2026-07-05.

## 1. Visão geral

API .NET 10 seguindo **Clean Architecture** (Domain → Application → Infrastructure → Api), construída como template de workshop de pós-graduação FIAP. Expõe endpoints via **Minimal APIs** (não MVC controllers), com versionamento de API, autenticação JWT e autorização baseada em roles.

Solução: `Fiap.Workshop.slnx`

```
src/
  Fiap.Workshop.Domain          # Entidades, Value Objects, eventos de domínio — zero dependências externas
  Fiap.Workshop.Application     # Use cases, boundaries (Input/Output), interfaces (portas)
  Fiap.Workshop.Infrastructure  # EF Core, repositórios, serviços concretos (JWT, current user, dispatcher)
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
- **Persistência**: EF Core 10 + SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`)
- **Testes**: xUnit, FluentAssertions, Moq, NSubstitute (ambos presentes — inconsistência a resolver, ver §6), AutoFixture
- **Erros HTTP**: `AddProblemDetails()` + `UseExceptionHandler()`

## 3. Camada de Domínio (`Fiap.Workshop.Domain`)

### Abstrações (`Abstractions/`)
- `AggregateRoot<TId>`: base com `Id`, lista interna de `IDomainEvent`, `RaiseDomainEvent`, `GetDomainEvents`, `ClearDomainEvents`.
- `ValueObject`: igualdade estrutural via `GetEqualityComponents()`.
- `DomainException(string message)`: exceção de domínio simples.
- `IAggregateRoot`, `IDomainEvent`: contratos marcadores.

### Agregado `User` (`Users/User.cs`)
Propriedades: `Email` (Value Object), `Name`, `Password`, `Role` (`UserRole`).

⚠️ **Estado atual em transição (WIP não commitado)** — ver §6 "Estado atual / pendências" antes de mexer aqui.

### Value Object `Email` (`Users/Email.cs`)
- `MaxLength = 256`
- `Email.Create(string)`: valida vazio, tamanho máximo e presença de `@`; normaliza para lowercase/trim.
- Lança `DomainException` com mensagens de `UserErrors`.

### `UserRole` (enum): `User = 0`, `Admin = 1`

### `UserErrors` (constantes de mensagem)
`NameEmpty`, `EmailEmpty`, `EmailTooLong`, `EmailInvalidFormat`, `NotFound`, `EmailAlreadyInUse`.

### Eventos (`Users/Events/`)
- `UserCreatedEvent(Guid UserId)` — disparado no construtor de `User`.

## 4. Camada de Aplicação (`Fiap.Workshop.Application`)

Padrão de use case: **Input (record) → UseCase (classe) → Output (classe genérica com Result/Errors)**, sem MediatR — injeção direta de interfaces de use case.

### `Commons/Output.cs`
Classe de retorno padrão de todos os use cases: `IsValid`, `Messages`, `ErrorMessages`, `Result` (object, lido via `GetResult<T>()`), métodos `AddResult`, `AddMessage`, `AddErrorMessage(s)`.

### Use cases existentes (`UseCases/Users/`)

| Use Case | Input | Interface | Descrição |
|---|---|---|---|
| `CreateUserUseCase` | `CreateUserInput(CorrelationId, Name, Email, Password, Role)` | `ICreateUserUseCase` | Valida e-mail único via `IUserRepository.ExistsWithEmailAsync`, cria `User`, persiste, retorna `UserResponse` |
| `GetUserByIdUseCase` | `GetUserByIdInput(Id, CorrelationId)` | `IGetUserByIdUseCase` | Busca por id, 404 lógico via `UserErrors.NotFound` |
| `LoginUseCase` | `LoginInput(Email, CorrelationId)` | `ILoginUseCase` | Busca usuário por e-mail e gera JWT via `IJwtService` |

Cada use case tem uma pasta `Boundaries/` (Input) e `Mapper/` (mapeamento Domain → DTO de resposta).

### Interfaces / portas (`Interfaces/`)
- `IUnitOfWork.CommitAsync()`
- `Repositories/IUserRepository : IRepository<User, Guid>` — `GetByEmailAsync`, `ExistsWithEmailAsync`
- `Abstractions/IRepository<T, TId>` — CRUD genérico + `UnitOfWork`
- `Services/ICurrentUserService` — `UserId`, `Email`, `Role`, `IsAuthenticated` (lido do `ClaimsPrincipal`)
- `Services/IDomainEventDispatcher.DispatchAsync(events)`
- `Services/IJwtService.GenerateToken(userId, email, role)`
- `Abstractions/IDomainEventHandler<TEvent>` — infraestrutura de handlers de evento de domínio (nenhum handler concreto implementado ainda)

### DTOs (`DTOs/Users/`)
- `UserResponse(Id, Name, Email, Role)`
- `LoginResponse(Token)`

### Composição (`IoC/DependencyInjection.cs`)
`AddApplication()` registra os 3 use cases como `Scoped`.

## 5. Camada de Infraestrutura (`Fiap.Workshop.Infrastructure`)

### Persistência (EF Core, SQL Server)
- `AppDbContext` (implementa `IUnitOfWork`): `DbSet<UserModel> Users`; `CommitAsync` faz `SaveChangesAsync`, e se houver dispatcher e eventos pendentes, dispara `IDomainEventDispatcher.DispatchAsync` **após** o commit; captura exceções e retorna `false` (não propaga).
- `UserModel` (`Repositories/Models/`): `Id`, `Email`, `Name`, `Role`, `CreatedAt` (default `GETUTCDATE()` no banco). **Não tem coluna/propriedade `Password`** — ver §6.
- Mapeamento tabela `Users`: `Email` único (`HasIndex().IsUnique()`), `MaxLength(256)`; `Name` `MaxLength(100)`; `Role` `MaxLength(20)`.
- `UserRepository : IUserRepository` — usa `AsNoTracking()` para leituras, mapeia Model↔Domain via `DomainMappers`/`ModelMappers`; enfileira eventos de domínio no `AppDbContext` antes de limpar.
- Migrations: `InitialCreate` (2026-06-28) e `AddRoleToUsers` (2026-06-29, adiciona coluna `Role` com default `"User"`). Nenhuma migration para `Password` ainda.
- `Program.cs` roda `dbContext.Database.MigrateAsync()` automaticamente no startup (não usa migrations manuais em produção — atenção ao usar em cenário real).

### Serviços
- `JwtService : IJwtService` — gera token HS256 com claims `NameIdentifier`, `Email`, `Role`; lê `Jwt:SecretKey/Issuer/Audience/ExpirationInMinutes` da configuração.
- `CurrentUserService : ICurrentUserService` — lê claims do `IHttpContextAccessor`.
- `DomainEventDispatcher : IDomainEventDispatcher` — resolve `IDomainEventHandler<TEvent>` via reflection (`serviceProvider.GetServices` + `MakeGenericType`) e invoca `HandleAsync` em cada handler registrado. Nenhum handler concreto existe ainda no DI.

### Composição (`IoC/DependencyInjection.cs`)
`AddInfrastructure()`: registra `AppDbContext` (SqlServer via `ConnectionStrings:DefaultConnection`), `IUserRepository`, `ICurrentUserService`, `IJwtService`, `IDomainEventDispatcher`, `IHttpContextAccessor`.

## 6. Camada de API (`Fiap.Workshop.Api`)

Minimal APIs organizadas por feature em `Endpoints/{Feature}/*Endpoints.cs`, registradas em `Endpoints/EndpointsExtensions.cs` → `app.MapEndpoints()`.

### Endpoints existentes

**Users** (`/api/v{version}/users`, grupo com `RequireAuthorization("UserOnly")`)
- `POST /` — `CreateUser`. Marcado `.AllowAnonymous()` (sobrescreve a policy do grupo — cadastro público). Valida com `CreateUserRequestValidator`, chama `ICreateUserUseCase`. Retorna `201 Created` ou `400 BadRequest`.
- `GET /{id:guid}` — `GetUserById`. Exige autenticação (`UserOnly`). Retorna `200 Ok` ou `404 NotFound`.

**Auth** (`/api/v{version}/auth`, `AllowAnonymous`)
- `POST /login` — valida `LoginRequest`, chama `ILoginUseCase`. Retorna `200 Ok` com token ou `401 Unauthorized`.

Ambos os grupos usam header `x-correlation-id` (opcional em alguns endpoints, obrigatório em outro — inconsistência a revisar).

### Requests / Validators / Mappers
- `CreateUserRequest(Name, Email, Password, PasswordConfirmation, Role)` — validado por `CreateUserRequestValidator`: nome obrigatório (≤100), e-mail obrigatório/formato/≤256, senha obrigatória (mín. 10 chars, 1 maiúscula, 1 minúscula, 1 número — mensagem de erro ainda cita "8 characters", desatualizada), confirmação de senha igual à senha.
- `LoginRequest(Email)` — validado por `LoginRequestValidator` (e-mail obrigatório/formato/≤256). **Não pede senha.**
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

## 8. CI/CD (GitHub Actions)

- `ci-feature.yml` — dispara em push para `feature/**`: build → unit tests → integration tests (sobe `docker-compose.tests.yml`) → functional tests (projeto sem testes reais ainda, portanto sempre "passa" vazio).
- `ci-develop.yml` — mesmo pipeline de `ci-feature.yml` + job final `open-release-pr` que abre PR automático `develop → release` se a branch `release` existir.
- `codeql.yml` — análise de segurança estática (não inspecionado em detalhe).
- Não há workflow para `main` nem para `release` ainda.

## 9. Estado atual / pendências (importante — WIP não commitado)

A branch atual (`feature/1-project-start`) tem mudanças **não commitadas** introduzindo um campo `Password` no fluxo de criação de usuário, mas a refatoração está **incompleta e o projeto não compila** no estado atual. Pontos a resolver antes de continuar:

1. **`User.cs` mudou de "factory estático" para construtor público**, removendo `User.Create(...)` e a validação de `Name` vazio (`DomainException(UserErrors.NameEmpty)` não é mais lançada em lugar nenhum).
2. **`CreateUserUseCase.cs` ainda chama `User.Create(input.Email, input.Name, input.Role)`** — método que não existe mais e não passa a senha. Precisa ser atualizado para `new User(Guid.NewGuid(), input.Email, input.Name, input.Password, input.Role)` (ou equivalente).
3. **`DomainMappers.MapToDomain` (Infrastructure) também chama a assinatura antiga** do construtor de `User` (passava um `Email` já construído, sem `password`) — quebrado pela mesma mudança.
4. **`UserModel` (Infrastructure) não tem coluna/propriedade `Password`** — não existe migration para isso, então mesmo corrigindo o código, a senha não seria persistida.
5. **Nenhum hashing de senha existe** — `Password` é tratado como string crua em todo o fluxo (`CreateUserInput.Password`, `User.Password`). Antes de persistir, é necessário introduzir hashing (ex.: BCrypt/Argon2/`PasswordHasher<T>` do ASP.NET Identity) — não usar texto plano em banco.
6. **`LoginUseCase` não valida senha nenhuma** — apenas busca por e-mail e emite token. É necessário adicionar verificação de senha (hash) e o `LoginRequest`/`LoginInput` precisam incluir `Password`.
7. **Testes desatualizados que quebram com o WIP atual**:
   - `tests/Fiap.Workshop.UnitTests/Domain/Users/UserTests.cs` usa `User.Create(...)` (não existe mais).
   - `tests/Fiap.Workshop.IntegrationTests/.../LoginUseCaseTests.cs` chama `new CreateUserInput(Guid, "Alice", email, UserRole.User)` sem `Password` (assinatura mudou, tem 5 posições agora).
   - `tests/Fiap.Workshop.UnitTests/.../CreateUserUseCaseTests.cs` provavelmente também será afetado quando `CreateUserUseCase` for corrigido, já que usa `AutoFixture` para gerar `CreateUserInput` (deve continuar funcionando, mas os asserts não cobrem `Password`).
8. **`Fiap.Workshop.FunctionalTests`** existe como projeto mas não tem nenhum arquivo de teste ainda — scaffold vazio.
9. **Mocking duplicado nos testes**: `Fiap.Workshop.UnitTests.csproj` referencia tanto `Moq` quanto `NSubstitute`; os testes atuais usam apenas `Moq`. Vale decidir um padrão único.
10. **Mensagem de validação de senha desatualizada**: `CreateUserInputValidator` exige `MinimumLength(10)` mas a mensagem de erro diz "at least 8 characters".
11. **Correlation-id inconsistente**: obrigatório (`[FromHeader]` sem `?`) no endpoint `CreateUser`, opcional (`Guid?`) em `GetUserById` e `Login`.
12. **`.env` versionado no git** com segredos de exemplo (SA_PASSWORD, Jwt SecretKey) — confirmar se é intencional para o workshop ou se deveria ir para `.gitignore`.

## 10. Próximos passos sugeridos

- [ ] Fechar a feature de senha: corrigir `CreateUserUseCase`, `DomainMappers`, adicionar coluna `Password` (hasheada) + migration, atualizar `LoginUseCase` para validar credenciais.
- [ ] Introduzir um `IPasswordHasher`/serviço de hashing na Application/Infrastructure.
- [ ] Atualizar testes unitários e de integração quebrados pela mudança de assinatura de `User`.
- [ ] Adicionar testes funcionais reais (`Fiap.Workshop.FunctionalTests` está vazio).
- [ ] Revisar consistência do header `x-correlation-id` entre endpoints.
- [ ] Avaliar handlers concretos para `IDomainEventHandler<UserCreatedEvent>` (hoje o dispatcher existe mas não há nenhum handler registrado).
