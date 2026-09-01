# Roteiro — Vídeo de apresentação (Fase 1, até 15 min)

Guia pra gravar sozinho. Cada bloco tem: tempo alvo, o que falar (pontos, não texto decorado) e o que mostrar na tela. Ordem pensada pra contar uma história única (abrir uma OS do zero e seguir até a entrega) em vez de uma lista de features soltas — é mais fácil de narrar e economiza tempo.

**Antes de gravar, deixe aberto:**
- Terminal na raiz do repo.
- Navegador com abas: Swagger (`http://localhost:8080/swagger`), SonarCloud (o dashboard que você já tem), GitHub Actions do repositório, o board do Miro.
- Um cliente HTTP no Swagger mesmo (não precisa de Postman) — os exemplos abaixo são os bodies exatos dos endpoints.
- Rode `docker compose up -d --build` **antes** de começar a gravar de verdade (o build demora; corte esse tempo morto do vídeo, ou acelere em edição).

Tempo total planejado: **~14h30 min**, com folga sob o limite de 15.

---

## 1. Abertura (0:00 – 0:40)

Fale rápido e direto:
- Nome, curso (Pós FIAP, Arquitetura de Software — SOAT), Tech Challenge Fase 1.
- Uma frase sobre o problema: oficina mecânica sem controle digital de OS, peças e histórico de cliente.
- Uma frase sobre a proposta: MVP de back-end em .NET 10, Clean Architecture + DDD.

---

## 2. Arquitetura e stack (0:40 – 2:10)

Mostre a estrutura de pastas do repo (VS Code/Explorer) enquanto fala:
- 4 camadas: `Domain` (zero dependência externa) → `Application` (use cases, portas) → `Infrastructure` (EF Core, JWT, hashing) → `Api` (Minimal APIs, validação).
- Regra de dependência: cada camada só conhece a de dentro.
- DDD: agregados de negócio reais da oficina — `Customer`, `Vehicle`, `Service`, `InventoryItem`, `ServiceOrder` — com invariantes no próprio domínio (ex.: `ModelYear` não pode ser anterior a `ManufactureYear`; `ReservedQuantity` nunca passa `QuantityOnHand`).
- Stack: .NET 10, Minimal APIs, EF Core + SQL Server, JWT, FluentValidation, Serilog, xUnit.
- Mencione rapidamente: "a documentação DDD completa — Event Storming e linguagem ubíqua — está no Miro, mostro no final."

---

## 3. Subindo o ambiente (2:10 – 2:50)

No terminal:
```bash
docker compose up -d
docker compose ps
```
Fale enquanto sobe (ou já deixe rodando e só mostre o `ps`):
- Um `Dockerfile` multi-stage builda a API.
- `docker-compose.yml` orquestra API + SQL Server + um serviço de init que roda o schema (`db/init.sql`) + um serviço de seed que popula dado de demonstração (`db/seed-demo.sql`) — clientes, veículos, catálogo e OS em cada status da máquina de estados, pra já ter algo pra mostrar.

---

## 4. Login e tour do Swagger (2:50 – 3:40)

No Swagger, `POST /api/v1/auth/login`:
```json
{
  "correlationId": "11111111-0000-0000-0000-000000000001",
  "email": "admin@admin.com",
  "password": "Admin@123"
}
```
- Copie o token, clique em "Authorize" no Swagger, cole como Bearer.
- Fale: "JWT com roles de negócio — Admin, Attendant, Mechanic — cada endpoint exige a policy certa, não um role genérico."

---

## 5. CRUD ao vivo: cliente e veículo (3:40 – 4:40)

`POST /api/v1/customers`:
```json
{
  "correlationId": "11111111-0000-0000-0000-000000000002",
  "name": "Pedro Rocha",
  "document": "52998224725",
  "email": "pedro.rocha@example.com",
  "phone": "11976543210"
}
```
Fale: "documento validado por dígito verificador real de CPF/CNPJ, não só formato." Guarde o `id` retornado.

