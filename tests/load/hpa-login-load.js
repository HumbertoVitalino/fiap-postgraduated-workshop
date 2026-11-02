// Plano B do hpa-load.js: carga no login para observar o HPA quando o banco é o gargalo (Azure SQL
// gratuito). Cada login confere a senha com BCrypt, que gasta CPU na api e quase nada no banco (uma
// leitura do usuário e a gravação do refresh token), então a CPU dos pods sobe mesmo com o banco lento.
// 10 usuários virtuais sem pausa por ~4 min já passam do alvo do HPA; mais usuários só aumentam a fila.
//
// Uso (PowerShell, na raiz do repo; painel em http://localhost:5665):
//   docker run --rm -t -p 5665:5665 -v "${PWD}/tests/load:/scripts" -e K6_WEB_DASHBOARD=true `
//     -e K6_WEB_DASHBOARD_HOST=0.0.0.0 grafana/k6 run /scripts/hpa-login-load.js
// Outro endereço da api: -e BASE_URL=http://<host>:<porta>
import http from 'k6/http';
import { check } from 'k6';

// host.docker.internal: de dentro do container do k6, aponta para a máquina onde o k3d expõe a 8081.
const BASE = __ENV.BASE_URL || 'http://host.docker.internal:8081';

export const options = {
  stages: [
    { duration: '30s', target: 10 },
    { duration: '3m', target: 10 },
    { duration: '15s', target: 0 },
  ],
};

// Usuária do seed-demo.
const BODY = JSON.stringify({
  correlationId: '3f2b1c4e-8a9d-4e6f-b1a2-c3d4e5f60718',
  email: 'atendente@oficina.com',
  password: 'Demo@123',
});

export default function () {
  const res = http.post(`${BASE}/api/v1/auth/login`, BODY, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res, { 'status 200': (r) => r.status === 200 });
}
