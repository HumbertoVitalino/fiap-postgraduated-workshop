# Backlog — Fase 2 (Tech Challenge)

> Criado em 2026-10-25. Enunciado: `SOAT - Fase 2 - Tech challenge.pdf` (raiz). Continua a numeração do backlog da Fase 1 (tickets 1-30; o 31 foi absorvido pelo 23).
>
> Estratégia: tudo é construído e validado **localmente** (docker-compose + cluster kind/k3d) e a infraestrutura na Azure só é provisionada no fim, para rodar o CD de verdade e gravar o vídeo — depois `terraform destroy`.

## Índice

| # | Feature | Status |
|---|---|---|
| 32 | Listagem de OS ordenada por prioridade de status | ✅ feito (revalidar antes da entrega) |
| 33 | Consulta de status da OS | ✅ feito |
| 34 | Health checks (liveness/readiness) | ✅ feito |
| 35 | Testes funcionais (`WebApplicationFactory`) | ⏳ a fazer |
| 36 | Webhook de aprovação/recusa de orçamento | ⏳ a fazer |
| 37 | Notificação de status da OS por e-mail | ✅ feito |
| 38 | Refatoração — dívida técnica e cobertura | ⏳ a fazer |
| 39 | Manifestos Kubernetes (`/k8s`) | ⏳ a fazer |
| 40 | Terraform (`/infra`) — Azure | ⏳ a fazer |
| 41 | CI/CD com deploy no cluster | ⏳ a fazer |
| 42 | Entrega — README, collection, carga, vídeo | ⏳ a fazer |

## Decisões já fechadas

- **Abertura de OS mantida como está** (`POST /service-orders` com `CustomerId` + `VehicleId`; serviços e peças continuam entrando só pelo `AddBudget`, no diagnóstico). O PDF pede que a abertura receba "os dados do cliente, veículo, serviços e peças". **Decisão do usuário, contra a recomendação do assistente**, que era receber os dados de cliente/veículo e fazer find-or-create por documento/placa (mantendo serviços/peças fora). Justificativa a apresentar no README/vídeo: o orçamento reserva estoque, e o que será usado só se sabe após o diagnóstico do mecânico; cliente e veículo já têm cadastro próprio. Pode ser reaberta.
- **E-mail: MailKit direto**, atrás de `IEmailService`. FluentEmail descartado: sem release desde 2022 (`FluentEmail.Razor` desde 2020, dependente de RazorLight beta), `FluentEmail.Smtp` usa `System.Net.Mail.SmtpClient` (não recomendado pela Microsoft para código novo), `FluentEmail.MailKit` resolve MailKit 2.10.1 (2020) sem pin manual. Mailpit como servidor SMTP em dev e no vídeo.
- **Webhook autenticado por API key** no header `X-Api-Key` (segredo em Secret do K8s), idempotente.
- **Listagem**: a OS `Cancelada` continua aparecendo (até o carro ser retirado, quando vira `Entregue`), por último na ordenação. Só `Finalizada` e `Entregue` saem.
- **Infraestrutura: Azure** (Azure for Students, US$100). VM B2s (2 vCPU, 4 GB, x86) com k3s + **Azure SQL Database free offer** fora do cluster (mesmo motor do SQL Server, sem migração). Histórico: AWS Learner Lab testado e descartado (2026-10-14); VPS (Hetzner/DigitalOcean) e SQL Server como StatefulSet no cluster foram cogitados e substituídos por esta opção. AKS descartado: load balancer padrão + nós pagos consumiriam o crédito sem ganho para a entrega.

## Ticket 32: Listagem de OS ordenada por prioridade de status

**Descrição:** alterar o `GET v1/service-orders` existente (ticket 30) para seguir a regra do PDF, em vez de criar um segundo endpoint de listagem.

**Critérios de Aceite:**
- Ordem: `InProgress` > `AwaitingApproval` > `Diagnosing` > `Received` > `Cancelled`
- Dentro do mesmo status, mais antigas primeiro (`OpenedAt` ascendente)
- `Completed` e `Delivered` não aparecem (exclusão lógica — nada é apagado)

**Feito em 2026-10-25:** regra no use case (array `ListingPriority`: define os status listáveis e a prioridade), repositório com filtro genérico `GetByStatusesAsync` no banco, ordenação em memória. Alternativa descartada: filtro + ordenação na query (regra de negócio na Infrastructure). Detalhes em `CONTEXT.md` §4; pontos a revalidar antes da entrega em §9 item 18.