`POST /api/v1/vehicles` (use o `customerId` acima):
```json
{
  "correlationId": "11111111-0000-0000-0000-000000000003",
  "customerId": "<id do Pedro>",
  "licensePlate": "RES1A23",
  "brand": "Chevrolet",
  "model": "Onix",
  "manufactureYear": 2023,
  "modelYear": 2023,
  "color": "Branco"
}
```
Fale: "placa validada nos dois padrões, Mercosul e antigo."

---

## 6. Fluxo completo de uma Ordem de Serviço (4:40 – 9:30)

Esse é o bloco principal — narre cada transição de status explicando a regra de negócio, não só o clique.

**6.1 Abrir a OS** — `POST /api/v1/service-orders` (login como Admin ou Attendant):
```json
{
  "correlationId": "11111111-0000-0000-0000-000000000004",
  "customerId": "<id do Pedro>",
  "vehicleId": "<id do Onix>",
  "problemDescription": "Barulho na suspensão dianteira",
  "odometerReading": 15000
}
```
Fale: "a OS nasce em `Received`, sem orçamento — decisão de design: orçamento só existe depois do diagnóstico." Guarde o `id` da OS.

**6.2 Diagnóstico** — troque pro usuário Mechanic (`mecanico@oficina.com` / `Demo@123`, login de novo), depois `POST /api/v1/service-orders/{id}/diagnosis`:
```json
{
  "correlationId": "11111111-0000-0000-0000-000000000005",
  "diagnoseDescription": "Amortecedores dianteiros desgastados, recomendada substituição"
}
```
Fale: "só o Mechanic pode diagnosticar; a OS vai de `Received` pra `Diagnosing`."

**6.3 Orçamento** — `POST /api/v1/service-orders/{id}/budget`, usando um serviço e uma peça do catálogo seedado (ex.: `SVC-004` Troca de pastilhas / `PC-003`, ou consulte `GET /api/v1/services` e `GET /api/v1/inventory-items` na tela pra pegar ids reais):
```json
{
  "correlationId": "11111111-0000-0000-0000-000000000006",
  "services": [{ "serviceId": "<id de um serviço>", "quantity": 1 }],
  "parts": [{ "inventoryItemId": "<id de uma peça>", "quantity": 2 }]
}
```
Fale enquanto mostra a resposta: "o subtotal é calculado automaticamente a partir do preço do catálogo; o estoque da peça já foi **reservado** (não baixado ainda); a OS vai pra `AwaitingApproval`." Mostre `GET /api/v1/inventory-items/{id}` antes/depois pra provar a reserva.

**6.4 Aprovação** — volte pro Attendant, `POST /api/v1/service-orders/{id}/approval`:
```json
{ "correlationId": "11111111-0000-0000-0000-000000000007" }
```
Fale: "aprovação **efetiva** a baixa de estoque (reserva vira débito real) e move a OS pra `InProgress`." Mostre o estoque de novo.

*(Opcional, se sobrar tempo: mencione que existe o caminho inverso — `/rejection` — que libera a reserva sem debitar, indo pra `Cancelled`.)*

**6.5 Conclusão** — volte pro Mechanic, `POST /api/v1/service-orders/{id}/completion`. Pegue o `serviceOrderServiceId` do `GET /api/v1/service-orders/{id}`:
```json
{
  "correlationId": "11111111-0000-0000-0000-000000000008",
  "serviceDurations": [{ "serviceOrderServiceId": "<id>", "actualDuration": 55 }]
}
```
Fale: "registra a duração real; o `EstimatedDuration` do serviço no catálogo é recalculado como uma média incremental — é como o desafio pede o monitoramento de tempo médio de execução. OS vai pra `Completed`." Mostre `GET /api/v1/services/{id}` antes/depois, o `ExecutionCount` mudou.

**6.6 Entrega** — Attendant, `POST /api/v1/service-orders/{id}/delivery`:
```json
{ "correlationId": "11111111-0000-0000-0000-000000000009" }
```
Fale: "fecha o ciclo, OS vai pra `Delivered`, o veículo saiu da oficina."

---

## 7. Consulta pública do cliente (9:30 – 10:10)

