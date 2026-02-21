# Rapport de Sécurité - DataShare API

Ce document détaille l'audit de sécurité des dépendances, la politique de gestion des vulnérabilités et les mécanismes de défense implémentés dans l'architecture backend.

## 1. Audit Automatisé des Dépendances (SCA)

Nous utilisons l'analyseur de vulnérabilités natif de .NET pour scanner la chaîne d'approvisionnement logicielle (Supply Chain).

### Protocole d'audit
*   **Cible :** Packages NuGet (Directs et Transitifs)
*   **Base de données :** GitHub Advisory Database & NuGet.org
*   **Fréquence :** À chaque build majeur et avant mise en production.
*   **Date du dernier audit :** 10/02/2026

### Résultat de l'analyse
> Commande : `dotnet list package --vulnerable --include-transitive`

✅ **Statut : PAS DE VULNÉRABILITÉ DÉTECTÉE**

Le projet est sain. Aucun package obsolète critique, haut ou modéré n'a été détecté à ce jour.

---

## 2. Architecture de Sécurité (Defense in Depth)

Le backend DataShare n'attend pas que les failles soient trouvées ; il les prévient par design.

### A. Authentification & Identité
*   **Stateless Authentication :** Utilisation exclusive de JWT (JSON Web Tokens). Le serveur ne stocke pas de session, réduisant la surface d'attaque (Session Hijacking).
*   **Hachage Robuste :** Les mots de passe sont hachés via **PBKDF2** (implémentation standard ASP.NET Core Identity), rendant les attaques par Rainbow Table impossibles.
*   **Principe de Moindre Privilège :** Les contrôleurs sont verrouillés par défaut via `[Authorize]`. Seuls les endpoints publics explicites (Login/Register) sont ouverts.

### B. Protection des Données (Fichiers)
*   **Sanitization des Noms :** Aucun fichier n'est stocké avec son nom d'origine. Ils sont renommés avec un `GUID` pour empêcher les attaques de type **Path Traversal** (ex: `../../etc/passwd`).
*   **Liste Blanche (Allowlist) :** Seules les extensions strictement nécessaires sont autorisées. Les exécutables (`.exe`, `.sh`, `.bat`) sont rejetés au niveau du code métier.
*   **Isolation Logique :** Une validation stricte (`User.Identity.Name`) assure qu'un utilisateur ne peut accéder qu'aux fichiers dont il est propriétaire en base de données.

### C. Sécurité de l'Infrastructure (Code Level)
*   **Anti-Injection SQL :** Utilisation stricte d'Entity Framework Core (ORM) qui paramètre toutes les requêtes, neutralisant les injections SQL classiques.
*   **CORS Restrictif :** La politique Cross-Origin est configurée pour n'accepter que les requêtes provenant du Frontend officiel (à paramétrer selon l'environnement).

---

## 3. Gestion des Secrets et Configuration

Pour éviter les fuites de données sensibles (Hardcoded Secrets) :

1.  **En Développement :** Utilisation de l'outil **User Secrets** de .NET (`secrets.json`). Aucune clé (JWT Key, Connection String) n'est commise dans le dépôt Git.
2.  **En Production :** Les secrets sont injectés via des **Variables d'Environnement** sécurisées.
3.  **HTTPS :** L'API force la redirection HTTPS et utilise HSTS (HTTP Strict Transport Security) pour prévenir les attaques Man-in-the-Middle.

---

## 4. Plan d'Action en cas de faille
Si une vulnérabilité est découverte :
1.  Isolation du serveur concerné.
2.  Patch du package via NuGet (`dotnet add package [Nom] --version [SafeVersion]`).
3.  Rotation immédiate de la clé de signature JWT (`Jwt:Key`).
