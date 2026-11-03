# Roteiro do vídeo — Fase 2 (até 15 min)

O PDF pede que o vídeo mostre: **deploy da aplicação, execução do CI/CD, consumo das APIs e escalabilidade automática**. O roteiro abaixo cobre os quatro em ~14 min, com folga. Tempos são alvo; o pipeline leva ~10 min, então **pause a gravação** enquanto ele roda (indicado no bloco 4).

---

## Antes de gravar (não entra no vídeo, ~40 min)

### 1. Ligar o ambiente

```powershell
az vm start -g fiap-workshop-rg -n fiap-workshop-vm
curl.exe http://20.226.32.136/health/ready        # acorda o banco; repita até "Healthy" (~1 min na 1ª vez)
```

Conferir em *GitHub → Settings → Actions → Runners* que o `fiap-workshop-vm` está **Idle** (online).

### 2. Terminais (Git Bash)

| Terminal | Comando | Fica aberto |
|---|---|---|
| T0 — túnel | `ssh -i ~/.ssh/fiap-workshop-azure -N -L 6443:localhost:6443 azureuser@20.226.32.136` | o vídeo inteiro |
| T1 — HPA | `export KUBECONFIG=~/.kube/azure-k3s.yaml; kubectl -n fiap-workshop get hpa api -w` | bloco 7 |
| T2 — pods | `export KUBECONFIG=~/.kube/azure-k3s.yaml; kubectl -n fiap-workshop get pods -l app=api -w` | bloco 7 |
| T3 — Mailpit | `export KUBECONFIG=~/.kube/azure-k3s.yaml; kubectl -n fiap-workshop port-forward svc/mailpit 8025:8025` | bloco 6 |
| T4 — comandos | `export KUBECONFIG=~/.kube/azure-k3s.yaml` | livre |

O T3 cai quando o pod do Mailpit é recriado (o deploy do bloco 4 recria): reabra depois do deploy.

### 3. Ensaio do teste de carga (decide o bloco 7)

No PowerShell, na raiz do repositório:

```powershell
docker run --rm -t -p 5665:5665 -v "${PWD}/tests/load:/scripts" `
  -e BASE_URL=http://20.226.32.136 -e K6_WEB_DASHBOARD=true -e K6_WEB_DASHBOARD_HOST=0.0.0.0 `
  grafana/k6 run /scripts/hpa-load.js
```

- **Escalou para 3** (T1/T2): ok, siga o roteiro. Espere voltar para **1 réplica** (~5 min depois do fim) antes de gravar.
- **Não escalou** (CPU da api baixa, latência alta): o gargalo é o Azure SQL gratuito, e a api fica esperando o banco em vez de gastar CPU. Plano B: o mesmo comando com `/scripts/hpa-login-load.js` (10 usuários fazendo login; o BCrypt gasta CPU na api e quase não usa o banco). No bloco 7, troque a fala "30 usuários consultando a listagem" por "10 usuários fazendo login".

### 4. Abas abertas no navegador

1. Repositório no GitHub (README, na seção "Arquitetura de implantação").
2. PR #90 (`develop → release`), **ainda sem merge**.
3. *Actions* do repositório.
4. Portal da Azure, grupo de recursos `fiap-workshop-rg`.
5. Swagger: `http://20.226.32.136/swagger`.
6. Mailpit: `http://localhost:8025` (depois do T3).

### 5. Bloco de notas (não mostrar na tela)

- Chave do webhook: `terraform output -raw webhook_api_key` (na pasta `infra`). **Não** exibir no vídeo; colar direto no Swagger.
- Os corpos JSON do bloco 6 (abaixo).
- **Nada de segredo na tela**: `terraform.tfvars`, `terraform output` de valores sensíveis, valores dos GitHub Secrets.

### 6. Conferências rápidas

- Revalidar a listagem de OS (CONTEXT.md §9 item 18): `GET /service-orders` no Swagger com o token do Admin — ordem `InProgress` > `AwaitingApproval` > `Diagnosing` > `Received`, mais antigas primeiro, sem `Completed`/`Delivered`.
- Nenhum pipeline rodando em *Actions*.
- Checks do PR #90 verdes. O PR é aberto pelo `github-actions[bot]`, e o GitHub segura os workflows disparados por ele até alguém aprovar: se o CodeQL aparecer como *action required*, clique em **Approve and run** e espere terminar antes de gravar.

---

## Roteiro

### Bloco 1 — Abertura (0:00–0:45)

- Apresentação: projeto da oficina mecânica, Fase 2 do Tech Challenge.
- O problema da fase: crescimento da demanda → escalabilidade, provisionamento e deploy automatizados, qualidade de código.
- O que será mostrado: arquitetura, infraestrutura como código, pipeline com deploy, APIs novas e escala automática.

