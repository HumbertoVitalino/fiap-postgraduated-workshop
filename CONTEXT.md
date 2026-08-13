# Fiap.Workshop — Contexto do Projeto

> Documento vivo de contexto técnico. Atualize sempre que a arquitetura, os use cases ou as decisões de design mudarem. Última atualização: 2026-08-12 (branch `feature/8-put-users`).

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
- `AggregateRoot` (classe abstrata, **não genérica**): construtor posicional `(Guid id, DateTime createdAt, DateTime updatedAt)`; `Id`/`CreatedAt`/`UpdatedAt` com setter `protected`; lista interna de `IDomainEvent`; `RaiseDomainEvent` (protected), `GetDomainEvents()`, `ClearDomainEvents()`, `SetUpdatedAt()` (protected, `UpdatedAt = DateTime.Now` — trocado de `DateTime.UtcNow` em 2026-08-12 pra manter consistência com o resto do código, que usa `DateTime.Now` em todo lugar, ex. `CreateUserMapper`/`CreateCustomerMapper`). Até `feature/7-get-users`, `SetUpdatedAt()` existia mas **nenhum agregado a usava**; `User` é o primeiro a chamá-la, via `UpdateProfile`/`ChangePassword` (ver abaixo).
- `ValueObject`: igualdade estrutural via `GetEqualityComponents()` — hoje **sem nenhum VO concreto usando** (nenhuma classe do Domain herda dela).
- `DomainException(string message)`: exceção de domínio simples.
- `IDomainEvent`: contrato marcador (não existe `IAggregateRoot` separado).

### Agregado `User` (`Entities/User.cs`)
Propriedades: `Name`, `Password`, `Role` (`UserRole`), `Email` — todas `string`/`enum` simples com setter `private`. **Não há Value Objects.**

Um único construtor **público**, posicional: `User(Guid id, string email, string name, string password, UserRole role, DateTime createdAt, DateTime updatedAt)`. Não existem `Create`/`Rehydrate`/fábricas nomeadas, e não há validação de invariante nenhuma no construtor (nome vazio, formato de e-mail, força de senha — tudo isso, se acontece, acontece na Api, ver §6). O construtor **sempre** dispara `RaiseDomainEvent(new UserCreatedEvent(id))`, inclusive quando `DomainMappers.MapToDomain` (Infrastructure) chama `new User(...)` para reidratar um usuário lido do banco — na prática é inofensivo hoje porque só `UserRepository.AddAsync`/`Update` enfileiram os eventos pendentes no `AppDbContext` (`EnqueueEvents`); leituras (`GetByIdAsync`/`GetByEmailAsync`) descartam o evento junto com o objeto. É um ponto frágil a observar se um dia uma leitura passar a enfileirar eventos — todo `SELECT` de usuário reemitiria "usuário criado".

Dois métodos de comportamento, adicionados em `feature/8-put-users` (2026-08-12) pros use cases `UpdateUser`/`ChangePassword` (ver §4):
- `UpdateProfile(string name, string email, UserRole role)`: muta `Name`/`Email`/`Role` direto e chama `SetUpdatedAt()`.
- `ChangePassword(string newPasswordHash)`: muta `Password` direto e chama `SetUpdatedAt()`.

**Bug corrigido nesta sessão, antes de esses métodos existirem**: a primeira versão de "atualizar usuário" reconstruía um `User` inteiro via `new User(...)` só pra trocar 3 campos — e como o construtor sempre dispara `UserCreatedEvent` (parágrafo acima), isso fazia todo `Update` (mesmo um repeat idempotente) enfileirar um evento de "usuário criado" incorreto via `UserRepository.Update`'s `EnqueueEvents`. `UpdateProfile`/`ChangePassword` resolvem isso na raiz: mutam o agregado já existente em vez de reconstruí-lo, então nenhum dos dois dispara evento. É o padrão a seguir pra qualquer mutação futura de `User` — nunca `new User(...)` pra "atualizar", sempre um método de comportamento dedicado.

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

> ⚠️ Reescrita em 2026-08-08 (junto com §3), atualizada em 2026-08-11 com a chegada de `LoginUser` e `CreateCustomer`, em 2026-08-12 com `GetCustomer` e `GetUsers` (branch `feature/7-get-users`), e novamente no mesmo dia com `UpdateUser` e `ChangePassword` (branch `feature/8-put-users`). `UpdateEmailUseCase` (o scaffold antigo, só `Email`) continua sem existir — foi substituído em espírito pelo `UpdateUser` (Name/Email/Role) descrito abaixo.

Padrão de use case: **Input (record) → UseCase (classe) → Output (classe genérica com Result/Errors)**, sem MediatR — injeção direta de interfaces de use case.

### `Commons/Output.cs`
Classe de retorno padrão de todos os use cases: `IsValid`, `Messages`, `ErrorMessages`, `Result` (object, lido via `GetResult<T>()`), métodos `AddResult`, `AddMessage`, `AddErrorMessage(s)`.

### Use cases existentes (sete hoje: `CreateUser`, `LoginUser`, `CreateCustomer`, `GetCustomer`, `GetUsers`, `UpdateUser`, `ChangePassword`)

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
- `MapToDomain(this CreateUserInput input, string passwordHash)`: `new User(Guid.NewGuid(), input.Email, input.Name, passwordHash, input.Role, DateTime.Now, DateTime.Now)`.
- `MapToDto(this User user)`: `new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString())` — usado por `CreateUserUseCase` (ver acima) e por `GetUsersMapper` (ver `UseCases/GetUsers/` abaixo).

**Bug corrigido em 2026-08-12**: até então, `MapToDomain` usava **`input.CorrelationId` como `Id` do `User`**, em vez de gerar um `Guid` novo — diferente do padrão usado em `CreateCustomer` (`Guid.NewGuid()`, ver abaixo). Isso quebrou em produção: um cliente que reenvia a mesma requisição com o mesmo `CorrelationId` (timeout, duplo clique, retry) faz a segunda tentativa colidir com a `PRIMARY KEY` da tabela `Users`, gerando um `DbUpdateException` (violação de PK) em vez do erro amigável de "e-mail já existe" — porque o conflito acontece no `Id`, não no `Email`, então `ExistsWithEmailAsync` não pega o caso antes do `INSERT`. Corrigido trocando para `Guid.NewGuid()`, alinhando com `CreateCustomer`.

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

