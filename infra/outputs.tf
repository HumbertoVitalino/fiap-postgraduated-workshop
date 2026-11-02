output "vm_public_ip" {
  description = "IP público da VM: a api responde em http://<ip>/"
  value       = azurerm_public_ip.vm.ip_address
}

output "ssh_command" {
  description = "Comando para entrar na VM."
  value       = "ssh -i ~/.ssh/fiap-workshop-azure ${var.admin_username}@${azurerm_public_ip.vm.ip_address}"
}

output "sql_server_fqdn" {
  value = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_admin_login" {
  value = azurerm_mssql_server.main.administrator_login
}

output "sql_admin_password" {
  description = "Ver com: terraform output -raw sql_admin_password"
  sensitive   = true
  value       = random_password.sql_admin.result
}

# Connection Timeout=60: a primeira conexão depois do auto-pause espera o banco retomar (~1 min).
output "connection_string" {
  description = "Vai para o Secret da api (GitHub Secret no CI/CD). Ver com: terraform output -raw connection_string"
  sensitive   = true
  value       = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${var.database_name};User Id=${azurerm_mssql_server.main.administrator_login};Password=${random_password.sql_admin.result};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
}
