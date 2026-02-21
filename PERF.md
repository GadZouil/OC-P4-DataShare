# Rapport de Performance - DataShare

Ce document présente les résultats des tests de charge sur l'API et le budget de performance défini pour le frontend.

## 1. Performance Backend (API)

**Objectif :** Valider la stabilité de l'upload de fichiers sous charge concurrente.

### Méthodologie
*   **Outil :** [k6](https://k6.io/)
*   **Environnement :** Local (Docker)
*   **Scénario :** 20 utilisateurs virtuels (VUs) uploadant simultanément des fichiers pendant 30 secondes.
*   **Script :** `upload-test.js` (Auth JWT + POST /api/Files)

### Résultats du Test de Charge

> **Date du test :** 17 Février 2026
> **Commande :** `k6 run upload-test.js`

| Métrique | Valeur Obtenue | Objectif | Statut |
| :--- | :--- | :--- | :--- |
| **Itérations Totales** | 500 uploads | > 100 | ✅ Succès |
| **Requêtes / seconde** | ~32 /s | > 10/s | ✅ Succès |
| **Temps Moyen (Avg)** | 107.9 ms | < 500 ms | ✅ Excellent |
| **P95 (95% des cas)** | 125.19 ms | < 2000 ms | ✅ Excellent |
| **Taux d'Erreur** | 0.00% | < 1% | ✅ Parfait |
| **Données Transférées** | 52 MB | - | Info |

### Analyse
Le serveur encaisse parfaitement la charge sans dégradation notable. Le temps de réponse moyen de ~100ms pour un upload complet (y compris l'écriture disque) démontre une excellente gestion des I/O asynchrones par .NET 8.

---

## 2. Budget de Performance Frontend (Vue.js)

Afin de garantir une expérience utilisateur fluide, les limites suivantes ont été définies pour l'application client.

### Métriques Cibles (Lighthouse / Network)

| Métrique | Budget Cible | Justification |
| :--- | :--- | :--- |
| **Bundle Size (Gzipped)** | < 350 KB | Chargement rapide sur réseau mobile (3G/4G). |
| **FCP (First Contentful Paint)** | < 1.5 s | Affichage rapide des éléments visuels. |
| **LCP (Largest Contentful Paint)** | < 2.5 s | Temps pour voir le contenu principal. |
| **CLS (Layout Shift)** | < 0.1 | Stabilité visuelle (pas d'éléments qui sautent). |

### Optimisations mises en place
*   Utilisation de **Vite** pour un bundling optimisé (Tree-shaking).
*   Chargement asynchrone des composants (Lazy Loading sur les routes Vue Router).
*   Assets CSS minifiés automatiquement en production.
