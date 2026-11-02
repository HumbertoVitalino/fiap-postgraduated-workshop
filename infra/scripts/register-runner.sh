#!/usr/bin/env sh
# Registers the k3s VM as a self-hosted GitHub Actions runner (label "azure-k3s"), used by the deploy job.
# Run once after `terraform apply`, from any directory: sh infra/scripts/register-runner.sh
# Requires: gh logged in with admin access to the repository, terraform in PATH (or VM_IP set), and the
# SSH key used by the VM (~/.ssh/fiap-workshop-azure).
set -eu

REPO="${REPO:-HumbertoVitalino/fiap-postgraduated-workshop}"
INFRA_DIR="$(cd "$(dirname "$0")/.." && pwd)"
VM_IP="${VM_IP:-$(terraform -chdir="$INFRA_DIR" output -raw vm_public_ip)}"

# O token de registro vale 1 hora: por isso é gerado aqui, na hora, e não guardado no Terraform.
TOKEN="$(gh api -X POST "repos/$REPO/actions/runners/registration-token" --jq .token)"
VERSION="$(gh api repos/actions/runner/releases/latest --jq .tag_name | sed 's/^v//')"

ssh -i ~/.ssh/fiap-workshop-azure -o StrictHostKeyChecking=accept-new "azureuser@$VM_IP" \
  sh -s -- "$REPO" "$TOKEN" "$VERSION" <<'EOF'
set -eu
REPO="$1"; TOKEN="$2"; VERSION="$3"

echo "Aguardando o cloud-init (instalação do k3s)..."
cloud-init status --wait > /dev/null
sudo k3s kubectl get nodes

mkdir -p ~/actions-runner && cd ~/actions-runner
if [ ! -f .runner ]; then
  curl -sSL "https://github.com/actions/runner/releases/download/v$VERSION/actions-runner-linux-x64-$VERSION.tar.gz" | tar -xz
  sudo ./bin/installdependencies.sh > /dev/null
  ./config.sh --unattended --url "https://github.com/$REPO" --token "$TOKEN" \
    --name "$(hostname)" --labels azure-k3s --replace
  sudo ./svc.sh install
  sudo ./svc.sh start
fi
sudo ./svc.sh status | grep -E "Active:|active"
EOF
