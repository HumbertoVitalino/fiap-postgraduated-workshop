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

- **T0 com `Connection timed out`**: seu IP público mudou e o firewall só libera SSH para o IP antigo. Confira com `curl https://api.ipify.org`, atualize `admin_ip` no `infra/terraform.tfvars` e rode `terraform plan -out=tfplan` + `terraform apply tfplan`.
- **Mailpit vazio no navegador**: a porta 8025 está ocupada pelo Mailpit do docker-compose local, e o T3 não conseguiu abrir. Rode `docker compose stop` na raiz do repositório e reabra o T3.
- O T3 só cai se o pod do Mailpit for recriado (quando o manifesto dele muda). Se cair, reabra com o mesmo comando.

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
2. PR #90 (`develop → release`), já mesclado. O deploy desse merge foi cancelado com a VM desligada, e o bloco 4 dispara o pipeline de novo.
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
- Mailpit vazio: apague os e-mails do ensaio (lixeira, *Delete all*).
- HPA de volta a 1 réplica (T1).
- Notificações do Windows e do navegador desligadas.
- Checks do PR #90 verdes. O PR é aberto pelo `github-actions[bot]`, e o GitHub segura os workflows disparados por ele até alguém aprovar: se o CodeQL aparecer como *action required*, clique em **Approve and run** e espere terminar antes de gravar.

---

## Roteiro

Cada passo tem **o que fazer** e, em citação, **o que falar**. A fala é uma sugestão: diga com as suas palavras, não precisa decorar.

### Bloco 1 — Abertura (0:00–0:45)

Tela: README do repositório, no topo.

> Olá, eu sou o Humberto, e este é o vídeo da Fase 2 do Tech Challenge da pós em Arquitetura de Software da FIAP.
>
> Na Fase 1 eu construí o back-end de gestão de uma oficina mecânica: ordens de serviço, clientes, veículos, serviços e controle de estoque de peças. Na Fase 2, o cenário é de crescimento: mais demanda, novas unidades e a necessidade de alta disponibilidade. Por isso o objetivo desta fase é deixar a aplicação escalável, com provisionamento e deploy automatizados, e com mais qualidade de código.
>
> Neste vídeo eu vou mostrar a arquitetura, a infraestrutura como código, o pipeline de CI/CD fazendo o deploy no Kubernetes, o consumo das APIs novas e a escala automática sob carga.

### Bloco 2 — Arquitetura (0:45–2:30)

Tela: README, seção **Arquitetura de implantação** (diagrama).

1. Mostrar o diagrama de cima para baixo.

   > A aplicação é uma API em .NET 10, organizada em Clean Architecture, com quatro camadas: Domain, Application, Infrastructure e Api. O domínio não depende de nada externo, e as regras de negócio ficam no Domain e nos use cases da Application.
   >
   > A infraestrutura está na Azure. Tem uma máquina virtual Linux rodando o k3s, que é uma distribuição leve e certificada do Kubernetes. Dentro do cluster, no namespace fiap-workshop, roda a API, de uma a três réplicas, controladas pelo Horizontal Pod Autoscaler, e o Mailpit, que é um servidor de e-mail de teste para as notificações de status. O banco é um Azure SQL Database, fora do cluster, na oferta gratuita.

2. Seguir o fluxo de deploy no diagrama.

   > O fluxo de deploy é: o desenvolvedor abre um PR de uma branch feature para a develop e depois da develop para a release. O merge na release dispara o pipeline no GitHub Actions, que compila, roda os testes, gera a imagem Docker e publica no GitHub Container Registry. O job de deploy roda num runner instalado na própria VM, que aplica o schema no banco e os manifestos no cluster.

3. Justificar as escolhas.

   > Duas escolhas que vale explicar. Primeiro, k3s numa VM em vez do AKS, o Kubernetes gerenciado da Azure: o AKS consumiria o crédito de estudante com nós e load balancer pagos, sem ganho para esta solução. Segundo, o runner dentro da VM: assim a API do Kubernetes não fica exposta na internet e nenhuma credencial da Azure fica guardada no GitHub.

