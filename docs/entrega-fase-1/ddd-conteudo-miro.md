# Conteúdo de referência para o board de DDD (Miro)

Extraído do código e do `CONTEXT.md` reais do projeto — não é conteúdo inventado pra preencher o board, é o desenho que já existe implementado. Organizado pelos 4 artefatos pedidos pelos professores.

---

## 1. Event Storming

Convenção de cores clássica: 🟧 laranja = evento de domínio · 🟦 azul = comando · 🟨 amarelo = ator · 🟪 roxo = policy (reação automática) · 🩷 rosa = agregado.

### 1.1 Criação e acompanhamento da Ordem de Serviço

| Ator | Comando | Evento | Agregado |
|---|---|---|---|
| Attendant | Abrir Ordem de Serviço | **OS Recebida** | ServiceOrder |
| Mechanic | Iniciar diagnóstico | **Diagnóstico Iniciado** (OS → Diagnosing) | ServiceOrder |
| Cliente (público, sem login) | Consultar status (CPF/CNPJ + placa) | — (é leitura, não gera evento; sempre `200`, nunca revela existência de cadastro) | ServiceOrder (read) |

### 1.2 Elaboração, aprovação ou reprovação do orçamento

| Ator | Comando | Evento | Agregado |
|---|---|---|---|
| Mechanic | Montar orçamento (serviços + peças) | **Orçamento Elaborado** (OS → AwaitingApproval) | ServiceOrder |
| *(policy)* | — | **Estoque Reservado** (reação automática ao orçamento) | InventoryItem |
| Attendant | Aprovar orçamento | **Orçamento Aprovado** (OS → InProgress) | ServiceOrder |
| *(policy)* | — | **Estoque Debitado** (reação automática à aprovação) | InventoryItem |
| Attendant | Reprovar orçamento | **Orçamento Reprovado** (OS → Cancelled) | ServiceOrder |
| *(policy)* | — | **Reserva de Estoque Liberada** (reação automática à reprovação) | InventoryItem |

### 1.3 Execução e finalização do serviço

| Ator | Comando | Evento | Agregado |
|---|---|---|---|
| Mechanic | Concluir OS (informa duração real de cada serviço) | **Serviço Concluído** (OS → Completed) | ServiceOrder |
| *(policy)* | — | **Tempo Médio do Catálogo Atualizado** (reação automática — média incremental de `EstimatedDuration`) | Service |
| Attendant | Entregar veículo | **Veículo Entregue** (OS → Delivered) | ServiceOrder |

### 1.4 Gestão de peças e insumos

| Ator | Comando | Evento | Agregado |
|---|---|---|---|
| Admin | Cadastrar peça/insumo | **Peça Cadastrada** | InventoryItem |
| Admin | Atualizar peça/insumo | **Peça Atualizada** | InventoryItem |
| Admin | Remover peça/insumo | **Peça Removida** (bloqueado com erro se já usada em alguma OS) | InventoryItem |
| *(consumido pelo fluxo 1.2)* | — | Estoque Reservado / Debitado / Liberado | InventoryItem |

> Nota pro board: só existe **um** evento de domínio implementado como classe de código (`UserCreatedEvent`, disparado pelo agregado `User`). Todos os outros eventos acima são conceituais — no Event Storming representam "algo relevante que aconteceu no negócio", mas no código são mutações diretas de estado (`ServiceOrder.Approve()`, `InventoryItem.Reserve()` etc.), não eventos publicados. Vale colocar essa nota no board pra não parecer inconsistência entre diagrama e código.

---

## 2. Context Map / Bounded Contexts

```
┌─────────────────────┐        valida existência        ┌──────────────────────────┐
│  Identidade &        │ ───────────────────────────────▶│  Cadastro                 │
│  Acesso               │   (claims JWT: quem é o ator)   │  (Customer, Vehicle)      │
│  (User, roles, JWT)   │                                  └──────────────────────────┘
└──────────┬───────────┘
           │ autentica/autoriza todo comando
           ▼
┌───────────────────────────────────────────┐   snapshot (anti-corrupção)   ┌───────────────────────────┐
│  Atendimento / Ordem de Serviço             │──────────────────────────────▶│  Catálogo & Estoque        │
│  (ServiceOrder + Parts/Services/History)    │   reserva → debita → libera   │  (Service, InventoryItem)  │
│  — contexto núcleo, orquestra os outros     │◀──────────────────────────────│                            │
│  três só por referência de Id, nunca        │                               └───────────────────────────┘
│  compondo os objetos de verdade             │
└──────────────────────────────────────────────┘
```

**Relações a explicar no board:**
- **Atendimento → Cadastro**: relação *Customer/Supplier* simples — só lê para validar que `CustomerId`/`VehicleId` existem antes de abrir a OS.
- **Atendimento → Catálogo & Estoque**: no orçamento, `ServiceOrder` não guarda uma referência viva a `Service`/`InventoryItem` — copia (**"fotografa"**) `Name`/`Description`/`UnitPrice` no momento do `AddBudget`. É um padrão de **anti-corrupção por snapshot**: se o preço do catálogo mudar depois, o orçamento já fechado não muda junto. Vale destacar isso no diagrama como uma decisão deliberada, não um esquecimento.
- **Identidade & Acesso** é transversal: todo comando nos outros três contextos passa por ele (quem pode `Approve`, quem pode `StartDiagnosis` etc. — policies por role).
- É um **monolito modular**: os quatro contextos vivem no mesmo processo/deploy hoje (não são serviços separados) — o Context Map descreve fronteiras de responsabilidade dentro do monolito, não uma topologia de microsserviços.