Sem token nenhum, `GET /api/v1/service-orders/lookup?document=52998224725&licensePlate=RES1A23`.
Fale: "o cliente acompanha o status sem autenticação — só com o próprio documento e placa; se não bater, devolve lista vazia sempre com `200`, nunca revela se o cadastro existe."

---

## 8. Gestão administrativa: listagem e tempo médio (10:10 – 11:10)

`GET /api/v1/service-orders` autenticado — mostre que aparecem as OS seedadas cobrindo **todos** os status (Recebida, Diagnóstico, Aguardando aprovação, Em execução, Finalizada, Cancelada, Entregue) além da que você acabou de criar.
Fale: "listagem e detalhamento administrativos completos — é o requisito de gestão do desafio." Abra um `GET /api/v1/service-orders/{id}` de uma das OS seedadas pra mostrar o histórico de status (`StatusHistory`) já populado.

---

## 9. Qualidade: testes e cobertura (11:10 – 12:10)

No terminal:
```bash
dotnet test tests/Fiap.Workshop.UnitTests
```
Fale enquanto roda: "209 testes unitários de Domain e Application, mais um projeto de integração que sobe um SQL Server real em container."
Troque pra aba do SonarCloud: mostre Quality Gate "Passed", 97,3% de cobertura em Domain/Application (meta do desafio era 80%), e o pipeline de CI no GitHub Actions rodando a cada push.

---

## 10. Segurança e relatório de vulnerabilidades (12:10 – 13:10)

Fale, mostrando o Swagger/policies:
- JWT com três policies de negócio (`AdminOnly`, `AttendantOnly`, `MechanicOnly`).
- BCrypt pra senha, nunca texto puro.
- Validação de CPF/CNPJ e placa na borda da API.

Abra `docs/entrega-fase-1/entrega-fase-1.pdf` (ou o CodeQL do GitHub) e resuma:
- "Rodamos CodeQL (SAST) e SonarQube continuamente no CI. O CodeQL identificou 15 alertas de severidade média — log forging e exposição de e-mail em log — que já corrigimos com uma sanitização/mascaramento dedicado antes de logar."
- Mostre rapidamente o arquivo `LoggingExtensions.cs` e um dos use cases corrigidos.

---

## 11. Documentação DDD no Miro (13:10 – 14:00)

Troque pra aba do Miro. Passeie rápido por:
- Event Storming dos dois fluxos pedidos: criação/acompanhamento de OS, e gestão de peças/insumos.
- Diagramas de DDD (contexto/agregados).
- Onde está a Linguagem Ubíqua aplicada.

Fale uma frase ligando o board ao código: "os nomes que vocês veem aqui — `ServiceOrder`, `AwaitingApproval`, `Reserve`/`CommitReservation` — são literalmente os nomes das classes e métodos no código, não uma tradução feita depois."

---

## 12. Encerramento (14:00 – 14:30)

- Resuma em uma frase: "MVP completo, todos os fluxos obrigatórios do desafio implementados, testados e documentados."
- Agradeça e feche.

---

## Checklist rápido do que **precisa** aparecer (cobertura do PDF)

- [ ] Identificação do cliente por CPF/CNPJ — passo 5
- [ ] Cadastro de veículo — passo 5
- [ ] Inclusão de serviços/peças no orçamento — passo 6.3
- [ ] Orçamento automático — passo 6.3
- [ ] Todos os status da OS — passo 6 (ao vivo) + passo 8 (seed cobrindo todos)
- [ ] Consulta pública via API — passo 7
- [ ] CRUD de clientes/veículos/serviços/peças — passo 5 (ao vivo) + mencionar que os outros seguem o mesmo padrão
- [ ] Listagem/detalhamento admin de OS — passo 8
- [ ] Monitoramento de tempo médio de execução — passo 6.5
- [ ] JWT + validação de dados sensíveis — passo 10
- [ ] Testes automatizados — passo 9
- [ ] Docker/docker-compose — passo 3
- [ ] Relatório de vulnerabilidades — passo 10
- [ ] Documentação DDD — passo 11