4. Mostrar a raiz do repositório.

   > No repositório: o código em src, os testes em tests, os manifestos do Kubernetes em k8s, o Terraform em infra e os pipelines em .github/workflows.

### Bloco 3 — Infraestrutura como código (2:30–4:00)

1. Abrir `infra/main.tf`.

   > A infraestrutura é toda criada com Terraform. Aqui no main.tf ficam o grupo de recursos, a rede virtual, o firewall e a VM. O firewall libera a porta 80 para a internet e a porta 22, de SSH, só para o meu IP. A VM instala o k3s sozinha no primeiro boot, pelo cloud-init.

2. Abrir `infra/database.tf`.

   > No database.tf fica o Azure SQL. Ele usa a oferta gratuita, que pausa sozinha quando o banco fica ocioso, e o firewall do banco só aceita conexão da VM e do meu IP.

3. Mostrar `infra/secrets.tf` rapidamente, **sem** rodar `terraform output`.

   > Os segredos, que são a senha do banco, a chave do JWT e a chave do webhook, também são gerados pelo Terraform. Eles vão para o pipeline como GitHub Secrets e chegam no cluster como Secret do Kubernetes. Nada disso fica no código.

4. Portal da Azure, grupo `fiap-workshop-rg`.

   > Aqui no portal estão os recursos que o Terraform criou: a VM, o IP público, a rede, o firewall e o banco.

5. Opcional (~30 s): `terraform plan` na pasta `infra`.

   > E o terraform plan mostra "No changes": a infraestrutura no ar é exatamente a que está descrita no código.

### Bloco 4 — CI/CD e deploy (4:00–6:30, com pausa)

1. PR #90 (já mesclado): mostrar a lista de commits.

   > Este é o PR da develop para a release, com a refatoração da Fase 2 e a documentação da entrega. Ele já foi mesclado, mas o deploy ainda não aconteceu, e eu vou disparar o pipeline agora para mostrar ele de ponta a ponta.

2. *Actions* → **CI/CD - Release** → **Run workflow** (branch `release`) → abrir a execução.

   > Este é o pipeline da release. Primeiro o build da solução. Depois os testes unitários e, em seguida, os testes de integração, que sobem um SQL Server num container. Em paralelo, a análise de qualidade no SonarQube. Com os testes passando, ele gera a imagem Docker e publica no registry com a tag do commit. Por último vem o deploy no Kubernetes.

3. **Pausar a gravação** até o job de deploy começar (~6–8 min).

4. Retomar no job **Deploy to Kubernetes (Azure)** e abrir os passos.

   > O job de deploy roda no runner self-hosted, dentro da VM. Primeiro ele espera o banco acordar, porque a oferta gratuita pausa quando fica ociosa. Depois faz o deploy do banco: aplica o schema e os dados de demonstração com o sqlcmd. Os scripts são idempotentes, então rodar de novo não duplica nada. Em seguida ele monta o Secret com os GitHub Secrets, troca a tag da imagem pela do commit, aplica os manifestos com kubectl apply e espera o rollout terminar. Por fim, um smoke test: chama o health check e faz um login de verdade.

5. No T4:
   ```bash
   kubectl -n fiap-workshop get deploy api -o jsonpath='{.spec.template.spec.containers[0].image}'
   ```

   > E no cluster a imagem da API agora é a do commit que acabou de passar pelo pipeline.

### Bloco 5 — Kubernetes (6:30–8:00)

1. No T4: `kubectl -n fiap-workshop get all`.

   > Este é o que está rodando no cluster: os Deployments e os Services da API e do Mailpit, e o HPA.

2. `k8s/base/api-deployment.yaml`.

   > No Deployment da API, as probes: a de startup usa o /health/ready, que testa o banco, e só roda quando o pod sobe. As probes periódicas usam o /health/live, que não toca no banco, para não gastar a cota do Azure SQL gratuito. Também defino CPU e memória reservadas e o limite de cada pod, e o container roda sem root.

3. `k8s/base/kustomization.yaml`.

   > A configuração não sensível vai num ConfigMap: ambiente, JWT, servidor de e-mail. O que é sensível vai num Secret: connection string, chave do JWT e chave do webhook.