#### `UseCases/GetCustomer/` (existia antes desta branch, nunca tinha sido documentado aqui)

`GetCustomerUseCase` (interface `IGetCustomerUseCase`), input `GetCustomerInput` — **não é um `record`, é um `readonly struct`** (`CorrelationId`, `CustomerId`), único boundary do projeto com esse desenho (todos os outros são `record`/classe):
1. Busca por id via `ICustomerRepository.GetByIdAsync(input.CustomerId, ...)` — se não encontrar, loga `LogWarning` e retorna erro `"Unable to find customer"`.
2. Em sucesso, `output.AddResult(customer.MapToDto())`.

**Sem mapper próprio** — reaproveita `CreateCustomerMapper.MapToDto` (`UseCases/CreateCustomer/Mapper/`) em vez de ter um `GetCustomerMapper.cs` dedicado; diferente de `GetUsers` (abaixo), que tem mapper próprio. Cobertura: unitários (`GetCustomerUseCaseUnitTests`) e integração (`GetCustomerUseCaseIntegrationTests` — cria um customer de verdade via `ICreateCustomerUseCase` e busca de volta; caso "não existe" usa um `Guid.NewGuid()` que nunca foi inserido, não depende de a tabela estar vazia).

#### `UseCases/GetUsers/` (novo, branch atual `feature/7-get-users`)

`GetUsersUseCase` (interface `IGetUsersUseCase`), input é só um `Guid correlationId` solto — **não tem boundary/record próprio**, diferente de todos os outros use cases:
1. Busca todos via `IUserRepository.GetAllAsync(...)` — sem filtro, sem paginação, devolve a tabela `Users` inteira.
2. Se a lista vier vazia, loga `LogWarning` e `output.AddResult(Array.Empty<User>())`.
3. Se houver usuários, `output.AddResult(users.MapToDto())` (`IEnumerable<UserResponse>`, via `GetUsersMapper`).
4. Em ambos os casos `output.IsValid` fica `true` (o `AddResult` sempre marca válido) — **não existe um caminho de erro real** neste use case; "lista vazia" é sucesso, só logado como warning.

⚠️ **Inconsistência de tipo em `Output.Result`**: o passo 2 devolve `Array.Empty<User>()` (entidades de domínio) e o passo 3 devolve `IEnumerable<UserResponse>` (DTO) — dois formatos diferentes de payload dependendo de a tabela estar vazia ou não. Como `Output.Result` é `object?` (sem tipo genérico), isso não gera erro de compilação, mas quem serializa a resposta (o endpoint HTTP, ver §6) devolve um JSON com forma diferente em cada caso (ex.: `Password`/hash exposto no caminho vazio, já que seria a entidade `User` crua — hoje inofensivo só porque o array está sempre vazio nesse caminho, mas é uma armadilha se alguém copiar esse padrão em outro use case). Vale corrigir para `output.AddResult(Enumerable.Empty<UserResponse>())` (ou `users.MapToDto()` já aceita lista vazia direto, tornando o `if` desnecessário).

**Mapper (`UseCases/GetUsers/Mapper/GetUsersMapper.cs`)**
- `MapToDto(this IEnumerable<User> users)`: `users.Select(x => x.MapToDto())` — reaproveita o `User.MapToDto()` de `CreateUserMapper` (ver acima) por extensão, não duplica o mapeamento campo a campo.

Cobertura: unitários (`GetUsersUnitTests` — sucesso com usuários via `AutoFixture.CreateMany<User>()`, e sucesso+log quando a lista vem vazia, usando `LoggerTestBase<GetUsersUseCase>` pra verificar o `LogWarning`) e integração (`GetUsersUseCaseIntegrationTests` — cria um usuário via `ICreateUserUseCase` e confere que ele aparece no resultado). **Não existe cenário de integração "lista vazia"**: como `DatabaseFixture` é compartilhado por toda a collection `"Integration"` (`ICollectionFixture`, sem reset entre testes) e `GetAllAsync` lê a tabela inteira sem filtro, não há como garantir a tabela vazia de forma confiável entre classes de teste diferentes — decisão consciente de só cobrir esse caminho no unit test (mockado).

#### `UseCases/UpdateUser/` (novo, branch atual `feature/8-put-users`)

`UpdateUserUseCase` (interface `IUpdateUserUseCase`), input `UpdateUserInput(CorrelationId, UserId, Name, Email, Role)` — **sem `Password`**, decisão consciente (ver `ChangePassword` abaixo, troca de senha é um fluxo separado):
1. Busca o usuário por id (`GetByIdAsync`) — não existe → `LogWarning` + erro `"Unable to find user"`.
2. **Só checa e-mail duplicado se o e-mail estiver mudando** (`if (!string.Equals(existing.Email, input.Email, ...))`) — comparando contra o estado **atual** buscado no banco a cada chamada, não contra um snapshot antigo. Isso é o que torna a rota idempotente: uma segunda chamada idêntica não colide com o próprio e-mail que ela mesma já gravou na primeira chamada. Se checasse sempre, incondicionalmente, um retry do mesmo `PUT` falharia com "e-mail já existe" na segunda vez.
3. Se o e-mail mudou e já pertence a outro usuário, `LogWarning` + erro `"User with email {Email} already exists."` (mesma mensagem do `CreateUser`).
4. Aplica `user.UpdateProfile(input.Name, input.Email, input.Role)` (método de comportamento no `User`, ver §3 — **não reconstrói o agregado**) e persiste (`Update` + `UnitOfWork.CommitAsync`).
5. Falha no commit → `LogError` + erro `"Error updating user with id {UserId}."`.
6. Sucesso → `output.AddResult(user.MapToDto())`, reaproveitando `CreateUserMapper.MapToDto` (sem mapper próprio — o `UpdateUserMapper` que existiu brevemente foi removido; a "tradução" de input pra chamada de domínio é só `user.UpdateProfile(...)` direto no use case).