### Bloco 2 — Arquitetura (0:45–2:30)

Na aba do README, seção **Arquitetura de implantação** (diagrama):

- **Aplicação**: .NET 10, Clean Architecture (Domain, Application, Infrastructure, Api), DDD.
- **Infraestrutura**: VM na Azure com **k3s** (Kubernetes leve) + **Azure SQL** gratuito fora do cluster; Mailpit como servidor de e-mail de teste.
- **Fluxo de deploy**: PR → `develop` → `release` → GitHub Actions → imagem no GHCR → runner instalado na própria VM aplica no cluster.
- **Por que essas escolhas** (1 frase cada): k3s numa VM em vez de AKS (custo no crédito de estudante); runner na VM (não expõe a API do Kubernetes na internet, nem guarda credenciais da Azure no GitHub).
- Mostrar rapidamente a estrutura do repositório: `src/`, `tests/`, `k8s/`, `infra/`, `.github/workflows/`.

### Bloco 3 — Infraestrutura como código (2:30–4:00)

- Abrir `infra/main.tf` e `infra/database.tf`: grupo de recursos, rede, firewall (HTTP aberto, SSH só do IP do administrador), VM com k3s instalado no primeiro boot (`cloud-init.yaml`), Azure SQL na oferta gratuita.
- Portal da Azure: os recursos criados no grupo `fiap-workshop-rg`.
- Opcional (~30 s): `terraform plan` na pasta `infra` → **"No changes"**: a infraestrutura no ar é exatamente a descrita no código.
- Mencionar: segredos (senha do banco, chave do JWT, chave do webhook) gerados pelo Terraform e passados ao pipeline como GitHub Secrets.

### Bloco 4 — CI/CD e deploy (4:00–6:30, com pausa)

1. PR #90: mostrar o que entra (refatoração do ticket 38, documentação da entrega e correção do CodeQL). **Fazer o merge.**
2. *Actions* → **CI/CD - Release**: mostrar os jobs — Build, Unit Tests, Integration Tests, SonarQube, Build and Push Docker Image, Deploy.
3. **Pausar a gravação** até o job de deploy começar (~6–8 min).
4. Retomar no job **Deploy to Kubernetes (Azure)** — está rodando no runner `fiap-workshop-vm`. Abrir os passos:
   - *Wait for database* (o banco acorda da pausa automática);
   - *Deploy database schema and demo data* — **deploy do banco**;
   - *Apply manifests* — `kubectl apply` e rollout — **aplicação dos manifestos**;
   - *Smoke test* — `/health/ready` e login.
5. No T4: `kubectl -n fiap-workshop get deploy api -o jsonpath='{.spec.template.spec.containers[0].image}'` → a imagem tem a **tag do commit** que acabou de ser mesclado.

### Bloco 5 — Kubernetes (6:30–8:00)

No T4: `kubectl -n fiap-workshop get all`. Depois, em `k8s/base/`:

- `api-deployment.yaml`: probes (`/health/ready` só no arranque, para não gastar a cota do banco; `/health/live` periódico), CPU/memória reservadas e limites, container sem root.
- `kustomization.yaml`: **ConfigMap** (configuração) e **Secret** (senha do banco, chave do JWT, chave do webhook).
- `api-hpa.yaml`: **HPA** de 1 a 3 réplicas, CPU 70% do reservado, memória 400Mi; espera 60 s antes de escalar (o pico de CPU do arranque do .NET disparava escala sem carga).
- `overlays/local` e `overlays/azure`: a mesma base para o k3d na máquina e para a Azure.

### Bloco 6 — Consumo das APIs (8:00–11:30)

No Swagger (`http://20.226.32.136/swagger`). Copiar o `id` devolvido em cada passo.

1. **Login** — `POST /api/v1/auth/login`:
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "email": "admin@admin.com", "password": "Admin@123" }
   ```
   *Authorize* → **Bearer** → colar o `accessToken`.

2. **Listagem por prioridade** — `GET /api/v1/service-orders`: em execução primeiro, depois aguardando aprovação, em diagnóstico e recebidas; mais antigas primeiro; finalizadas e entregues fora.

3. **Abertura de OS** — `POST /api/v1/service-orders` (cliente João, Fiat Argo):
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "customerId": "22222222-2222-2222-2222-222222220002", "vehicleId": "33333333-3333-3333-3333-333333330002", "problemDescription": "Ruído ao frear", "odometerReading": 61000 }
   ```
   **Falar da decisão** (até 30 s): a abertura recebe cliente e veículo já cadastrados; serviços e peças entram no orçamento, depois do diagnóstico, porque o orçamento **reserva estoque** e só o mecânico sabe o que será usado.

