# Documentation des Tests - DataShare API

Ce document détaille la stratégie de test, le plan de validation et les résultats de la couverture de code pour l'API Backend de DataShare.

## 1. Stratégie de Test

L'approche qualité repose sur la pyramide des tests, privilégiant une base solide de tests unitaires et d'intégration pour garantir la robustesse du backend .NET.

### Typologie des tests
*   **Tests Unitaires (Unit Tests) :** Validation de la logique métier isolée (Services) en utilisant des Mocks (Moq) pour s'abstraire de la base de données et du système de fichiers.
*   **Tests d'Intégration (Integration Tests) :** Validation des endpoints de l'API (Controllers) dans un environnement complet via `WebApplicationFactory` et une base de données en mémoire (InMemory Database).
*   **Analyse Statique :** Vérification de la syntaxe et des bonnes pratiques lors de la compilation.

### Stack Technique
*   **Framework :** xUnit
*   **Assertions :** FluentAssertions (pour la lisibilité)
*   **Mocking :** Moq
*   **Couverture :** Coverlet / ReportGenerator
*   **Client HTTP de test :** `Microsoft.AspNetCore.Mvc.Testing`

---

## 2. Plan de Tests (Couverture Fonctionnelle)

Le tableau suivant synthétise les scénarios validés, répartis entre cas nominaux (fonctionnement normal) et gestion des erreurs.

| Module | Fonctionnalité | Type de Scénario | Résultat Attendu | Critères d'acceptation |
| :--- | :--- | :--- | :--- | :--- |
| **Auth** | Inscription | **Nominal** : Création compte valide | 200 OK + Token JWT | Token JWT valide retourné, durée de vie > 0, compte créé en base |
| **Auth** | Connexion | **Nominal** : Login email/password valides | 200 OK + Token JWT | Token JWT valide retourné, durée de vie > 0 |
| **Auth** | Connexion | **Erreur** : Mot de passe incorrect | 401 Unauthorized | Réponse 401 sans token, message d'erreur présent |
| **Auth** | Connexion | **Erreur** : Email inexistant | 401 Unauthorized (ou 404 selon config) | Réponse 401/404 sans token, aucune information sur l'existence du compte |
| **Files** | Upload Privé | **Nominal** : Fichier valide (jpg/png/txt) | 201 Created + Métadonnées JSON | Fichier accessible via le lien retourné, taille identique à l'original |
| **Files** | Upload Privé | **Erreur** : Aucun fichier envoyé | 400 Bad Request | Réponse 400 avec message d'erreur explicite, aucun fichier créé |
| **Files** | Upload Privé | **Erreur** : Fichier trop volumineux | 400 Bad Request / 413 Payload Too Large | Réponse 400/413 avec indication de la limite, aucun fichier créé |
| **Files** | Listing | **Nominal** : Récupérer ses fichiers | 200 OK + Liste JSON | Liste contient uniquement les fichiers de l'utilisateur authentifié |
| **Files** | Download | **Nominal** : Télécharger son fichier | 200 OK + FileStream | Contenu binaire identique à l'original, en-tête `Content-Disposition: attachment` présent |
| **Files** | Download | **Erreur** : ID de fichier inexistant | 404 Not Found | Réponse 404, aucun contenu binaire retourné |
| **Files** | Suppression | **Nominal** : Supprimer son fichier | 204 No Content | GET sur le lien retourne 404 après suppression, fichier physique absent |
| **Files** | Suppression | **Erreur** : Supprimer fichier introuvable | 404 Not Found | Réponse 404 avec message d'erreur, aucun effet de bord |
| **Public** | Upload Anonyme | **Nominal** : Transfert sans compte | 201 Created + URL de partage | URL de partage fonctionnelle, fichier téléchargeable sans authentification |
| **Public** | Download Anonyme | **Nominal** : Accès via lien public | 200 OK + FileStream | Contenu binaire identique à l'original, accessible sans token |
| **Core** | Stockage Local | **Unitaire** : Écriture sur disque | Le fichier physique est créé dans `Uploads/` | Fichier présent sur le disque, taille non nulle, chemin conforme |
| **Core** | Stockage Local | **Unitaire** : Lecture du disque | Le flux binaire est correctement ouvert | Flux non nul, lecture complète sans exception |

