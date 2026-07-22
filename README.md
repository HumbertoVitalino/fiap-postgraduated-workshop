# Fiap.Workshop — Sistema Integrado de Atendimento e Execução de Serviços

API back-end para o **Tech Challenge — Fase 1** da pós-graduação FIAP em Arquitetura de Software (SOAT). O desafio proposto é o MVP de um sistema para uma oficina mecânica de médio porte, cobrindo gestão de ordens de serviço (OS), clientes, veículos, serviços e peças, aplicando **Domain-Driven Design** e boas práticas de qualidade e segurança.

> ⚠️ **Estado atual do projeto**: apenas o módulo de **autenticação e cadastro de usuários** (login JWT, roles `Admin`/`User`) está implementado. Os domínios de negócio da oficina (Ordem de Serviço, Cliente, Veículo, Serviço, Peça/Estoque) ainda **não existem no código** — ver [Roadmap / pendências do desafio](#roadmap--pendências-do-desafio) abaixo. Detalhes técnicos completos e o histórico de decisões de design ficam em [`CONTEXT.md`](CONTEXT.md).

## Sumário

- [Arquitetura](#arquitetura)
- [Stack técnica](#stack-técnica)
- [O que já está implementado](#o-que-já-está-implementado)
- [Roadmap / pendências do desafio](#roadmap--pendências-do-desafio)
- [Como rodar localmente](#como-rodar-localmente)
- [Rodando os testes](#rodando-os-testes)
- [Documentação da API (Swagger)](#documentação-da-api-swagger)
- [Autenticação](#autenticação)
- [Estrutura do repositório](#estrutura-do-repositório)

## Arquitetura

Clean Architecture em camadas (monolito, conforme requisito técnico do desafio):

```
Domain          →  entidades, value objects, eventos de domínio — zero dependências externas
Application     →  use cases, boundaries (Input/Output), interfaces (portas)
Infrastructure  →  EF Core, repositórios, serviços concretos (JWT, hashing, dispatcher de eventos)
Api             →  Minimal APIs, composição de DI, validadores, mapeadores HTTP ↔ Application
```

Regra de dependência: `Domain` não referencia nada; `Application` referencia `Domain`; `Infrastructure` referencia `Application`; `Api` referencia `Infrastructure` (e, transitivamente, todo o resto).

## Stack técnica

| Categoria | Escolha |
|---|---|
| Runtime | .NET 10 / C# `latest` |
| API | Minimal APIs + `Asp.Versioning` (versionamento por segmento de URL, `/api/v1/...`) |
| Documentação | `Microsoft.AspNetCore.OpenApi` + Swagger UI (`/swagger`) |
| Autenticação | JWT Bearer, policies `AdminOnly` / `UserOnly` |
| Hash de senha | BCrypt.Net-Next (work factor 12) |
| Persistência | EF Core 10 + **SQL Server** |
| Validação | FluentValidation |
| Logging | Serilog (console + request logging) |
| Testes | xUnit, FluentAssertions, Moq/NSubstitute, AutoFixture |

**Por que SQL Server?** O domínio da oficina é fortemente relacional (OS ⇄ cliente ⇄ veículo ⇄ itens de serviço/peça, com integridade referencial e transações que precisam ser consistentes — ex.: baixa de estoque ao aprovar uma OS). SQL Server foi escolhido por ser um banco relacional maduro, com bom suporte a migrations via EF Core e por já estar configurado no ambiente de containers do time.

## O que já está implementado

- Cadastro de usuário (`POST /api/v1/users`), com validação de força de senha (mínimo 10 caracteres, maiúscula, minúscula e dígito) e e-mail único.
- Login (`POST /api/v1/auth/login`) com emissão de JWT.
- Consulta de usuário por id (`GET /api/v1/users/{id}`), autenticada.
- Autorização baseada em roles (`User`, `Admin`).
- Migrations de banco (EF Core) aplicadas automaticamente no startup da API.
- Testes unitários cobrindo Domain e Application; projeto de testes de integração com SQL Server real via `docker-compose.tests.yml`.

## Roadmap / pendências do desafio

Funcionalidades obrigatórias do Tech Challenge que **ainda não foram implementadas**:

- **Criação de OS**: identificação do cliente por CPF/CNPJ, cadastro de veículo (placa/marca/modelo/ano), inclusão de serviços e peças, orçamento automático, envio para aprovação do cliente.
- **Acompanhamento de OS**: máquina de status (`Recebida` → `Em diagnóstico` → `Aguardando aprovação` → `Em execução` → `Finalizada` → `Entregue`), com consulta via API pelo cliente.
- **Gestão administrativa**: CRUD de clientes, veículos, serviços, peças/insumos (com controle de estoque), listagem/detalhamento de OS, monitoramento do tempo médio de execução.
- **Validação de dados sensíveis**: CPF/CNPJ e placa de veículo.
- Cobertura mínima de testes de **80% nos domínios críticos** (ainda não medida/garantida).
- Relatório de análise de vulnerabilidades (SAST) do código.
- Documentação DDD (Event Storming, diagramas, linguagem ubíqua) dos fluxos de OS e de gestão de peças/insumos.

O histórico de decisões e o desenho detalhado do que já existe estão em [`CONTEXT.md`](CONTEXT.md).

## Como rodar localmente

Pré-requisitos: [Docker](https://www.docker.com/) e Docker Compose.

1. Copie o arquivo de variáveis de ambiente de exemplo:

   ```bash
   cp .env.example .env
   ```

2. Suba a aplicação e o banco de dados:

   ```bash
   docker compose up -d --build
   ```

3. A API estará disponível em `http://localhost:8080` (porta configurável via `API_PORT` no `.env`). As migrations do EF Core rodam automaticamente no startup.

4. Para derrubar o ambiente:

   ```bash
   docker compose down
   ```

### Rodando sem Docker (.NET SDK local)

Requer .NET SDK 10 e uma instância de SQL Server acessível (ajuste `ConnectionStrings__DefaultConnection` em `src/Fiap.Workshop.Api/appsettings.Development.json` ou via variável de ambiente):

```bash
dotnet restore Fiap.Workshop.slnx
dotnet run --project src/Fiap.Workshop.Api
```

## Rodando os testes

**Testes unitários** (Domain + Application, sem dependências externas):

```bash
dotnet test tests/Fiap.Workshop.UnitTests
```

**Testes de integração** (sobem um SQL Server dedicado em container):

```bash
docker compose -f docker-compose.tests.yml up -d
dotnet test tests/Fiap.Workshop.IntegrationTests
```

**Testes funcionais**: projeto `tests/Fiap.Workshop.FunctionalTests` existe no scaffold, mas ainda não tem testes escritos.

## Documentação da API (Swagger)

Com a aplicação em execução, acesse:

```
http://localhost:8080/swagger
```

O documento OpenAPI puro fica em `/openapi/v1.json`.

## Autenticação

1. Crie um usuário: `POST /api/v1/users` (endpoint público).
2. Faça login: `POST /api/v1/auth/login`, com `email`/`password` — retorna um token JWT.
3. Use o token no header `Authorization: Bearer {token}` para chamar endpoints protegidos (ex.: `GET /api/v1/users/{id}`).

Endpoints protegidos exigem também o header `x-correlation-id` (obrigatório em `POST /users`, opcional nos demais — inconsistência conhecida, ver `CONTEXT.md`).

## Estrutura do repositório

```
src/
  Fiap.Workshop.Domain          # Entidades, Value Objects, eventos de domínio
  Fiap.Workshop.Application     # Use cases, boundaries, interfaces (portas)
  Fiap.Workshop.Infrastructure  # EF Core, repositórios, serviços concretos
  Fiap.Workshop.Api             # Minimal API endpoints, composição de DI, validadores
tests/
  Fiap.Workshop.UnitTests           # Domain e Application isolados
  Fiap.Workshop.IntegrationTests    # EF Core real contra SQL Server
  Fiap.Workshop.FunctionalTests     # Scaffold, sem testes ainda
```

Documentação técnica viva (arquitetura, decisões de design, pendências): [`CONTEXT.md`](CONTEXT.md).

Link para o Miro: [Miro](https://miro.com/welcomeonboard/YTlnZGFLbXd2UmRNODRDdTk3d0owcXJ3V3lxakN1b0x6dXlJMVpRK2FaL05GcE0vbk5QUVBQUzBxZW5XNlRoc1ltQ01FS1pnMjY0K0pmLzJrWFRTR1p0cDlxSTRWWHF5YVpQcVZraXdudzF5SVNjYncyUVA4cGpXMTVZL0g1ZU1NakdSWkpBejJWRjJhRnhhb1UwcS9BPT0hdjE=?share_link_id=69986103094)
