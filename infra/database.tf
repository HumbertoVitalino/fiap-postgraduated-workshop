# Azure SQL Database, fora do cluster: mesmo motor do SQL Server do docker-compose e do k3d.

# O nome do servidor SQL é global na Azure (vira <nome>.database.windows.net), por isso o sufixo.
resource "random_string" "sql_suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "random_password" "sql_admin" {
  length           = 24
  min_upper        = 2
  min_lower        = 2
  min_numeric      = 2
  min_special      = 2
  override_special = "!#%*-_+="
}

resource "azurerm_mssql_server" "main" {
  name                         = "${var.prefix}-sql-${random_string.sql_suffix.result}"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = "workshopadmin"
  administrator_login_password = random_password.sql_admin.result
  minimum_tls_version          = "1.2"
}

# Oferta gratuita: serverless, pausa sozinho quando ocioso e, se a cota mensal acabar, pausa até o mês
# seguinte em vez de cobrar. Criado via azapi porque o azurerm não tem useFreeLimit.
resource "azapi_resource" "database" {
  type      = "Microsoft.Sql/servers/databases@2023-08-01"
  name      = var.database_name
  parent_id = azurerm_mssql_server.main.id
  location  = azurerm_resource_group.main.location

  body = {
    sku = {
      name     = "GP_S_Gen5"
      tier     = "GeneralPurpose"
      family   = "Gen5"
      capacity = 2
    }
    properties = {
      useFreeLimit                     = true
      freeLimitExhaustionBehavior      = "AutoPause"
      autoPauseDelay                   = 60
      minCapacity                      = 0.5
      maxSizeBytes                     = 34359738368 # 32 GB, o teto da oferta gratuita
      requestedBackupStorageRedundancy = "Local"
      zoneRedundant                    = false
    }
  }
}

# Só a VM (onde rodam a api e o Job que aplica o schema) e o seu IP alcançam o banco.
resource "azurerm_mssql_firewall_rule" "vm" {
  name             = "allow-k3s-vm"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = azurerm_public_ip.vm.ip_address
  end_ip_address   = azurerm_public_ip.vm.ip_address
}

resource "azurerm_mssql_firewall_rule" "admin" {
  name             = "allow-admin"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = var.admin_ip
  end_ip_address   = var.admin_ip
}