## Ticket 33: Consulta de status da OS

**Descrição:** endpoint enxuto `GET v1/service-orders/{id}/status` devolvendo id, status atual e data da última mudança — o `GET /{id}` já existe, mas devolve a OS inteira.

**Feito em 2026-10-25:** mesma policy do `GET /{id}` (autenticado sem papel específico), `404` se não existir. "Data da última mudança" = `ChangedAt` mais recente do histórico, ou `OpenedAt` se vazio (`UpdatedAt` descartado — mede qualquer alteração, não a mudança de status). Detalhes em `CONTEXT.md` §4/§6.

## Ticket 34: Health checks

**Descrição:** `/health/live` e `/health/ready` para as probes do Kubernetes.

**Cuidado (Azure SQL free offer):** as probes periódicas **não** podem consultar o banco — elas rodam a cada poucos segundos, impediriam o auto-pause e esgotariam a cota mensal (100.000 vCore-segundos) em poucos dias, pausando o banco até o mês seguinte.

**Feito em 2026-10-25 (opção B, decisão do usuário):** `/health/live` sem checks (liveness + readiness) e `/health/ready` com check de conexão ao banco, usado **só pela startupProbe** — um pod novo com banco inalcançável não recebe tráfego e o rollout trava, sem consumir a cota. Pacote oficial `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.13. Detalhes em `CONTEXT.md` §6.

## Ticket 35: Testes funcionais

**Descrição:** dar conteúdo ao projeto `Fiap.Workshop.FunctionalTests` (vazio desde a Fase 1, ver `CONTEXT.md` §9 item 5) com `WebApplicationFactory`, cobrindo a composição de DI/rotas da Api.

**Por que tão cedo:** o webhook (ticket 36) traz um esquema de autenticação novo, e bugs de composição (validator não registrado, `.WithName` duplicado) só aparecem com a Api rodando de ponta a ponta. Cobrir 32-34 aqui e seguir escrevendo funcionais junto com cada ticket seguinte.

## Ticket 36: Webhook de aprovação/recusa de orçamento

**Descrição:** endpoint para um sistema externo notificar a decisão do cliente sobre o orçamento. Reaproveita `ApproveServiceOrderUseCase`/`RejectServiceOrderUseCase`; os endpoints atuais do atendente (`/approval`, `/rejection`) continuam existindo.

**A decidir antes de implementar:**
- Formato da rota/corpo (proposta: `POST v1/webhooks/budget-decisions` com `serviceOrderId` + `decision`)
- **Quem é o `ChangedBy`**: `ServiceOrderStatusHistories.ChangedBy` tem FK para `Users`, e uma chamada externa não tem usuário. Opções: usuário "de sistema" semeado no `init.sql` (mesmo padrão do bootstrap de Admin) ou relaxar a FK.
- Política de idempotência: repetir a mesma decisão para uma OS que já saiu de `AwaitingApproval` por essa mesma decisão = sucesso sem efeito; decisão contraditória = erro (`400`, ver `docs/standards/ddd-guidelines.md` §8)

## Ticket 37: Notificação de status da OS por e-mail

**Descrição:** enviar e-mail ao cliente a cada mudança de status da OS. Primeiro `IDomainEventHandler` real do projeto (mecânica em `CONTEXT.md` §11).

**Tarefas:**
- Evento de domínio disparado nos métodos de transição de `ServiceOrder` (não no construtor — o construtor também roda na reidratação, ver §11)
- `IEmailService` (Application) + `SmtpEmailService` com MailKit (Infrastructure)
- Mailpit no `docker-compose.yml`; host/porta/remetente via configuração (ConfigMap no K8s), senha via Secret

**A decidir:** `ServiceOrder` só conhece o `CustomerId` — o handler precisa carregar o `Customer` para obter o e-mail (diverge da preferência por "evento gordo" registrada em §11). Avaliar também se a abertura da OS (`Received`) notifica.

**Feito em 2026-10-26**, em 4 etapas (domínio → Application → Infrastructure → validação HTTP):
- `ServiceOrderStatusChangedEvent` disparado pelo novo `ServiceOrder.ChangeStatus`, que concentra o bloco antes repetido nas 6 transições
- Abertura da OS **não** gera e-mail (decisão do usuário: o PDF pede "atualização"; o construtor também reidrata do banco)
- `IEmailService.SendAsync(EmailMessage)` genérica; o texto (português, nomes de status do PDF) mora no handler `SendStatusChangedEmailHandler`, que busca o cliente pelo `CustomerId`
- `SmtpEmailService` com MailKit 4.18.1; Mailpit v1.31.6 no docker-compose; `TZ=America/Sao_Paulo` no container (o horário deve ser o de Brasília — o container rodava em UTC)
- Falha de SMTP não desfaz a mudança de status: só `LogError`
- Detalhes e alternativas descartadas em `CONTEXT.md` §3/§4/§5/§7/§11

## Ticket 38: Refatoração — dívida técnica e cobertura

**Descrição:** requisito "refatorar aplicando Clean Code / Clean Architecture + testes dos fluxos críticos". Itens já conhecidos (`CONTEXT.md` §9):
- Padronizar uma única biblioteca de mock (hoje Moq e NSubstitute)
- `GetUsersUseCase` devolvendo dois formatos diferentes (item 8)
- JSON malformado virando `500` em vez de `400` (item 16)
- Medir a cobertura dos domínios críticos (nunca foi medida contra a meta)
- Revisão de dependências entre camadas, a fazer antes de listar outros itens
- Extrair a validação de status de origem repetida nas 6 transições de `ServiceOrder` (o bloco de mudança de status já foi extraído para `ChangeStatus` no ticket 37)

## Ticket 39: Manifestos Kubernetes (`/k8s`)

**Descrição:** Deployment e Service da Api, ConfigMap, Secret, HPA (CPU/memória) e Mailpit. Validados num cluster local (kind/k3d) antes de existir qualquer recurso na Azure.

**Do ticket 37 (e-mail):**
- `TZ: America/Sao_Paulo` no Deployment da Api (o horário do sistema e do e-mail é o de Brasília)
- Mailpit (`axllent/mailpit:v1.31.6`) como Deployment + Service **interno** (`ClusterIP`); a Api envia para `mailpit:1025`
- `Email__Host/Port/SecureSocketOptions/FromAddress/FromName` no ConfigMap; `Email__Username/Password` no Secret (vazios com o Mailpit, mas a estrutura fica pronta para um provedor real)
- Não expor a interface do Mailpit na internet: no vídeo, `kubectl port-forward svc/mailpit 8025:8025` e abrir `localhost:8025`

**Probes (do ticket 34):** liveness e readiness em `/health/live`; startupProbe em `/health/ready`. Na startupProbe, `timeoutSeconds` precisa passar do padrão de 1 s (com o banco fora, o check leva ~15 s até o timeout de conexão) e `failureThreshold` × `periodSeconds` precisa cobrir ~1 min de retomada do Azure SQL após auto-pause.

## Ticket 40: Terraform (`/infra`) — Azure

**Descrição:** provisionar resource group, rede/NSG, VM B2s com k3s instalado via cloud-init, Azure SQL Server + Database (free offer). Documentar no README o que é criado e como aplicar/destruir.

**Cuidados:** `db/init.sql` precisa rodar sem `CREATE DATABASE`/`USE` (o banco passa a ser criado pelo Terraform); conexão com `EnableRetryOnFailure` para a primeira conexão após o auto-pause.

## Ticket 41: CI/CD com deploy no cluster

**Descrição:** pipeline (GitHub Actions) com build, testes, build/push da imagem, deploy do schema do banco e `kubectl apply` dos manifestos.

**A decidir:** registry da imagem (proposta: GHCR) e como o CD alcança o cluster (runner self-hosted na VM, sem expor a porta 6443, vs. runner hospedado com kubeconfig em secret). Login na Azure via OIDC (o diretório da conta é pessoal, então permite registrar o app).

## Ticket 42: Entrega

- README: descrição da solução e objetivos da fase, desenho da arquitetura (componentes, infra provisionada, fluxo de deploy), instruções de execução local / deploy em K8s / Terraform
- Collection completa das APIs (Postman ou Swagger)
- Teste de carga para demonstrar o HPA escalando
- Vídeo de até 15 min (deploy, CI/CD, consumo das APIs, escalabilidade), no YouTube/Vimeo
- PDF de entrega no portal, com o repositório compartilhado com o usuário GitHub `soat-architecture`
- Antes de gravar: revalidar as decisões da listagem de OS (ticket 32, `CONTEXT.md` §9 item 18)
- `terraform destroy` depois da gravação
