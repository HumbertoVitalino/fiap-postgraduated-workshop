variable "subscription_id" {
  description = "Id da assinatura Azure (az account show --query id -o tsv)."
  type        = string
}

variable "location" {
  description = "Região. A assinatura Azure for Students só permite algumas (ver política 'Allowed resource deployment regions')."
  type        = string
  default     = "brazilsouth"
}

variable "prefix" {
  description = "Prefixo do nome de todos os recursos."
  type        = string
  default     = "fiap-workshop"
}

variable "vm_size" {
  description = "Tamanho da VM do k3s. Precisa de >= 4 GB de memória e cota disponível na família."
  type        = string
}

variable "admin_username" {
  description = "Usuário Linux da VM."
  type        = string
  default     = "azureuser"
}

variable "ssh_public_key_path" {
  description = "Chave pública SSH instalada na VM."
  type        = string
  default     = "~/.ssh/fiap-workshop-azure.pub"
}

variable "admin_ip" {
  description = "Seu IP público. Único endereço liberado para SSH na VM e para conexão direta ao Azure SQL."
  type        = string
}

variable "k3s_version" {
  description = "Mesma versão do k3s usada pelo k3d no ambiente local."
  type        = string
  default     = "v1.35.5+k3s1"
}

variable "database_name" {
  description = "Nome do banco: o mesmo da connection string do docker-compose e do k3d."
  type        = string
  default     = "Fiap_Workshop"
}