Cobertura: unitários (`UpdateUserUseCaseUnitTests` — 5 cenários: sucesso sem mudar e-mail, sucesso mudando e-mail livre, falha usuário não existe, falha e-mail já em uso, falha ao salvar) e integração (`UpdateUserUseCaseIntegrationTests` — persiste e relê via `IUserRepository.GetByIdAsync`, falha usuário não existe, falha e-mail duplicado entre dois usuários seedados). Domínio: `UserUnitTests.User_ShouldUpdateProfile_WhenUpdateProfileIsCalled` confirma que `Name`/`Email`/`Role` mudam, `UpdatedAt` avança, e **nenhum evento novo é disparado**.

#### `UseCases/ChangePassword/` (novo, branch atual `feature/8-put-users`)

`ChangePasswordUseCase` (interface `IChangePasswordUseCase`), input `ChangePasswordInput(CorrelationId, UserId, CurrentPassword, NewPassword)` — desenhado como **troca self-service** (o usuário prova que é ele mesmo enviando a senha atual), não reset administrativo:
1. Busca o usuário por id — não existe → `LogWarning` + erro `"Unable to find user"`.
2. Verifica a senha atual via `IPasswordService.Verify(input.CurrentPassword, user.Password)` — inválida → `LogWarning` + erro `"Invalid current password"`, **sem persistir nada** (`Update` nunca é chamado nesse caminho).
3. Hasheia a nova senha (`IPasswordService.Hash`) e aplica via `user.ChangePassword(newPasswordHash)` (método de comportamento, ver §3 — mesma lógica de "mutar, não reconstruir" do `UpdateProfile`).
4. Persiste (`Update` + `UnitOfWork.CommitAsync`) — falha → `LogError` + erro `"Error changing password for user with id {UserId}."`.
5. Sucesso → `output.AddResult(user.MapToDto())` (`UserResponse` não expõe `Password`, seguro devolver).

**Endpoint Api** (`PATCH /api/v1/users/{id}/password`, ver §6) implementado no mesmo dia — a decisão de método HTTP: `PUT` fica descartado porque a operação **não é idempotente** (uma segunda chamada idêntica falha, já que `CurrentPassword` deixa de bater depois da primeira troca), o que viola a exigência de idempotência do `PUT` por spec; `PATCH` não exige idempotência (RFC 5789) e descreve corretamente "atualização parcial de um campo do recurso `User`" — o argumento inicial contra `PATCH` (que ele só devia carregar o valor-alvo, não uma prova/precondição) foi revisto: nada impede um `PATCH` de carregar uma precondição (paralelo a `If-Match`/ETag), e como não há efeito colateral adicional hoje (sem invalidar sessão, sem e-mail de aviso, sem log de auditoria), `POST`-como-ação ficaria over-engineered pro que o sistema faz agora.

**Revogação de token discutida e descartada por enquanto**: trocar a senha **não invalida** nenhum JWT já emitido — eles continuam válidos até expirar (`Jwt:ExpirationInMinutes`, fallback 60min). O `JwtService` (ver §5) é totalmente stateless (sem `jti`, sem tabela de sessão, sem denylist), então revogação de verdade exigiria uma mudança estrutural na autenticação inteira (ex.: `TokenVersion`/`SecurityStamp` no `User` + validação customizada no pipeline JWT) — fora do escopo desta feature, registrado como limitação conhecida (ver §9).

Cobertura: unitários (`ChangePasswordUseCaseUnitTests` — 4 cenários: sucesso, usuário não existe, senha atual inválida, falha ao salvar) e integração (`ChangePasswordUseCaseIntegrationTests` — troca via `ICreateUserUseCase` real + confere com `IPasswordService.Verify` real (BCrypt) que a senha nova bate e a antiga não bate mais; falha usuário não existe; falha senha atual incorreta). Domínio: `UserUnitTests.User_ShouldChangePassword_WhenChangePasswordIsCalled` confirma `Password` alterado, `UpdatedAt` avançado, zero eventos novos.

### Interfaces / portas (`Interfaces/`)
- `Repositories/IUnitOfWork.CommitAsync()`
- `Repositories/IUserRepository : IRepository<User>` — `GetByEmailAsync(string email, ...)`, `ExistsWithEmailAsync(string email, ...)`, `GetAllAsync(...)` (novo, `feature/7-get-users` — sem paginação/filtro, devolve `IEnumerable<User>` com a tabela inteira)
- `Repositories/ICustomerRepository : IRepository<Customer>` — `AnyAsync(string document, ...)` (adicionado com o `CreateCustomer` use case; antes a interface era vazia, seguindo a política incremental de repositório — ver §9)
- `Abstractions/IRepository<T> where T : AggregateRoot` — **sem segundo parâmetro `TId`** (assume `Guid` implicitamente): `UnitOfWork`, `GetByIdAsync`, `AddAsync`, `Update`, `Remove`. Outras portas de repositório já existem seguindo o mesmo molde, ainda vazias (sem método extra): `IVehicleRepository`, `IInventoryItemRepository`, `IServiceRepository`, `IServiceOrderRepository` (implementações em Infrastructure, ver §5, mas sem use case/endpoint consumindo ainda).
- `Services/ICurrentUserService` — `UserId` (`Guid`), `Email` (`string`), `Role` (`string`), `IsAuthenticated` (lido do `ClaimsPrincipal`)
- `Services/IDomainEventDispatcher.DispatchAsync(events, cancellationToken)`
- `Services/IJwtService.GenerateToken(User user)` — **mudou desde a última reescrita**: antes recebia três `string` soltos (`userId`, `email`, `role`); hoje recebe a entidade `User` inteira e extrai os claims internamente (ver implementação em §5).
- `Services/IPasswordService` — `Hash(string rawPassword)`, `Verify(string rawPassword, string hash)`. Mora em `Application.Interfaces.Services` (não em Domain) — ver nota em §10 sobre essa diferença em relação a uma iteração anterior do projeto.
- `Abstractions/IDomainEventHandler<TEvent>` — infraestrutura de handlers de evento de domínio (nenhum handler concreto implementado ainda — ver §11)
- `UseCases/ICreateUserUseCase`, `ILoginUserUseCase`, `ICreateCustomerUseCase`, `IGetCustomerUseCase`, `IGetUsersUseCase`, `IUpdateUserUseCase`, `IChangePasswordUseCase` — um por use case, todos só com `Handle(TInput, CancellationToken) : Task<Output>` (em `IGetUsersUseCase`, `TInput` é só `Guid correlationId`, sem boundary próprio — ver acima).

