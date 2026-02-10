# Rapport de Performance - DataShare API

Ce document détaille les tests de performance initiaux réalisés sur l'API DataShare avant la phase de déploiement.

## 1. Méthodologie

Les tests ont été réalisés sur l'environnement de développement local.

*   **Outil utilisé :** [k6](https://k6.io/)
*   **Type de test :** Test de charge (Load Testing)
*   **Endpoint testé :** `POST /api/Auth/login`
    *   *Justification :* L'authentification est l'opération la plus coûteuse en ressources CPU (hachage de mot de passe `BCrypt` + signature de token `JWT`).

### Scénario du Test
*   **Utilisateurs Virtuels (VUs) :** 20 utilisateurs simultanés constants.
*   **Durée :** 30 secondes.
*   **Objectif :** Valider la stabilité du serveur sous charge concurrente.

---

## 2. Résultats

> **Date du test :** 10 Février 2026
> **Commande :** `k6 run load-test.js`

| Métrique | Valeur Obtenue | Objectif | Statut |
| :--- | :--- | :--- | :--- |
| **Requêtes Totales** | 580 | > 500 | ✅ Succès |
| **Requêtes / seconde** | ~18.75 /s | > 10/s | ✅ Succès |
| **Temps Moyen (Avg)** | 61.74 ms | < 200 ms | ✅ Excellent |
| **P95 (95% des cas)** | 97.29 ms | < 500 ms | ✅ Excellent |
| **Taux d'Erreur HTTP** | 0.00% | 0% | ✅ Parfait |

### Graphique de distribution (Latence)
*   **Minimum :** 45.63 ms
*   **Médiane :** 55.87 ms
*   **Maximum :** 124.7 ms

---

## 3. Analyse Technique

### Observations
L'API a démontré une excellente stabilité et rapidité avec 20 utilisateurs concurrents simulant des connexions en boucle.

1.  **Stabilité :** Aucun échec (HTTP 500 ou Timeout) n'a été constaté (`http_req_failed: 0.00%`). Le mécanisme de **Pooling de connexion** d'Entity Framework Core gère correctement les accès concurrents à la base de données PostgreSQL.
2.  **Performance :** Le temps de réponse moyen de **~62ms** est très bas pour une opération cryptographique. Cela indique que le serveur n'est pas saturé par le calcul des hashs de mots de passe.
3.  **Latence :** Le 95ème percentile à **97ms** garantit une expérience utilisateur fluide même sous charge.

### Limites du test actuel
*   Ce test a été réalisé en `localhost`, éliminant la latence réseau réelle (internet).
*   Le test cible uniquement le CPU. Les opérations d'Entrée/Sortie (Upload de fichiers) n'ont pas été chargées dans ce scénario.

### Recommandations pour la Production

1.  **Serveur Web :** Utiliser Kestrel derrière un Reverse Proxy (Nginx ou Azure App Service) pour gérer la terminaison SSL.
2.  **Optimisation :** Activer la compression Gzip (`ResponseCompression`) pour réduire la taille des réponses JSON.
3.  **Sécurité :** Mettre en place un **Rate Limiting** sur la route `/login` pour empêcher les attaques par force brute (le test actuel montre qu'un attaquant pourrait tenter ~19 mots de passe par seconde par IP sans restriction).

---
