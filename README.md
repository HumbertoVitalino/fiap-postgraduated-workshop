# Fiap.Workshop — Sistema Integrado de Atendimento e Execução de Serviços

Projeto do **Tech Challenge — Fase 1** da pós-graduação FIAP em Arquitetura de Software (SOAT).

## O desafio

Uma oficina mecânica de médio porte, especializada em manutenção de veículos, enfrenta dificuldades para crescer com qualidade e eficiência. Hoje o atendimento, o diagnóstico, a execução dos serviços e a entrega dos veículos são controlados de forma manual (anotações e planilhas), o que gera erros de priorização no atendimento, falhas no controle de peças e insumos, dificuldade em acompanhar o status dos serviços, perda de histórico de clientes/veículos e um fluxo de orçamento e aprovação ineficiente.

Este repositório é a primeira versão (MVP) do **back-end** de um Sistema Integrado de Atendimento e Execução de Serviços para essa oficina: o objetivo final é permitir que o cliente acompanhe em tempo real o andamento do serviço e autorize reparos adicionais, enquanto a equipe da oficina ganha uma gestão interna — de ordens de serviço, clientes, veículos e peças — eficiente e segura.

O desenvolvimento segue **Domain-Driven Design (DDD)**, com atenção a boas práticas de qualidade de software e segurança: arquitetura em camadas, testes automatizados, autenticação JWT e validação de dados sensíveis (CPF/CNPJ, placa de veículo).

