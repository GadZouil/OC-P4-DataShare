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

## 2. Plan de Tests (Fonctionnalités Critiques)

Le tableau suivant liste les scénarios critiques validés par la suite de tests automatisée.

| Fonctionnalité | Type de Test | Cas de Test (Scénario) | Critères d'Acceptation |
| :--- | :--- | :--- | :--- |
| **Authentification** | Intégration | Inscription nouvel utilisateur | Retour 200 OK + JWT Token généré |
| **Authentification** | Intégration | Login avec mauvais mot de passe | Retour 401 Unauthorized |
| **Upload Fichier** | Intégration | Upload utilisateur authentifié | Fichier stocké, Entrée BDD créée, Retour 201 Created |
| **Upload Fichier** | Intégration | Upload extension interdite (.exe) | Fichier rejeté, Retour 400 Bad Request |
| **Upload Public** | Intégration | Upload sans compte (Anonyme) | Fichier stocké, Token de partage généré |
| **Téléchargement** | Intégration | Téléchargement fichier existant | Flux binaire reçu, Content-Type correct |
| **Nettoyage** | Unitaire | Suppression fichiers expirés | Le service supprime le fichier physique et l'entrée BDD |
| **Sécurité** | Unitaire | Accès ressource d'un autre user | Retour 403 Forbidden ou 404 (selon config) |

---

## 3. Exécution des Tests

### Prérequis
*   .NET 9 SDK installé
*   Docker (optionnel, si tests SQL Server réels)

### Commandes
Pour lancer l'ensemble de la suite de tests :
```bash
dotnet test
```

# 1. Exécution avec collecte
```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

# 2. Génération du rapport HTML
```bash
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```
