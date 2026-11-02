terraform {
  required_version = ">= 1.6"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    # Só para o banco: o azurerm (4.x e 5.x) não expõe a oferta gratuita do Azure SQL (useFreeLimit).
    azapi = {
      source  = "Azure/azapi"
      version = "~> 2.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id

  # A assinatura Azure for Students só precisa de Compute, Network e Sql, registrados uma vez à mão
  # (az provider register). Sem isto, o provider tentaria registrar dezenas de outros na primeira execução.
  resource_provider_registrations = "none"
}
