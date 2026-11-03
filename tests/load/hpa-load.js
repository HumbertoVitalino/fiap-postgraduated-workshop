// Carga para observar o HPA da api: 30 usuários virtuais consultando a listagem de OS sem pausa
// por ~4 min. Como não há pausa entre as requisições, satura a CPU de qualquer número de réplicas —
// serve para ver a escala acontecer, não para medir capacidade.
//
// Uso (PowerShell, na raiz do repo; painel em http://localhost:5665):
//   docker run --rm -t -p 5665:5665 -v "${PWD}/tests/load:/scripts" -e K6_WEB_DASHBOARD=true `
//     -e K6_WEB_DASHBOARD_HOST=0.0.0.0 grafana/k6 run /scripts/hpa-load.js
// Outro endereço da api: -e BASE_URL=http://<host>:<porta>
import http from 'k6/http';
import { check } from 'k6';

// host.docker.internal: de dentro do container do k6, aponta para a máquina onde o k3d expõe a 8081.
const BASE = __ENV.BASE_URL || 'http://host.docker.internal:8081';

export const options = {
  stages: [
    { duration: '30s', target: 30 },
    { duration: '3m', target: 30 },
    { duration: '15s', target: 0 },
  ],
};

export function setup() {
  const res = http.post(`${BASE}/api/v1/auth/login`, JSON.stringify({
    correlationId: '3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718',
    email: 'atendente@oficina.com',
    password: 'Demo@123',
  }), { headers: { 'Content-Type': 'application/json' } });
  return { token: res.json('result.accessToken') };
}

export default function (data) {
  const res = http.get(`${BASE}/api/v1/service-orders`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });
  check(res, { 'status 200': (r) => r.status === 200 });
}
