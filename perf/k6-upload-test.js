import http from 'k6/http';
import { check, sleep } from 'k6';

// Configuration : 20 utilisateurs simultanés pendant 30 secondes
export const options = {
  vus: 20,
  duration: '30s',
  insecureSkipTLSVerify: true, // Ignorer SSL en localhost
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% des requêtes sous 2s (upload + traitement)
    http_req_failed: ['rate<0.01'],    // Moins de 1% d'échec
  },
};

// On crée un contenu "texte" de 100KB environ
const fileContent = 'a'.repeat(1024 * 100); 

// On prépare l'objet fichier pour k6
const fileData = http.file(fileContent, 'perf-test.txt', 'text/plain');

export default function () {
  // 1. Authentification
  const loginUrl = 'http://localhost:5180/api/Auth/login';
  const loginPayload = JSON.stringify({
    email: "e@g.c",
    password: "Mdpe@g.c"
  });

  const loginParams = {
    headers: { 'Content-Type': 'application/json' },
  };

  const loginRes = http.post(loginUrl, loginPayload, loginParams);

  // Si le login fonctionne, on récupère le token
  if (check(loginRes, { 'Login Successful': (r) => r.status === 200 })) {
    
    const token = loginRes.json('token');

    // 2. Upload du fichier
    const uploadUrl = 'http://localhost:5180/api/Files';
    
    const uploadPayload = {
      file: fileData, 
    };

    const uploadParams = {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    };

    const uploadRes = http.post(uploadUrl, uploadPayload, uploadParams);

    check(uploadRes, {
      'Upload status is 201': (r) => r.status === 201,
      'Upload duration < 1s': (r) => r.timings.duration < 1000,
    });
  }

  sleep(1);
}