### Commons (`Commons/`)
- `Output.cs` — ver acima.
- `StringExtensions.cs` (novo): `StandardizeDocument(this string document)` (formata CPF/CNPJ sem pontuação via `CpfCnpjLibrary`, baseado no tamanho — 11 = CPF, 14 = CNPJ, outro tamanho lança `ArgumentException`) e `IsValidDocument(this string document)` (valida dígito verificador; tamanho fora de 11/14 retorna `false` em vez de lançar). Usado por `CreateCustomerInput` (normalização) e por `CreateCustomerRequestValidator` na Api (validação de fato, ver §6).

### DTOs
- `DTOs/Users/UserResponse(Id, Name, Email, Role)` — usado por `CreateUserUseCase.MapToDto`, por `GetUsersUseCase` (via `GetUsersMapper`), e agora também por `UpdateUserUseCase`/`ChangePasswordUseCase` (chamando `user.MapToDto()` direto, sem mapper próprio — ver acima); `Role` é `string` (`user.Role.ToString()`). Não expõe `Password`, então é seguro devolver mesmo no fluxo de troca de senha.
- `DTOs/Users/LoginResponse(Token)` — segue existindo como DTO, mas `LoginUserUseCase` devolve o token como `string` cru (`output.AddResult(token)`), não empacotado num `LoginResponse` — parece scaffold ainda não conectado.
- `DTOs/Customer/CustomerResponse(Id, Name, Phone)` — usado por `CreateCustomerUseCase.MapToDto` **e** por `GetCustomerUseCase` (que reaproveita o mesmo mapper, ver acima). Note que **não inclui `Document` nem `Email`** na resposta.

### Composição (`IoC/DependencyInjection.cs`)
`AddApplication()` registra `ICreateUserUseCase`, `ILoginUserUseCase`, `ICreateCustomerUseCase`, `IGetCustomerUseCase`, `IGetUsersUseCase`, `IUpdateUserUseCase` e `IChangePasswordUseCase`, todos como `Scoped`.

## 5. Camada de Infraestrutura (`Fiap.Workshop.Infrastructure`)

> ⚠️ Ajustada em 2026-08-08 junto com §3/§4 — nomes de tipo (`IPasswordHasher`→`IPasswordService`), comportamento do `CommitAsync` e o estado real dos repositórios dos demais agregados estavam desatualizados nesta seção.

### Persistência (EF Core, SQL Server)
- `AppDbContext` (implementa `IUnitOfWork`): `DbSet` para todos os agregados (`Users`, `Customers`, `Vehicles`, `InventoryItems`, `Services`, `ServiceOrders`); `CommitAsync` roda `SaveChangesAsync` num `try/catch` **próprio** (se falhar, loga `LogError` e retorna `false` direto); só depois, **num `try/catch` separado**, entrega os eventos pendentes a `IDomainEventDispatcher.DispatchAsync` — se o dispatch falhar, só loga (não derruba o resultado do commit). O bug descrito numa versão anterior deste documento (um único `try/catch` em volta dos dois, fazendo falha de dispatch parecer falha de persistência) **já está corrigido** — ver §11.
- `UserModel` (`Repositories/Models/`): `Id`, `Email`, `Name`, `Password` (**`string`**, não `varbinary`/`byte[]` — refatorado em 2026-08-07), `Role`, `CreatedAt` (`DateTime`, não nulo), `UpdatedAt` (`DateTime?`). Vêm do domínio via `MapToModel`/`MapToDomain` — sem default de banco (`GETUTCDATE()`); o `User` é a fonte de verdade para essas datas.
- Mapeamento tabela `Users`: `Email` único (`HasIndex().IsUnique()`), `MaxLength(256)`; `Name` `MaxLength(100)`; `Password` `MaxLength(60)` (tamanho fixo de um hash BCrypt); `Role` `HasConversion<string>().HasMaxLength(20)`.
- `UserRepository : IUserRepository` — usa `AsNoTracking()` para leituras; mapeia Model↔Domain via `DomainMappers.MapToDomain`/`ModelMappers.MapToModel`, que chamam `new User(...)`/`new UserModel(...)` diretamente (**não existe `User.Rehydrate`**, ver §3 sobre o efeito colateral disso no `UserCreatedEvent`); enfileira eventos de domínio no `AppDbContext` (`EnqueueEvents`) só em `AddAsync`/`Update`, nunca em leituras. `GetAllAsync` (novo, `feature/7-get-users`): `_context.Users.AsNoTracking().ToListAsync(...)` + `DomainMappers.MapToDomain(this IEnumerable<UserModel>)` (novo overload, `model.Select(MapToDomain)`) — sem filtro, sem paginação, mesma observação de "leitura não enfileira evento" se aplica aqui também. `Update` (herdado, já existia desde antes) até `feature/7-get-users` só era exercitado por testes (`UserRepositoryTests`); desde `feature/8-put-users` é usado de verdade por `UpdateUserUseCase`/`ChangePasswordUseCase` (ver §4) — é por isso que o bug do `UserCreatedEvent` em `Update` (§3) só virou um problema prático agora, nunca antes.
- `CustomerRepository : ICustomerRepository` — mesmo molde de `UserRepository` (`AsNoTracking()` em leituras), mais `AnyAsync(string document, ...)` (`_context.Customers.AsNoTracking().AnyAsync(x => x.Document == document, ...)`), adicionado junto com o `CreateCustomer` use case (2026-08-11) — **primeiro repositório de negócio (fora `User`) a ganhar um método além do `IRepository<T>` base**, confirmando a política incremental (ver §9).
- **Os demais agregados já têm repositório completo implementado**, seguindo o mesmo molde de `UserRepository` (`GetByIdAsync`/`AddAsync`/`Update`/`Remove`, herdando de `Repository<T>`), mas ainda sem método extra: `VehicleRepository`, `InventoryItemRepository`, `ServiceRepository`, `ServiceOrderRepository` — todos registrados no DI (ver Composição abaixo). O que falta pra esses agregados é a camada de Application/Api por cima (use cases, endpoints), não a Infrastructure.
- **(2026-08-04)** Migrations antigas (`InitialCreate`, `AddRoleToUsers`, `AddPasswordToUsers`, `AddUpdatedAtToUsers`) apagadas; schema passou a nascer via `db/init.sql` (script SQL idempotente), executado pelo serviço `sqlserver-init` no `docker-compose.yml` **antes** da `api` subir (`depends_on: condition: service_completed_successfully`). `Program.cs` **não chama mais** `dbContext.Database.MigrateAsync()`. O mesmo `db/init.sql` é reaproveitado pelos testes de integração (`DatabaseFixture`, via `Link` no `.csproj`) — variável `$(DatabaseName)` (sintaxe `sqlcmd`) é substituída por `Fiap_Workshop` no compose e por `IntegrationTestsDb` via `string.Replace` no `DatabaseFixture`.
- **(2026-08-04)** Modelos de infra criados para todos os agregados do domínio (`CustomerModel`, `VehicleModel`, `InventoryItemModel`, `ServiceModel`, `ServiceOrderModel` + `ServiceOrderPartModel`/`ServiceOrderServiceModel`/`ServiceOrderStatusHistoryModel` como filhos, com `HasMany().WithOne().OnDelete(Cascade)`), com `ModelMappers`/`DomainMappers` e configuração completa em `AppDbContext.OnModelCreating` (índices únicos em `Document/Email/Phone` do Customer, `LicensePlate`, `Code`, `Number`; FKs explícitas entre agregados sem navegação — ex. `Vehicle.CustomerId → Customers` — via `HasOne<T>().WithMany().HasForeignKey().OnDelete(Restrict)`).
- **(2026-08-07)** Migration baseline **regenerada do zero** (`20260807031339_InitialCreate`, via `dotnet ef migrations add --project src/Fiap.Workshop.Infrastructure --startup-project src/Fiap.Workshop.Api`) cobrindo todos os agregados de uma vez — a pasta `Migrations/` existe só como apoio de ferramenta (gera SQL a partir do modelo real, útil pra conferir/gerar o `db/init.sql`), não é a fonte de verdade em runtime.
- `Fiap.Workshop.Api.csproj` também referencia `Microsoft.EntityFrameworkCore.Design` (necessário porque a Api é o startup project usado pela ferramenta `dotnet ef`).

