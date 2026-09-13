import http from 'k6/http';
import { check, sleep } from 'k6';

// =============================================================================
// DataShare - Test de charge k6 : connexion (POST /api/auth/login)
// Mesure le coût de la vérification PBKDF2 + génération du JWT sous charge.
//
// Usage :
//   k6 run perf/k6-login-test.js
//   k6 run -e BASE_URL=http://localhost:5180 -e VUS=50 perf/k6-login-test.js
// =============================================================================

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  vus: parseInt(__ENV.VUS || '20', 10),
  duration: __ENV.DURATION || '30s',
  insecureSkipTLSVerify: true,
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

const headers = { 'Content-Type': 'application/json' };

export function setup() {
  const credentials = { email: `k6-login-${Date.now()}@perf.test`, password: 'K6-Perf-Password-123' };
  const res = http.post(`${BASE_URL}/api/auth/register`, JSON.stringify(credentials), { headers });
  if (res.status !== 200) {
    throw new Error(`Inscription du compte de test impossible (HTTP ${res.status}) : ${res.body}`);
  }
  return credentials;
}

export default function (credentials) {
  const res = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify(credentials), { headers });

  check(res, {
    'status is 200 (Login OK)': (r) => r.status === 200,
    'token présent': (r) => typeof r.json('token') === 'string',
    'temps de réponse < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);
}