---

## 3. Diagrama do modelo de domínio

### Agregados e Aggregate Roots

| Aggregate Root | Entidades filhas | Regras de domínio no construtor/métodos |
|---|---|---|
| **User** | — | Nenhuma invariante no construtor (decisão: validação de formato fica na Api). `UpdateProfile`/`ChangePassword` mutam sem reconstruir o agregado. |
| **Customer** | — | Sem invariante no construtor. `Document` (CPF/CNPJ) é imutável após criado — decisão de negócio (documento não muda, como identidade). |
| **Vehicle** | — | `ModelYear` não pode ser anterior a `ManufactureYear` (`DomainException`). `LicensePlate` imutável. |
| **Service** (catálogo) | — | `BasePrice`/`EstimatedDuration` não podem ser negativos. `RecordExecution` recalcula `EstimatedDuration` como média incremental (`ExecutionCount` conta amostras). `IsActive=false` bloqueia entrar em orçamentos novos. |
| **InventoryItem** (catálogo + estoque) | — | `QuantityOnHand`/`ReservedQuantity`/`MinimumStock`/`UnitPrice` não-negativos; **invariante central**: `ReservedQuantity ≤ QuantityOnHand`. Ciclo de vida da reserva: `Reserve` → `CommitReservation` (debita de verdade) *ou* `ReleaseReservation` (desfaz sem debitar). |
| **ServiceOrder** | `ServiceOrderPart`, `ServiceOrderService`, `ServiceOrderStatusHistory` | Máquina de estados: `Received → Diagnosing → AwaitingApproval → (InProgress → Completed | Cancelled) → Delivered`. Cada transição valida o status anterior (`InvalidStatusTransition` se violado) e registra uma linha em `StatusHistory`. `Subtotal`/`Total` calculados a partir dos itens do orçamento. |

### Value Objects

**Não existem Value Objects na implementação atual.** Existe uma classe-base `ValueObject` (igualdade estrutural via `GetEqualityComponents()`) preparada no `Domain.Abstractions`, mas nenhuma classe do domínio a utiliza hoje — `Document`, `Email`, `LicensePlate`, `Money`/`UnitPrice` são todos primitivos (`string`/`decimal`) com validação de formato feita na camada Api (FluentValidation), não no Domain. Decisão de escopo consciente do time, documentada no `CONTEXT.md`.

### Eventos de domínio

- `UserCreatedEvent(UserId)` — único evento realmente implementado e despachado (`Infrastructure`, na criação/atualização de `User`).
- Os demais "eventos" do Event Storming (seção 1) são conceituais: representam transições de estado relevantes pro negócio, implementadas como mutação direta de agregado, não como eventos publicados.

### Relacionamentos relevantes

- `Customer` 1 — N `Vehicle` (por `CustomerId`, referência, não composição).
- `ServiceOrder` → `Customer` e `ServiceOrder` → `Vehicle`: referência por Id, validada na criação.
- `ServiceOrder` 1 — N `ServiceOrderPart` / `ServiceOrderService` / `ServiceOrderStatusHistory`: composição real (filhos só existem dentro do agregado pai).
- `ServiceOrderPart` → `InventoryItem` e `ServiceOrderService` → `Service`: referência por Id **+ snapshot** dos dados no momento do orçamento (ver Context Map, padrão anti-corrupção).

---

## 4. Linguagem Ubíqua

| Termo | Significado no domínio |
|---|---|
| **OS (Ordem de Serviço)** | Unidade de trabalho que representa o atendimento de um veículo, do recebimento à entrega. |
| **Orçamento** | Conjunto de serviços e peças propostos para uma OS, com subtotal/total calculados; existe só a partir do momento em que é "elaborado" (`AddBudget`), a OS não nasce com ele. |
| **Diagnóstico** | Avaliação técnica feita pelo Mechanic que identifica o problema real do veículo, antes de montar o orçamento. |
| **Reserva de estoque** | Promessa de uso de uma peça (`ReservedQuantity` sobe), feita ao montar o orçamento — ainda não é uma baixa real. |
| **Baixa de estoque / Débito** | Consumo real da peça (`QuantityOnHand` e `ReservedQuantity` descem juntos), só acontece quando o orçamento é aprovado. |
| **Liberação de reserva** | Desfazer a promessa de uso (`ReservedQuantity` desce, `QuantityOnHand` intocado) quando o orçamento é reprovado. |
| **Fotografia (snapshot)** | Cópia de nome/descrição/preço do catálogo (`Service`/`InventoryItem`) para dentro do item de orçamento (`ServiceOrderService`/`ServiceOrderPart`) no momento em que ele é criado — o orçamento não muda se o catálogo mudar depois. |
| **Tempo médio de execução** | Média incremental (`EstimatedDuration`) recalculada no catálogo de `Service` toda vez que uma OS usando aquele serviço é concluída; `ExecutionCount` indica quantas amostras compõem a média. |
| **Attendant / Atendente** | Papel de negócio responsável por abrir a OS, aprovar/reprovar orçamento e entregar o veículo. |
| **Mechanic / Mecânico** | Papel de negócio responsável por diagnóstico, montagem do orçamento técnico e conclusão do serviço. |
| **Admin** | Papel de negócio responsável pela gestão de catálogo (serviços, peças) e usuários. |

---

*Este documento é insumo para montar o board — não substitui o board em si. O board publicado no Miro é o entregável oficial pedido pelo desafio.*