4. `k8s/base/api-hpa.yaml`.

   > O HPA escala de uma a três réplicas. A métrica principal é CPU, com alvo de 70% do reservado. Memória entra como proteção. Ele espera 60 segundos acima do alvo antes de escalar, porque o pico de CPU na inicialização do .NET fazia ele escalar sem carga nenhuma.

5. `k8s/overlays/`.

   > Com o Kustomize, a mesma base serve para dois ambientes: o cluster local, com k3d na minha máquina, e a Azure. Os overlays só mudam o que é diferente em cada um.

### Bloco 6 — Consumo das APIs (8:00–11:30)

No Swagger (`http://20.226.32.136/swagger`). **Use o `id` da OS nova**: a OS do ensaio continua na listagem.

1. **Login**: `POST /api/v1/auth/login`, depois *Authorize* → **Bearer** → colar o `accessToken`.
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "email": "admin@admin.com", "password": "Admin@123" }
   ```

   > Esta é a documentação Swagger da aplicação no ar, que também é a collection das APIs. O documento OpenAPI está no repositório e pode ser importado no Postman. Primeiro eu faço login, que devolve um token JWT, e autorizo as chamadas com ele.

2. **Listagem por prioridade**: `GET /api/v1/service-orders`.

   > A listagem de ordens de serviço segue a regra pedida: primeiro as em execução, depois as aguardando aprovação, em diagnóstico e recebidas. Dentro de cada status, as mais antigas vêm primeiro. As finalizadas e entregues não aparecem, mas não são apagadas: é uma exclusão lógica, só da listagem.

3. **Abertura de OS**: `POST /api/v1/service-orders` (cliente João, Fiat Argo). Copiar o `id`.
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "customerId": "22222222-2222-2222-2222-222222220002", "vehicleId": "33333333-3333-3333-3333-333333330002", "problemDescription": "Ruído ao frear", "odometerReading": 61000 }
   ```

   > Agora abro uma ordem de serviço para o João, com o Fiat Argo dele, e a API devolve o identificador da OS.
   >
   > Uma decisão de projeto aqui: a abertura recebe o cliente e o veículo já cadastrados, e os serviços e peças entram depois, no orçamento, após o diagnóstico. O motivo é de negócio: o orçamento reserva estoque, e só depois do diagnóstico o mecânico sabe o que vai ser usado. Reservar peças na abertura prenderia estoque com um palpite.

4. **Consulta de status**: `GET /api/v1/service-orders/{id}/status` → `Received`.

   > A consulta de status devolve a situação atual da OS e quando ela mudou pela última vez. Agora ela está como recebida.

5. **Diagnóstico**: `POST /api/v1/service-orders/{id}/diagnosis`.
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "diagnoseDescription": "Pastilhas dianteiras gastas" }
   ```

   > O mecânico registra o diagnóstico, e a OS passa para "em diagnóstico".

6. **Orçamento**: `POST /api/v1/service-orders/{id}/budget` (troca de pastilhas + 2 pastilhas).
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "services": [ { "serviceId": "44444444-4444-4444-4444-444444440004", "quantity": 1 } ], "parts": [ { "inventoryItemId": "55555555-5555-5555-5555-555555550003", "quantity": 2 } ] }
   ```

   > Com o diagnóstico, ele monta o orçamento: o serviço de troca de pastilhas e duas pastilhas. O valor total é calculado automaticamente, as peças ficam reservadas no estoque e a OS vai para "aguardando aprovação" do cliente.

