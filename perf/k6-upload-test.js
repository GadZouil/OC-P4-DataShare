import http from 'k6/http';
import { check, sleep } from 'k6';

// =============================================================================
// DataShare - Test de charge k6 : upload authentifié (POST /api/files)
//
// Usage :
//   k6 run perf/k6-upload-test.js                              # API Docker (port 5000)
//   k6 run -e BASE_URL=http://localhost:5180 perf/k6-upload-test.js   # API `dotnet run`
//   k6 run -e VUS=50 -e DURATION=60s -e FILE_KB=1024 perf/k6-upload-test.js
//
// Le test crée son propre compte (setup) : aucune donnée pré-existante requise.
// =============================================================================

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const FILE_KB = parseInt(__ENV.FILE_KB || '100', 10);

export const options = {
  // 20 utilisateurs simultanés pendant 30 secondes (par défaut)
  vus: parseInt(__ENV.VUS || '20', 10),
  duration: __ENV.DURATION || '30s',
  insecureSkipTLSVerify: true, // Ignorer SSL en localhost
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95 % des requêtes sous 2 s (upload + écriture disque)
    http_req_failed: ['rate<0.01'],    // Moins de 1 % d'échec
    'checks{type:upload}': ['rate>0.99'],
  },
};

// Contenu texte de FILE_KB Ko envoyé à chaque itération
const fileData = http.file('a'.repeat(1024 * FILE_KB), 'perf-test.txt', 'text/plain');

// setup() s'exécute une seule fois : création d'un compte dédié + récupération du JWT.
export function setup() {
  const email = `k6-${Date.now()}@perf.test`;
  const password = 'K6-Perf-Password-123';

  const res = http.post(
    `${BASE_URL}/api/auth/register`,
    JSON.stringify({ email, password }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (res.status !== 200) {
    throw new Error(`Inscription du compte de test impossible (HTTP ${res.status}) : ${res.body}`);
  }
  return { token: res.json('token') };
}

export default function (data) {
  const res = http.post(
    `${BASE_URL}/api/files`,
    { file: fileData, expiresInDays: '1' },
    { headers: { Authorization: `Bearer ${data.token}` }, tags: { type: 'upload' } }
  );

  check(res, {
    'Upload status is 201': (r) => r.status === 201,
    'Upload duration < 1s': (r) => r.timings.duration < 1000,
  }, { type: 'upload' });

  sleep(1);
}