### Serviços
- `JwtService : IJwtService` — assinatura mudou para `GenerateToken(User user)` (antes recebia `string userId, email, role` soltos, ver §4); gera token HS256 com claims `ClaimTypes.NameIdentifier` (`user.Id`), `ClaimTypes.Email`, `ClaimTypes.Role` (`user.Role.ToString()`); lê `Jwt:SecretKey/Issuer/Audience/ExpirationInMinutes` da configuração (expiração com fallback de 60 min se a config não for um `int` válido).
- `CurrentUserService : ICurrentUserService` — lê claims do `IHttpContextAccessor`. Registrada desde sempre, mas **sem nenhum consumidor até `feature/8-put-users`** — o endpoint `PATCH /users/{id}/password` (ver §6) é o primeiro código da Api a injetá-la de verdade, pra checar se quem chama é o dono do `userId` da rota.
- `PasswordService : IPasswordService` — `BCrypt.Net.BCrypt.HashPassword`/`Verify`, work factor 12. Registrado como `Singleton` (stateless). Ver §10 — não é um Domain Service aqui, é um serviço técnico de Application/Infrastructure.
- `DomainEventDispatcher : IDomainEventDispatcher` — resolve `IDomainEventHandler<TEvent>` via reflection (`serviceProvider.GetServices` + `MakeGenericType`) e invoca `HandleAsync` em cada handler registrado. **Nenhum handler concreto existe ainda no DI** — hoje o dispatch é um no-op. Ver §11.

### Composição (`IoC/DependencyInjection.cs`)
`AddInfrastructure()`:
- `AddRepositories`: registra `AppDbContext` (SqlServer via `ConnectionStrings:DefaultConnection`) e os 6 repositórios (`IUserRepository`, `ICustomerRepository`, `IVehicleRepository`, `IInventoryItemRepository`, `IServiceRepository`, `IServiceOrderRepository`) como `Scoped`.
- `AddServices`: `IPasswordService` (`Singleton`), `ICurrentUserService` (`Scoped`), `IJwtService` (`Scoped`).
- `IDomainEventDispatcher` (`Scoped`), `IHttpContextAccessor`.

## 6. Camada de API (`Fiap.Workshop.Api`)

> ⚠️ Reescrita em 2026-08-11 — desde a última passada (2026-08-08) ganhou o endpoint de login (`Auth`) e o primeiro endpoint de um agregado de negócio (`Customers`, criação). Atualizada em 2026-08-12 (branch `feature/8-put-users`) com `PUT /users/{id}` e `PATCH /users/{id}/password`. `GetUserById` (o antigo scaffold) continua sem existir como rota própria — `GetUsers` (lista) e `UpdateUser` cobrem o essencial que ele faria.

Minimal APIs organizadas por feature em `Endpoints/{Feature}/*Endpoints.cs`, registradas em `Endpoints/EndpointsExtensions.cs` → `app.MapMinimalApisV1()`.

### Endpoints existentes