7. **Webhook de aprovação**: *Authorize* → **ApiKey** → colar a chave do bloco de notas. `POST /api/v1/webhooks/budget-decisions`:
   ```json
   { "correlationId": "3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718", "serviceOrderId": "<id da OS>", "decision": 0 }
   ```

   > A aprovação do orçamento vem de fora: um sistema externo, onde o cliente aprova ou recusa, chama este webhook. Ele não usa o login dos funcionários, e sim uma chave de API no header, que fica num Secret do Kubernetes. Decisão zero é aprovar. A OS vai para "em execução".

   Repetir a mesma chamada → `200` com `"Budget decision already applied"`.

   > E se o sistema externo reenviar a mesma notificação, o que é comum em webhook, a API responde que a decisão já foi aplicada, sem efeito colateral: o endpoint é idempotente.

   Se o Swagger der `401`, faça a chamada no T4:
   ```bash
   curl -s http://20.226.32.136/api/v1/webhooks/budget-decisions -H "Content-Type: application/json" \
     -H "X-Api-Key: $(cd infra && terraform output -raw webhook_api_key)" \
     -d '{"correlationId":"3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718","serviceOrderId":"<id da OS>","decision":0}'
   ```

8. **E-mails**: Mailpit (`http://localhost:8025`).

   > E a cada mudança de status o cliente recebe um e-mail. Aqui estão os três e-mails do João: em diagnóstico, aguardando aprovação e em execução. Isso funciona com evento de domínio: a OS dispara um evento quando o status muda, e um handler na camada de aplicação envia o e-mail. Se o envio falhar, a mudança de status não é desfeita, só fica registrada no log.

### Bloco 7 — Escalabilidade automática (11:30–14:15)

1. T1 (HPA) e T2 (pods) lado a lado: `REPLICAS 1`, um pod.

   > Agora a escala automática. Aqui em cima está o HPA, com uma réplica e a CPU quase zerada. Aqui embaixo, o único pod da API.

2. Rodar o k6 (PowerShell, na raiz do repositório) e abrir `http://localhost:5665`.
   ```powershell
   docker run --rm -t -p 5665:5665 -v "${PWD}/tests/load:/scripts" `
     -e BASE_URL=http://20.226.32.136 -e K6_WEB_DASHBOARD=true -e K6_WEB_DASHBOARD_HOST=0.0.0.0 `
     grafana/k6 run /scripts/hpa-load.js
   ```

   > Para simular um horário de pico, eu uso o k6, uma ferramenta de teste de carga: 30 usuários simultâneos consultando a listagem de ordens de serviço sem pausa, durante uns quatro minutos.

3. Enquanto a CPU sobe (T1).

   > A CPU já passou muito do alvo de 70%. O HPA espera 60 segundos acima do alvo para ter certeza de que não é um pico passageiro.

4. ~60–90 s: **`REPLICAS 3`** no T1; no T2, dois pods novos `Pending → ContainerCreating → Running 1/1`.

   > Pronto: o HPA escalou para três réplicas, e aqui os dois pods novos sendo criados até ficarem prontos. O Service já distribui as requisições entre os três.

5. Painel do k6.

   > No painel do k6, nenhuma requisição falhou enquanto os pods novos entravam.

6. No T4:
   ```bash
   kubectl -n fiap-workshop get events --field-selector reason=SuccessfulRescale
   ```

   > E aqui o registro do Kubernetes: novo tamanho, três réplicas, porque o uso de CPU passou do alvo.

7. Fim da carga (não esperar no vídeo).

   > Quando a carga acaba, o HPA volta para uma réplica depois de cinco minutos abaixo do alvo. Essa espera evita ficar criando e destruindo pods a cada oscilação.

8. Opcional (30 s), durante a carga: `kubectl -n fiap-workshop delete pod <um pod da api>`.

   > E se um pod morrer, o Deployment cria outro no lugar, e a carga continua sem erro.

### Bloco 8 — Encerramento (14:15–15:00)

Tela: README, seção **Qualidade e entrega**.

> Para fechar, a qualidade. O SonarQube Cloud mostra 97,5% de cobertura de testes no domínio e nos use cases, zero bugs e zero vulnerabilidades. Na refatoração desta fase, centralizei as transições de status da ordem de serviço numa única regra, protegi o usuário de sistema do webhook e melhorei o tratamento de erros.
>
> Tudo está documentado no README: como rodar localmente, como fazer o deploy no Kubernetes e como provisionar a infraestrutura com Terraform. As decisões de projeto e as alternativas descartadas estão no CONTEXT.md.
>
> Obrigado por assistir.

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
