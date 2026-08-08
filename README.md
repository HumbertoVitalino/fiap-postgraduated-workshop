# Fiap.Workshop — Sistema Integrado de Atendimento e Execução de Serviços

Projeto do **Tech Challenge — Fase 1** da pós-graduação FIAP em Arquitetura de Software (SOAT).

## O desafio

Uma oficina mecânica de médio porte, especializada em manutenção de veículos, enfrenta dificuldades para crescer com qualidade e eficiência. Hoje o atendimento, o diagnóstico, a execução dos serviços e a entrega dos veículos são controlados de forma manual (anotações e planilhas), o que gera erros de priorização no atendimento, falhas no controle de peças e insumos, dificuldade em acompanhar o status dos serviços, perda de histórico de clientes/veículos e um fluxo de orçamento e aprovação ineficiente.

Este repositório é a primeira versão (MVP) do **back-end** de um Sistema Integrado de Atendimento e Execução de Serviços para essa oficina: o objetivo final é permitir que o cliente acompanhe em tempo real o andamento do serviço e autorize reparos adicionais, enquanto a equipe da oficina ganha uma gestão interna — de ordens de serviço, clientes, veículos e peças — eficiente e segura.

O desenvolvimento segue **Domain-Driven Design (DDD)**, com atenção a boas práticas de qualidade de software e segurança: arquitetura em camadas, testes automatizados, autenticação JWT e validação de dados sensíveis (CPF/CNPJ, placa de veículo).

> ⚠️ **Estado atual do projeto**: só o fluxo de **criação de usuário** (`POST /api/v1/users`) está com a pilha completa (Domain → Application → Infrastructure → Api). Os agregados de negócio da oficina (`Cliente`, `Veículo`, `Serviço`, `Peça/Insumo`, `Ordem de Serviço`) já têm Domain e Infrastructure prontos (entidades, persistência, repositórios), mas ainda sem use cases nem endpoints por cima. Login e os demais fluxos de usuário existem só como rascunho. Ver [Roadmap / pendências do desafio](#roadmap--pendências-do-desafio) abaixo e [`CONTEXT.md`](CONTEXT.md) para o detalhamento técnico completo.

## Sumário

- [O desafio](#o-desafio)
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

- Cadastro de usuário (`POST /api/v1/users`), com validação de e-mail único e senha (mínimo 8 caracteres). Endpoint exige um token JWT com role `Admin` — ainda não há um jeito de emitir esse token (ver observação abaixo), então na prática o endpoint só é exercitável em testes.
- Entidades, persistência (EF Core) e repositórios para todos os agregados de negócio da oficina (`Cliente`, `Veículo`, `Serviço`, `Peça/Insumo`, `Ordem de Serviço`) — só falta a camada de use cases/endpoints por cima.
- Schema de banco (`db/init.sql`) aplicado por um serviço de init do `docker-compose` antes da API subir — sem migration em runtime.
- Testes unitários cobrindo Domain e Application; projeto de testes de integração com SQL Server real via `docker-compose.tests.yml`.

> Login (`POST /api/v1/auth/login`) e consulta de usuário por id ainda **não existem** — por isso hoje não há um caminho pela API para obter um token JWT e efetivamente chamar `POST /api/v1/users`. É a próxima pendência a resolver (ver roadmap).

## Roadmap / pendências do desafio

Funcionalidades obrigatórias do Tech Challenge que **ainda não foram implementadas**:

- **Login e bootstrap de usuário Admin**: hoje não existe endpoint de login nem seed de usuário administrador — sem isso, não há como obter um token pra chamar `POST /api/v1/users`. Bloqueia o uso ponta a ponta da API.
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

3. A API estará disponível em `http://localhost:8080` (porta configurável via `API_PORT` no `.env`). O schema do banco é criado por um serviço de init (`sqlserver-init`) rodando `db/init.sql` antes da API subir.

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

`POST /api/v1/users` exige um token JWT com role `Admin` (policy `AdminOnly`). Não existe hoje endpoint de login nem qualquer outro jeito de emitir esse token — é uma pendência conhecida (ver roadmap e `CONTEXT.md`).

O `CorrelationId` esperado pelo cadastro de usuário vai no corpo da requisição (campo `correlationId`), não em um header.

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