**Users** (`/api/v{version}/users`)
- `GET /` — `GetUsers` (novo, `feature/7-get-users`). Tem `.RequireAuthorization("AdminOnly")`. Sem parâmetro nenhum (nem query, nem rota, nem corpo) — o `CorrelationId` passado ao use case é gerado inline no próprio endpoint (`Guid.NewGuid()`), não vem do cliente. Sempre retorna `Results.Ok(result)` (**só declara `Produces<Output>(200)`, sem `400`**) — consistente com `GetUsersUseCase` nunca marcar `IsValid = false` (ver §4): lista vazia também é `200 OK` com resultado vazio.
- `POST /` — `CreateUser`. Tem `.RequireAuthorization("AdminOnly")` — **exige um token JWT válido com role `Admin`**. Não há `.AllowAnonymous()`. Na prática isso continua sendo um problema de bootstrap real: sem nenhum Admin pré-existente (não há seed no `db/init.sql`) e sem `AllowAnonymous` em `CreateUser`, **não há caminho pela API pra criar o primeiro usuário** — o endpoint de login (abaixo) resolve "logar depois de ter um usuário", não "criar o primeiro usuário". Ver §9. Valida com `CreateUserRequestValidator` via `.WithValidation<CreateUserRequest>()`, chama `ICreateUserUseCase`. Retorna `201 Created` (`Location` lendo `result.GetResult<UserResponse>()?.Id` — tipo certo, ver §9 item 2) com o `Output` (contendo o `UserResponse` em `Result`) ou `400 BadRequest` com o `Output` (lista de erros).
- `PUT /{userId}` — `UpdateUser` (novo, `feature/8-put-users`). Tem `.RequireAuthorization("AdminOnly")`. `userId` vem da rota (`[FromRoute] Guid`, `[Required]`); corpo é `UpdateUserRequest(CorrelationId, Name, Email, Role)`. Retorna sempre `400 BadRequest` pra qualquer falha do use case (usuário não existe **ou** e-mail já em uso) — **decisão consciente, não um descuido**: diferente do `GET /customers/{id}` (que usa `404` porque só tem um jeito de falhar), `UpdateUserUseCase` bota os dois motivos no mesmo `IsValid = false`/`ErrorMessages`, sem nenhum campo que distinga "não encontrado" de "conflito"; segui o mesmo padrão do `CreateUser`/`CreateCustomer` (sempre `400`) em vez de inspecionar o texto da mensagem de erro dentro do endpoint pra decidir o status. Se um dia isso importar, a mudança certa é o `Output` carregar um código de erro, não o endpoint interpretando string.
- `PATCH /{userId}/password` — `ChangePassword` (novo, `feature/8-put-users`). `userId` vem da rota; corpo é `ChangePasswordRequest(CorrelationId, CurrentPassword, NewPassword)`. **Único endpoint com `.RequireAuthorization()` sem policy nomeada** — todos os outros usam `AdminOnly`/`AttendantOnly`; aqui a regra não é sobre role (qualquer usuário autenticado, seja `Admin`/`Attendant`/`Mechanic`, pode trocar a própria senha), é sobre identidade. Por isso o handler também injeta `ICurrentUserService` (**primeiro consumidor real dela na camada de Api** — antes só existia registrada no DI, sem nada usando) e compara `currentUser.UserId != userId` da rota, devolvendo `403 Forbidden` (`Results.Forbid()`) se não bater — essa checagem vive só no endpoint, não no use case (`ChangePasswordUseCase` trocaria a senha de qualquer `UserId` que receber; quem impõe "só a sua própria" é a Api). Fora isso, `400 BadRequest` pra "usuário não existe"/"senha atual inválida" (mesmo raciocínio de status único do `PUT`, acima), `200 OK` em sucesso.

**Auth** (`/api/v{version}/auth`) — novo desde a última reescrita
- `POST /login` — `LoginUser`. Tem `.AllowAnonymous()` — não exige autenticação (faz sentido, é o próprio ponto de entrada). Valida com `LoginRequestValidator` via `.WithValidation<LoginRequest>()`, chama `ILoginUserUseCase`. Retorna `200 OK` com o `Output` (token cru em `Result`) se válido, ou `401 Unauthorized` **sem corpo** se inválido (diferente do padrão `400 BadRequest` + `Output` usado nos outros endpoints — aqui o `Output`/lista de erros do use case é descartado).

**Customers** (`/api/v{version}/customers`)
- `GET /{customerId}` — `GetCustomer` (existia antes desta branch, não documentado até agora). Tem `.RequireAuthorization("AttendantOnly")`. `customerId` vem da rota (`[FromRoute] Guid`); `CorrelationId` é gerado inline (`Guid.NewGuid()`), igual ao `GET /users`. Retorna `404 NotFound` com o `Output` se `IsValid == false`, senão `200 OK`.
- `POST /` — `CreateCustomer`. Tem `.RequireAuthorization("AttendantOnly")` (roles `Attendant`/`Admin`) — **primeiro endpoint a usar uma policy diferente de `AdminOnly`**. Valida com `CreateCustomerRequestValidator` via `.WithValidation<CreateCustomerRequest>()`, chama `ICreateCustomerUseCase`. Retorna `201 Created` (`Location` lendo `result.GetResult<CustomerResponse>()?.Id` — tipo certo, ver §9 item 2) com o `Output` ou `400 BadRequest` com o `Output`.

`CorrelationId` (`Guid`, `NotEmpty`) é um **campo do corpo** em todo request que tem corpo (`CreateUserRequest`, `LoginRequest`, `CreateCustomerRequest`, `UpdateUserRequest`, `ChangePasswordRequest` — `POST`/`PUT`/`PATCH`), não um header — não existe leitura de `x-correlation-id` em lugar nenhum do código atual. Nos dois `GET` (`GetUsers`, `GetCustomer`), que não têm corpo, o padrão é diferente: o próprio endpoint gera um `Guid.NewGuid()` inline na chamada ao use case, o cliente não tem como fornecer/propagar um `CorrelationId` nessas rotas.

Não existe grupo próprio nem rota para `UpdateEmailRequest` (`Requests/Users/`) — segue como record órfão, sem mapper, validator, use case ou rota.

