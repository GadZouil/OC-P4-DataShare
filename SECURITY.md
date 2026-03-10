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

---

## 5. Validation des Entrées

*   **Taille des fichiers :** La taille maximale des uploads est limitée côté backend (configuration Kestrel / `IFormFile`) pour prévenir les attaques par saturation (DoS via gros fichiers).
*   **Types autorisés :** Une liste blanche d'extensions est appliquée avant tout traitement. Toute extension non reconnue est rejetée avec un code `400 Bad Request`. Les exécutables (`.exe`, `.sh`, `.bat`, `.ps1`) sont explicitement interdits.
*   **Sanitization des noms de fichiers :** Le nom d'origine du fichier fourni par le client n'est jamais conservé sur le disque. Il est remplacé par un `GUID` généré aléatoirement, éliminant tout risque de Path Traversal (`../../etc/passwd`) ou d'injection via le nom de fichier.

---

## 6. CORS (Cross-Origin Resource Sharing)

*   **Configuration actuelle :** La politique CORS est déclarée dans `Program.cs` via `builder.Services.AddCors()` et activée via `app.UseCors()`.
*   **Domaines autorisés :** Seul le domaine du frontend officiel est listé dans `WithOrigins(...)`. Toute origine inconnue est bloquée par défaut par le navigateur.
*   **En développement :** L'origine `http://localhost:[PORT]` est autorisée pour faciliter le développement local. Cette configuration ne doit pas être déployée en production.

---

## 7. HTTPS

*   **En développement :** L'API tourne en HTTP local (`http://localhost:[PORT]`). Aucun certificat SSL n'est requis dans cet environnement.
*   **En production :** HTTPS est obligatoire. L'API active `UseHttpsRedirection()` pour rediriger automatiquement tout trafic HTTP vers HTTPS, et `UseHsts()` pour envoyer l'en-tête `Strict-Transport-Security` et prévenir les attaques Man-in-the-Middle (MITM).

---

## 8. Améliorations Futures (Roadmap Sécurité)

*   **Rate Limiting :** Implémenter une limitation du nombre de requêtes par IP (ex : middleware `AspNetCoreRateLimit`) pour prévenir les attaques par force brute sur les endpoints d'authentification.
*   **Refresh Tokens :** Introduire un système de refresh tokens pour permettre des access tokens JWT de courte durée de vie, réduisant la fenêtre d'exposition en cas de vol de token.
*   **CSP Headers (Content Security Policy) :** Ajouter des en-têtes `Content-Security-Policy` côté frontend pour restreindre les sources de scripts et prévenir les attaques XSS.
*   **Scan Antivirus des Uploads :** Intégrer un moteur antivirus (ex: ClamAV via `nClam`) pour analyser automatiquement les fichiers uploadés avant de les stocker, afin de prévenir la distribution de malwares via la plateforme.
