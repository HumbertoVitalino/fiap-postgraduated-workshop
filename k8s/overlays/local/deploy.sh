#!/usr/bin/env sh
# Builds the api image, loads it into the local k3d cluster and applies the local overlay.
# Usage (from any directory): sh k8s/overlays/local/deploy.sh
set -eu

CLUSTER="${CLUSTER:-fiap-workshop}"
NAMESPACE=fiap-workshop
IMAGE=fiap-workshop-api:local
OVERLAY_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$OVERLAY_DIR/../../.." && pwd)"

for file in api-secrets.env sqlserver-secrets.env; do
  if [ ! -f "$OVERLAY_DIR/$file" ]; then
    echo "Faltando $OVERLAY_DIR/$file - copie de $file.example e ajuste os valores." >&2
    exit 1
  fi
done

docker build -t "$IMAGE" "$ROOT_DIR"
k3d image import "$IMAGE" -c "$CLUSTER"

kubectl apply -f "$ROOT_DIR/k8s/base/namespace.yaml"
kubectl -n "$NAMESPACE" create configmap db-scripts \
  --from-file=init.sql="$ROOT_DIR/db/init.sql" \
  --from-file=seed-demo.sql="$ROOT_DIR/db/seed-demo.sql" \
  --dry-run=client -o yaml | kubectl apply -f -

# Job é imutável: recriar a cada deploy reaplica os scripts (idempotentes).
kubectl -n "$NAMESPACE" delete job db-init --ignore-not-found
kubectl apply -k "$OVERLAY_DIR"

# A tag "local" não muda entre builds; o restart faz os pods pegarem a imagem recém-importada.
kubectl -n "$NAMESPACE" rollout restart deployment/api
kubectl -n "$NAMESPACE" rollout status deployment/api --timeout=5m