### Requests / Validators / Mappers
- `CreateUserRequest(CorrelationId, Name, Email, Password, PasswordConfirmation, Role)` — validado por `CreateUserRequestValidator`: `CorrelationId` obrigatório; `Name` obrigatório (≤200); `Email` obrigatório + `.EmailAddress()` (sem limite de tamanho explícito no validator); `Password` obrigatório, mínimo 8 caracteres (**sem** checar maiúscula/minúscula/dígito); `PasswordConfirmation` igual a `Password`; `Role` dentro do enum (`Admin`/`Attendant`/`Mechanic`, ver §3). Não há nenhuma validação equivalente no Domain (ver §10) — hoje é só a API que garante isso, sem "defesa em profundidade". Mapper: `CreateUserMapper.MapToInput` (`Api/Mappers/`).
- `UpdateUserRequest(CorrelationId, Name, Email, Role)` (novo, `feature/8-put-users`) — validado por `UpdateUserRequestValidator`: mesmas regras de `CreateUserRequestValidator` menos as de senha (não existe campo `Password` aqui). Mapper: `UpdateUserMapper.MapToInput(this UpdateUserRequest request, Guid userId)` (`Api/Mappers/`) — assinatura diferente dos outros mappers HTTP↔Application porque o `userId` vem da rota, não do corpo, e precisa ser combinado com o request pra montar o `UpdateUserInput`.
- `ChangePasswordRequest(CorrelationId, CurrentPassword, NewPassword)` (novo, `feature/8-put-users`) — validado por `ChangePasswordRequestValidator`: `CorrelationId`/`CurrentPassword` obrigatórios; `NewPassword` obrigatório + mínimo 8 caracteres (mesma regra frouxa do `CreateUserRequestValidator`, sem checar maiúscula/minúscula/dígito; **sem** checar `NewPassword != CurrentPassword`, omissão consciente, não pedida). Mapper: `ChangePasswordMapper.MapToInput(this ChangePasswordRequest request, Guid userId)` — mesmo molde do `UpdateUserMapper` (`userId` da rota).
- `LoginRequest(CorrelationId, Email, Password)` — validado por `LoginRequestValidator`: `CorrelationId` obrigatório; `Email` obrigatório + `.EmailAddress()`; `Password` obrigatório, mínimo 8 caracteres. Mapper: `LoginUserMapper.MapToInput` (`Api/Mappers/`).
- `CreateCustomerRequest(CorrelationId, Name, Document, Email, Phone)` — validado por `CreateCustomerRequestValidator`: `CorrelationId` obrigatório; `Name` obrigatório (≤200); `Document` obrigatório, `Cascade(CascadeMode.Stop)` + tamanho 11 ou 14 + `IsValidDocument()` (dígito verificador real de CPF/CNPJ, via `CpfCnpjLibrary` — ver §2/§4); `Email` obrigatório + `.EmailAddress()`. **`Phone` não tem nenhuma regra de validação** (nem `NotEmpty`) — só existe validação de CPF/CNPJ, não de telefone, hoje. Mapper: `CreateCustomerMapper.MapToInput` (`Api/Mappers/`).
- `ValidationFilter<TRequest>` (`Filters/`) — `IEndpointFilter` genérico: resolve `IValidator<TRequest>` do DI, roda `ValidateAsync`, e se inválido retorna `400 BadRequest` com um `Output` (`Application.Commons`) preenchido via `AddErrorMessages`. Aplicado via `.WithValidation<TRequest>()` (`EndpointFilterExtensions`).

### Composição (`IoC/DependencyInjection.cs`)
`AddApi()`: `AddValidators` (registra `IValidator<CreateUserRequest>`, `IValidator<UpdateUserRequest>`, `IValidator<ChangePasswordRequest>`, `IValidator<LoginRequest>`, `IValidator<CreateCustomerRequest>`, todos `Scoped`), `AddRoles` (`AddAuthorizationBuilder` com as três policies `AdminOnly`/`AttendantOnly`/`MechanicOnly`, ver §2 — hoje só `AdminOnly` (`CreateUser`) e `AttendantOnly` (`CreateCustomer`) são de fato exercitadas por um endpoint; `MechanicOnly` ainda não tem consumidor), `AddJwtConfig` (JWT Bearer lendo `Jwt:Issuer/Audience/SecretKey`, `AddApiVersioning` com URL segment reader e versão default 1 + `AddApiExplorer` com `SubstituteApiVersionInUrl = true`, `AddProblemDetails`, OpenAPI com security scheme Bearer). **Fix aplicado em 2026-08-11** (commit `fe845b2`, "fix(ioc): dependency injection bearer fix"): o documento OpenAPI declarava o `SecurityScheme` "Bearer" nos `Components`, mas não adicionava o `document.Security` global exigindo esse scheme — o botão "Authorize" do Swagger UI aparecia, mas o token informado não era de fato anexado às requisições de teste feitas pela UI. Corrigido adicionando `document.Security.Add(new OpenApiSecurityRequirement { [Bearer] = [] })` no mesmo `AddDocumentTransformer`.

### `Program.cs`
Pipeline: `AddControllers` (não usado, pois tudo é Minimal API — resquício de template), `AddEndpointsApiExplorer`, `AddApplication → AddInfrastructure → AddApi`, `MapOpenApi + UseSwaggerUI` (docs em `/swagger`, aponta pro JSON em `/openapi/v1.json`), `UseExceptionHandler`, `UseHttpsRedirection`, `UseAuthentication`, `UseAuthorization`, `UseSerilogRequestLogging`, `MapMinimalApisV1()`.

## 7. Docker / Deploy

