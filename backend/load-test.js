import http from 'k6/http';
import { check, sleep } from 'k6';

// Configuration du test
export const options = {
  // On simule 20 utilisateurs simultanés pendant 30 secondes
  vus: 20,
  duration: '30s',
  // On ignore les erreurs de certificats SSL (car on est en localhost)
  insecureSkipTLSVerify: true,
};

export default function () {
  const url = 'http://localhost:5180/api/Auth/login';

  // Compte existant pour le test de connexion
  const payload = JSON.stringify({
    email: "e@g.c", 
    password: "Mdpe@g.c" 
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  // Envoi de la requête
  const res = http.post(url, payload, params);

  // Vérifications
  check(res, {
    'status is 200 (Login OK)': (r) => r.status === 200,
    'status is 401 (Login Failed)': (r) => r.status === 401, // Juste pour info
    'temps de réponse < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1); // Pause d'1 seconde entre chaque essai
}