---

## 3. Exécution des Tests

### Prérequis
*   .NET 8 SDK installé
*   Docker (optionnel, si tests PostgreSQL réels)

### Commandes
Pour lancer l'ensemble de la suite de tests :
```bash
dotnet test
```

### 1. Exécution avec collecte
```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### 2. Génération du rapport HTML
```bash
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### Frontend (Cypress E2E)

Lancer les tests en mode interactif (interface graphique) :
```bash
cd frontend/datashare-front
npx cypress open
```

Lancer en mode headless (CI/CD) :
```bash
cd frontend/datashare-front
npx cypress run
```

---

## 4. Couverture de Code

### Backend
La couverture est mesurée via **Coverlet** intégré à `dotnet test`. Le seuil cible est **≥ 80 %** sur les lignes du code métier (hors migrations EF Core).

| Couche | Couverture estimée |
| :--- | :--- |
| Services (`FileService`, `AuthService`) | ~85 % |
| Controllers (endpoints API) | ~80 % |
| Infrastructure (`LocalStorageService`) | ~75 % |

> Pour obtenir le rapport exact, exécuter les commandes de la section **Exécution des Tests** ci-dessus.

Le rapport HTML complet est disponible dans `backend/DataShare.Api.Tests/coverage-report/index.html`. Résultat actuel : **81.2% de couverture en lignes, 60% en branches**.

### Frontend
Le frontend Vue 3 n'est pas soumis à une mesure de couverture unitaire. La couverture fonctionnelle est assurée par les **tests E2E Cypress** qui couvrent les flux critiques (upload, suppression, téléchargement, tags, protection par mot de passe).

| Fichier | Scénario | Critères d'acceptation |
| :--- | :--- | :--- |
| `file-lifecycle.cy.ts` | Upload, téléchargement, suppression complète | Fichier uploadé → lien fonctionnel → suppression → lien invalide |
| `tags.cy.ts` | Ajout et filtrage par tags | Tags assignés visibles, filtre retourne les bons fichiers |
| `password-protection.cy.ts` | Protection par mot de passe | Accès refusé sans mdp, accès autorisé avec bon mdp |
| `limits.cy.ts` | Limites de taille/type de fichier | Rejet des fichiers hors limites avec message d'erreur |
| `full-scenario.cy.ts` | Parcours utilisateur complet | Inscription → connexion → upload → partage → suppression |

---

## 5. Tests Manuels Complémentaires

Les tests automatisés couvrent les flux principaux. Les scénarios suivants ont été vérifiés manuellement en environnement réel :

| Scénario | Navigateur(s) | Résultat |
| :--- | :--- | :--- |
| Upload d'un fichier volumineux (~50 Mo) | Chrome, Firefox | Progression visible, upload réussi |
| Téléchargement via lien public (multi-navigateur) | Chrome, Firefox, Edge | Fichier reçu intact, nom préservé |
| Interface responsive sur mobile (375 px) | Chrome DevTools | Formulaire accessible, boutons utilisables |
| Upload simultané de plusieurs fichiers | Chrome | Chaque fichier traité indépendamment |
| Session expirée : accès direct à `/me` | Chrome | Redirection automatique vers `/login` |
| Lien public protégé par mot de passe (via UI) | Chrome | Saisie du mot de passe demandée avant téléchargement |
| Vérification des en-têtes HTTP au téléchargement | DevTools → Network | `Content-Disposition: attachment` présent |
| Ajout d'un tag en doublon (case-insensitive) | Chrome | Doublon ignoré, normalisation côté UI |