- **Dockerfile**: multi-stage (`sdk:10.0` restore → publish → `aspnet:10.0` runtime), copia apenas os `.csproj` primeiro (cache de layers), expõe porta `8080`.
- **docker-compose.yml**: serviços `api` (build local) + `sqlserver` (`mssql/server:2022-latest`) + `sqlserver-init` (2026-08-03, novo), healthcheck via `sqlcmd`, volume nomeado `sqlserver-data`. Variáveis vêm do `.env`.
- **`sqlserver-init`** (2026-08-03): serviço descartável (`restart: "no"`) que roda a mesma imagem `mssql/server:2022-latest` com `entrypoint` sobrescrito pra executar `/opt/mssql-tools18/bin/sqlcmd -i /init.sql` (monta `db/init.sql` como volume) contra o serviço `sqlserver` assim que ele fica `healthy`. `api` agora depende de `sqlserver-init` com `condition: service_completed_successfully` (não mais de `sqlserver: service_healthy` diretamente) — o banco já está com schema pronto antes da API subir.
- **docker-compose.tests.yml**: SQL Server isolado (`integration-sql`) para os testes de integração, senha fixa `Integration@Test123`. **Não** tem serviço de init próprio — quem aplica o schema é a `DatabaseFixture` (C#), lendo o mesmo `db/init.sql` compartilhado. Importante: **não há nenhum seed de dados** nesse fluxo — `db/init.sql` só tem `CREATE TABLE`/`CREATE INDEX`, nenhum `INSERT`. Cada classe de teste de integração é responsável por inserir os próprios dados no Arrange (via use case real ou repositório direto); como `DatabaseFixture` é `ICollectionFixture` compartilhado por toda a collection `"Integration"` sem reset entre testes, cenários que dependem da tabela estar vazia não são confiáveis nesse nível (ver `GetUsers`, §4).
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

> ⚠️ Lista revisada em 2026-08-12 contra o código real (branch `feature/8-put-users`) — login, `GetCustomer`/`GetUsers`, `UpdateUser` e agora `ChangePassword` (Application **e** Api) já existem, mas o problema de bootstrap de Admin continua sem solução.

1. **Bootstrap de Admin (ainda sem solução)**: `POST /users` exige `RequireAuthorization("AdminOnly")`, sem `AllowAnonymous`, e não há seed de usuário Admin em `db/init.sql`. O endpoint de login (`POST /api/v1/auth/login`) já existe (desde a última reescrita) e funciona para autenticar um usuário **que já existe**, mas não resolve o problema original: **ainda não há nenhum caminho pela API para criar o primeiro usuário** (login pressupõe um usuário previamente criado por outro meio — direto no banco, hoje). Precisa de uma decisão: seed de Admin no `init.sql`, endpoint de bootstrap protegido por secret, ou tornar `CreateUser` público e restringir `Role: Admin` na validação/regra de negócio.
2. ~~`GetResult<Guid>()` no endpoint `CreateCustomer` provavelmente quebra em runtime`~~ — **não reproduz mais no código atual** (revisado em 2026-08-12): tanto `CustomersEndpoints.MapPost` quanto `UsersEndpoints.MapPost` usam `result.GetResult<CustomerResponse>()?.Id` / `result.GetResult<UserResponse>()?.Id`, com o tipo certo — não `GetResult<Guid>()`. Ou já foi corrigido silenciosamente em algum commit anterior, ou a descrição anterior deste documento nunca bateu com o código; de qualquer forma, hoje não é um problema. Continua valendo o ponto de fundo: `Fiap.Workshop.FunctionalTests` segue vazio (ver item 5), então esse tipo de bug de composição HTTP não tem uma camada de teste dedicada pra pegá-lo caso volte a acontecer.
3. **`.env` versionado no git** com segredos de exemplo (SA_PASSWORD, Jwt SecretKey) — confirmar se é intencional para o workshop ou se deveria ir para `.gitignore`.
4. **Mocking duplicado nos testes**: `Fiap.Workshop.UnitTests.csproj` referencia tanto `Moq` quanto `NSubstitute`; os testes atuais usam apenas `Moq`. Vale decidir um padrão único.
5. **`Fiap.Workshop.FunctionalTests`** existe como projeto mas não tem nenhum arquivo de teste ainda — scaffold vazio. Sem ele, bugs de composição HTTP (ver item 2) não são pegos por nenhuma camada de teste.
6. **Domain events sem consumidor**: mecânica pronta e correta (ver §11), mas nenhum `IDomainEventHandler` está registrado no DI — o dispatch acontece e não aciona nada. E-mail de boas-vindas planejado como primeiro handler, ver roadmap em §11. `Customer` nem dispara evento próprio ainda (ver §3).
7. **Escopo do Tech Challenge (Fase 1) — progresso desde a última passada**: o enunciado (`15SOAT - Fase 1 - Tech Challenge (1).pdf`, na raiz) pede um sistema de oficina mecânica — CRUD de clientes/veículos/serviços/peças (com controle de estoque), Ordem de Serviço com máquina de estados, orçamento automático, validação de CPF/CNPJ e placa, cobertura de teste mínima de 80% nos domínios críticos, relatório de vulnerabilidades (SAST) e documentação DDD.
   - **Cliente**: tem criação e consulta por id (`CreateCustomer` + `GetCustomer` — Domain→Application→Infrastructure→Api, ver §3/§4/§6), incluindo validação de CPF/CNPJ (dígito verificador via `CpfCnpjLibrary`, ver §2/§6). Ainda falta listagem, atualização e remoção de cliente.
   - **Veículo, Serviço, Peça/Insumo, Ordem de Serviço**: continuam só com Domain e Infrastructure prontos (entidades, models, repositórios, mapeamentos, migration) — falta a camada de Application (use cases) e Api (endpoints) por cima deles.
   - **Ainda faltando por completo**: validação de placa, máquina de estados de OS, orçamento automático, controle de estoque, cobertura de 80%, relatório de vulnerabilidades (SAST), documentação DDD.
   - Ver seção "Roadmap / pendências do desafio" do `README.md` para a lista cobrada pelo enunciado — **está desatualizada** (ainda diz que login e `Customer` não existem); vale revisar numa próxima passada.
8. **`GetUsersUseCase` devolve dois formatos diferentes em `Output.Result`** (novo, `feature/7-get-users`, ver §4): `Array.Empty<User>()` (entidade de domínio) quando a tabela está vazia, `IEnumerable<UserResponse>` (DTO) quando não está. Como `Result` é `object?`, isso não quebra a build, mas o JSON devolvido pelo `GET /api/v1/users` muda de forma dependendo do estado do banco. Simplificar para sempre devolver `users.MapToDto()` (que já lida com lista vazia sem precisar do `if`) resolve.
9. ~~`ChangePasswordUseCase` existe sem endpoint Api ainda~~ — **resolvido em 2026-08-12**: `PATCH /api/v1/users/{id}/password` implementado (ver §6), com a checagem de identidade (`ICurrentUserService.UserId == userId` da rota) que faz da rota um self-service de verdade.
10. **Troca de senha não revoga tokens JWT já emitidos** (novo, `feature/8-put-users`, ver §4): decisão consciente de escopo, não descuido — o `JwtService` é stateless (sem `jti`, sem sessão, sem denylist), então revogar um token específico exigiria uma mudança estrutural na autenticação (`TokenVersion`/`SecurityStamp` + validação customizada no pipeline JWT, ou denylist, ou refresh tokens). Registrado aqui pra não se perder: se a troca de senha precisar "deslogar" o usuário de outros dispositivos no futuro, é esse o ponto de partida.
11. **A checagem de autorização do `PATCH /users/{id}/password` (`403` se `userId` da rota não for o do chamador) não tem cobertura de teste nenhuma** (novo, `feature/8-put-users`, ver §6): diferente da lógica de negócio de `ChangePasswordUseCase` (testada em 3 camadas), essa regra vive só no lambda do endpoint na Api, e como `Fiap.Workshop.FunctionalTests` continua vazio (item 5), nenhum teste automatizado passa por ela — nem foi verificada manualmente ainda, porque isso esbarra na mesma pendência do bootstrap de Admin (item 1): sem um jeito de emitir um JWT de teste, não dá pra bater na rota de verdade. É a parte mais sensível do que foi construído em `feature/8-put-users` e a única sem nenhuma verificação.

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