> **Estado atual do projeto**: todas as funcionalidades obrigatórias do desafio estão implementadas — os quatro agregados de negócio da oficina (`Cliente`, `Veículo`, `Serviço`, `Peça/Insumo`) têm CRUD completo, a máquina de estados da Ordem de Serviço está fechada de ponta a ponta (`Recebida` → `Entregue`) com listagem e detalhamento administrativos, e o cliente já consegue acompanhar sua OS via API pública, sem autenticação. Restam só itens de qualidade/entrega (cobertura medida, relatório de SAST, documentação DDD) — ver [O que falta](#o-que-falta) abaixo e [`CONTEXT.md`](CONTEXT.md) para o detalhamento técnico completo.

## Sumário

- [O desafio](#o-desafio)
- [Arquitetura](#arquitetura)
- [Stack técnica](#stack-técnica)
- [O que já está implementado](#o-que-já-está-implementado)
- [O que falta](#o-que-falta)
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
| Autenticação | JWT Bearer, policies `AdminOnly` / `AttendantOnly` / `MechanicOnly` |
| Hash de senha | BCrypt.Net-Next (work factor 12) |
| Persistência | EF Core 10 + **SQL Server** |
| Validação | FluentValidation |
| Logging | Serilog (console + request logging) |
| Testes | xUnit, FluentAssertions, Moq/NSubstitute, AutoFixture |

**Por que SQL Server?** O domínio da oficina é fortemente relacional (OS ⇄ cliente ⇄ veículo ⇄ itens de serviço/peça, com integridade referencial e transações que precisam ser consistentes — ex.: baixa de estoque ao aprovar uma OS). SQL Server foi escolhido por ser um banco relacional maduro, com bom suporte a migrations via EF Core e por já estar configurado no ambiente de containers do time.

## O que já está implementado

- **Usuários**: cadastro (`POST /api/v1/users`, `AdminOnly`), login (`POST /api/v1/auth/login`), listagem, atualização de perfil, troca de senha (self-service) e remoção.
- **Clientes**: CRUD completo — criação com validação de CPF/CNPJ, consulta por id, listagem, atualização (documento é imutável após o cadastro) e remoção (bloqueada com `400` se o cliente tiver veículos vinculados).
- **Veículos**: CRUD completo — criação com validação de placa (Mercosul e padrão antigo) vinculada a um cliente existente, consulta por id, listagem, atualização (placa imutável) e remoção (bloqueada com `400` se houver OS vinculada).
- **Serviços** (catálogo): CRUD completo — criação, consulta por id, listagem, atualização (código imutável; desativar um serviço o impede de entrar em orçamentos novos) e remoção (bloqueada com `400` se o serviço já tiver sido usado em alguma OS). O tempo estimado de execução (`EstimatedDuration`) é uma média incremental que se autoatualiza a cada OS concluída, com `ExecutionCount` exposto para indicar quantas amostras embasam o número — é como o desafio pede o "monitoramento do tempo médio de execução dos serviços".
- **Peças/Insumos** (catálogo + estoque): CRUD completo — criação, consulta por id, listagem, atualização (código e quantidades de estoque ficam de fora: estoque só muda pelo fluxo de reserva/orçamento) e remoção (bloqueada com `400` se a peça já tiver sido usada em alguma OS). Desativar uma peça a impede de entrar em orçamentos novos.
- **Ordem de Serviço**: máquina de estados completa — `Recebida` → `Em diagnóstico` → `Aguardando aprovação` → (`Em execução` → `Finalizada` | `Cancelada`) → `Entregue`, com orçamento calculado automaticamente a partir dos serviços/peças informados, reserva e baixa real de estoque, e histórico de status registrado a cada transição. Listagem (`GET /api/v1/service-orders`) e detalhamento por id (`GET /api/v1/service-orders/{id}`) para gestão administrativa. Consulta pública e anônima da OS pelo cliente via CPF/CNPJ + placa (`GET /api/v1/service-orders/lookup`), sem vazar existência de cadastro.
- Schema de banco (`db/init.sql`) aplicado por um serviço de init do `docker-compose` antes da API subir — sem migration em runtime. Inclui seed idempotente do primeiro usuário Admin (bootstrap).
- Testes unitários (Domain + Application) e projeto de testes de integração com SQL Server real via `docker-compose.tests.yml`, cobrindo os fluxos acima.

Ver [`CONTEXT.md`](CONTEXT.md) para o desenho técnico completo (camada por camada) e `docs/backlog-proximas-tarefas.md` para o histórico de tickets já fechados.

## O que falta

Do que o desafio pede, ainda em aberto:

- **Cobertura mínima de testes de 80% nos domínios críticos**: o pipeline já roda Coverlet + SonarQube Cloud a cada push em `release`, mas o número ainda não foi medido/confirmado contra a meta.
- **Relatório de análise de vulnerabilidades (SAST)**: a análise em si já roda no CI (SonarQube Cloud), mas o relatório documentado com os achados ainda não foi escrito.
- **Documentação DDD** (Event Storming, diagramas, linguagem ubíqua) dos fluxos de OS e de gestão de peças/insumos — entregável separado do código, ainda não iniciado.

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

3. A API estará disponível em `http://localhost:8080` (porta configurável via `API_PORT` no `.env`). O schema do banco é criado por um serviço de init (`sqlserver-init`) rodando `db/init.sql` antes da API subir — inclui o seed de um usuário Admin de bootstrap:

   | Campo | Valor |
   |---|---|
   | Email | `admin@admin.com` |
   | Senha | `Admin@123` |

   Use essas credenciais em `POST /api/v1/auth/login` pra obter um token JWT com role `Admin` e conseguir chamar endpoints protegidos (ex.: `POST /api/v1/users`, pra cadastrar os demais usuários). Troque essa senha em qualquer ambiente que não seja local/dev.

4. Logo em seguida, outro serviço (`sqlserver-seed-demo`) popula o banco com dados de demonstração — 2 usuários extras, 4 clientes, 5 veículos, catálogo de serviços/peças com estoque e 6 Ordens de Serviço cobrindo cada status da máquina de estados, com histórico completo (ver `db/seed-demo.sql`). Não é aplicado nos testes de integração, só neste ambiente. Credenciais dos usuários extras (senha `Demo@123` pros dois):

   | Papel | Email |
   |---|---|
   | Attendant | `atendente@oficina.com` |
   | Mechanic | `mecanico@oficina.com` |

5. Para derrubar o ambiente:

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

`POST /api/v1/users` exige um token JWT com role `Admin` (policy `AdminOnly`). Obtenha um token via `POST /api/v1/auth/login`, usando o usuário Admin de bootstrap (`admin@admin.com` / `Admin@123`, ver "Como rodar localmente") — troque essa senha em qualquer ambiente que não seja local/dev.

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
