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

| Module | Fonctionnalité | Type de Scénario | Résultat Attendu |
| :--- | :--- | :--- | :--- |
| **Auth** | Inscription | **Nominal** : Création compte valide | 200 OK + Token JWT |
| **Auth** | Connexion | **Nominal** : Login email/password valides | 200 OK + Token JWT |
| **Auth** | Connexion | **Erreur** : Mot de passe incorrect | 401 Unauthorized |
| **Auth** | Connexion | **Erreur** : Email inexistant | 401 Unauthorized (ou 404 selon config) |
| **Files** | Upload Privé | **Nominal** : Fichier valide (jpg/png/txt) | 201 Created + Métadonnées JSON |
| **Files** | Upload Privé | **Erreur** : Aucun fichier envoyé | 400 Bad Request |
| **Files** | Upload Privé | **Erreur** : Fichier trop volumineux | 400 Bad Request / 413 Payload Too Large |
| **Files** | Listing | **Nominal** : Récupérer ses fichiers | 200 OK + Liste JSON |
| **Files** | Download | **Nominal** : Télécharger son fichier | 200 OK + FileStream |
| **Files** | Download | **Erreur** : ID de fichier inexistant | 404 Not Found |
| **Files** | Suppression | **Nominal** : Supprimer son fichier | 204 No Content |
| **Files** | Suppression | **Erreur** : Supprimer fichier introuvable | 404 Not Found |
| **Public** | Upload Anonyme | **Nominal** : Transfert sans compte | 201 Created + URL de partage |
| **Public** | Download Anonyme | **Nominal** : Accès via lien public | 200 OK + FileStream |
| **Core** | Stockage Local | **Unitaire** : Écriture sur disque | Le fichier physique est créé dans `Uploads/` |
| **Core** | Stockage Local | **Unitaire** : Lecture du disque | Le flux binaire est correctement ouvert |

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

### Frontend
Le frontend Vue 3 n'est pas soumis à une mesure de couverture unitaire. La couverture fonctionnelle est assurée par les **tests E2E Cypress** qui couvrent les flux critiques (upload, suppression, téléchargement, tags, protection par mot de passe).

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