4. **Consulta de status** — `GET /api/v1/service-orders/{id}/status` → `Received`.

5. **Diagnóstico** — `POST /api/v1/service-orders/{id}/diagnosis`:
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "diagnoseDescription": "Pastilhas dianteiras gastas" }
   ```

6. **Orçamento** — `POST /api/v1/service-orders/{id}/budget` (troca de pastilhas + 2 pastilhas):
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "services": [ { "serviceId": "44444444-4444-4444-4444-444444440004", "quantity": 1 } ], "parts": [ { "inventoryItemId": "55555555-5555-5555-5555-555555550003", "quantity": 2 } ] }
   ```
   Status → `AwaitingApproval`; valor calculado automaticamente; estoque reservado.

7. **Webhook de aprovação** — *Authorize* → **ApiKey** → colar a chave do bloco de notas. `POST /api/v1/webhooks/budget-decisions`:
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "serviceOrderId": "<id da OS>", "decision": 0 }
   ```
   Status → `InProgress`. Repetir a mesma chamada → `200` com `"Budget decision already applied"` (idempotente: um sistema externo pode reenviar sem efeito colateral).

   **Testar no ensaio**: se o Swagger responder `401` mesmo com a chave no *Authorize*, ele não está enviando o header — faça essa chamada no T4 (é até mais fiel a "um sistema externo chamando"):
   ```bash
   curl -s http://20.226.32.136/api/v1/webhooks/budget-decisions -H "Content-Type: application/json" \
     -H "X-Api-Key: $(cd infra && terraform output -raw webhook_api_key)" \
     -d '{"correlationId":"3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718","serviceOrderId":"<id da OS>","decision":0}'
   ```

8. **E-mails** — Mailpit (`http://localhost:8025`): um e-mail para `joao.almeida@example.com` a cada mudança de status (diagnóstico, aguardando aprovação, em execução).

### Bloco 7 — Escalabilidade automática (11:30–14:15)

1. T1 (HPA) e T2 (pods) lado a lado: `REPLICAS 1`, um pod.
2. Rodar o k6 (comando do ensaio) e abrir `http://localhost:5665` (painel do k6).
3. Explicar enquanto sobe: 30 usuários simultâneos consultando a listagem; o HPA compara a CPU média com 70% do reservado e espera 60 s acima do alvo.
4. ~60–90 s: **`REPLICAS 3`**, dois pods novos `Pending → ContainerCreating → Running 1/1`.
5. No painel do k6: **0% de erros** enquanto os pods novos entram.
6. `kubectl -n fiap-workshop describe hpa api` (T4) → evento `New size: 3; reason: cpu resource utilization ... above target`.
7. Fim da carga: o HPA volta para 1 réplica depois de 5 min (não esperar no vídeo — explicar, ou cortar e mostrar o evento `New size: 1` depois).
8. Opcional (30 s), durante a carga: `kubectl -n fiap-workshop delete pod <um pod da api>` → o Deployment cria outro e o k6 segue sem erros.

### Bloco 8 — Encerramento (14:15–15:00)

- Qualidade: SonarQube Cloud com 97,5% de cobertura, 0 bugs e 0 vulnerabilidades; refatoração da Fase 2 (guarda única de transição de status, usuário de sistema protegido, tratamento de erros).
- Tudo documentado no README (como rodar local, no Kubernetes e provisionar com Terraform) e decisões no CONTEXT.md.
- Agradecimento.

---

## Depois de gravar

1. Publicar no YouTube (não listado) ou Vimeo.
2. `gh variable set DEPLOY_ENABLED --body false`, **antes** do merge do passo 3: assim o pipeline da `release` não tenta fazer deploy numa VM desligada ou destruída.
3. Link do vídeo: trocar o "a publicar" em `README.md` (seção "Vídeo demonstrativo") e em `docs/entrega-fase-2/entrega-fase-2.html` (seção "Links"), regenerar o PDF (Git Bash, na pasta `docs/entrega-fase-2`) e levar até a `release` (feature → develop → release), que é o branch padrão do repositório, o que o avaliador vê:
   ```bash
   "/c/Program Files/Google/Chrome/Application/chrome.exe" --headless=new --disable-gpu --no-pdf-header-footer \
     --print-to-pdf="$(pwd -W)/entrega-fase-2.pdf" "file:///$(pwd -W)/entrega-fase-2.html"
   ```
4. Portal do aluno: enviar o `entrega-fase-2.pdf` (repositório, desenho da arquitetura e link do vídeo).
5. Infraestrutura: `terraform destroy` na pasta `infra` e remover o runner em *Settings → Actions → Runners*. Antes de destruir, decidir se a api fica no ar para o frontend (ver `docs/plano-proximos-passos.md` no repositório do frontend).
