# Segredos da api gerados aqui para não serem inventados à mão. Vão para os GitHub Secrets do pipeline
# (o job de deploy monta o Secret do K8s com eles) e continuam consultáveis por terraform output — os
# GitHub Secrets não podem ser lidos de volta, e a chave do webhook é usada para chamar a api.

resource "random_password" "jwt_secret_key" {
  length  = 64
  special = false
}

resource "random_password" "webhook_api_key" {
  length  = 40
  special = false
}

output "jwt_secret_key" {
  sensitive = true
  value     = random_password.jwt_secret_key.result
}

output "webhook_api_key" {
  description = "Header X-Api-Key do webhook de decisão do orçamento. Ver com: terraform output -raw webhook_api_key"
  sensitive   = true
  value       = random_password.webhook_api_key.result
}
