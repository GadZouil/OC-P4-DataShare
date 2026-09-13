# Sécurité — DataShare

Ce document décrit les mécanismes de sécurité **réellement implémentés** dans le prototype (avec référence au code), l'audit des dépendances et les décisions prises, ainsi que les points volontairement reportés après le MVP.

---

## 1. Gestion des accès

### Authentification (US03 / US04)

| Mécanisme | Implémentation | Fichier |
|---|---|---|
| Comptes utilisateurs | ASP.NET Core Identity (`AddIdentityCore<AppUser>`), email unique | `Program.cs` |
| Hachage des mots de passe | PBKDF2 (implémentation Identity `PasswordHasher`), jamais de mot de passe en clair en base | Identity |
| Politique de mot de passe | 8 caractères minimum (conforme aux spécifications) | `Program.cs` |
| Session | **JWT stateless** signé HMAC-SHA256, durée de vie 8 h, claims `sub` (id) et `email` | `AuthController.CreateJwt` |
| Validation du token | Émetteur, audience, signature **et** expiration vérifiés sur chaque requête | `Program.cs` (`TokenValidationParameters`) |
| Clé de signature | Lue dans la configuration (`Jwt:Key`) ; **l'API refuse de démarrer** si la clé est absente ou fait moins de 32 caractères (256 bits) — plus de clé de repli codée en dur | `Program.cs` |
| Réponse en cas d'échec de connexion | `401` identique que l'email soit inconnu ou le mot de passe faux (pas d'énumération de comptes) | `AuthController.Login` |

### Autorisation et isolation des données

- `FilesController` est décoré `[Authorize]` : sans JWT valide, toutes ses routes répondent `401`.
- L'identité vient **uniquement du token** (`User.FindFirstValue(ClaimTypes.NameIdentifier)`), jamais d'un paramètre client.
- Chaque requête privée filtre en base sur `OwnerId == userId` : un utilisateur ne peut ni lire, ni supprimer, ni lister les fichiers d'un autre. L'API répond `404` (et non `403`) pour ne pas révéler l'existence d'un fichier.
- Ces règles sont couvertes par des tests d'intégration dédiés (`SecurityTests` : lecture/suppression croisée → 404, `/files/me` isolé par utilisateur, endpoints privés → 401 sans token).
- Les liens publics reposent sur un **token de partage** distinct de l'identifiant technique : 32 octets issus de `RandomNumberGenerator` (CSPRNG), encodés Base64Url, index unique en base. Les métadonnées publiques n'exposent ni le propriétaire, ni le nom stocké, ni le token.

### Protection par mot de passe des fichiers (US09)

- Mot de passe optionnel (6 caractères min.), **haché** avec `PasswordHasher<FileItem>` (PBKDF2) — jamais stocké en clair.
- Transmis dans le **corps** d'une requête `POST .../download` (jamais dans l'URL, donc absent des logs et de l'historique navigateur).
- Mot de passe manquant ou faux → `401` avec message explicite, et ligne de log `Warning`.

---

## 2. Protection des fichiers

| Risque | Parade | Fichier |
|---|---|---|
| Path traversal (`../../etc/passwd`) | Le nom d'origine n'est **jamais** utilisé sur disque : le fichier est renommé `{GUID}{extension}` ; l'extension est tronquée si > 20 caractères | `LocalFileStorage.SaveAsync` |
| Diffusion d'exécutables | **Liste noire** d'extensions refusées à l'upload : `.exe .bat .cmd .com .msi .scr .ps1` → `400 Forbidden file type.` | `FilesController.IsForbiddenFile` |
| Saturation disque (DoS) | Taille max **1 Go** par fichier (`[RequestSizeLimit]`, `MultipartBodyLengthLimit`, `client_max_body_size` nginx) | `Program.cs`, `nginx.conf` |
| Conservation illimitée | Expiration obligatoire (1 à 7 jours) ; purge quotidienne disque + base par `ExpiredFilesCleanupService` ; liens expirés → `410 Gone` | `Services/` |
| Stockage hors de la base | Seules les métadonnées sont en PostgreSQL ; le contenu est sur disque dans `Storage/Uploads` (volume Docker) | `LocalFileStorage` |

> Choix assumé pour le MVP : une liste noire d'extensions plutôt qu'une liste blanche, pour ne pas bloquer les usages légitimes des freelances (formats variés). Une liste blanche configurable et un scan antivirus sont listés en roadmap (§ 6).

---

## 3. Sécurité applicative

- **Injection SQL** : accès aux données exclusivement via Entity Framework Core (requêtes paramétrées). Aucun SQL concaténé.
- **Validation des entrées** : double validation, côté client (UX immédiate) et côté serveur (autorité) — présence du fichier, taille, durée 1–7 jours, extension, longueur du mot de passe, normalisation des tags (trim, dédoublonnage insensible à la casse, 20 max).
- **CORS** : politique nommée `frontend`, limitée aux origines de `Cors:AllowedOrigins` (défaut `http://localhost:5173`, le serveur de dev Vite). En Docker, nginx sert le front **et** proxifie `/api` en même origine : CORS n'entre pas en jeu. L'ancienne politique `AllowAnyOrigin` a été retirée.
- **Secrets** : aucun secret réel dans le dépôt. `appsettings.json` ne contient que des valeurs `CHANGE_ME_*` ; les vraies valeurs viennent des *user-secrets* (`dotnet user-secrets`) en développement et des variables d'environnement (`.env` non versionné, voir `.env.example`) en Docker.
- **Journalisation** : chaque requête produit une ligne de log structurée (méthode, route, statut, durée, volume) ; les échecs d'authentification sur un fichier protégé sont tracés en `Warning`. Aucune donnée sensible (mot de passe, token de partage complet) n'est journalisée. Voir [PERF.md](./PERF.md) § 3.
- **Transport** : le prototype est servi en HTTP sur `localhost`. **HTTPS n'est pas activé dans le code** (`UseHttpsRedirection`/`UseHsts` absents) : en production, la terminaison TLS est prévue sur le reverse proxy nginx (certificat Let's Encrypt), ce qui est la pratique standard pour une API conteneurisée derrière un proxy. Voir § 6.

---

## 4. Audit des dépendances (SCA)

### Protocole

| Cible | Commande | Fréquence |
|---|---|---|
| NuGet (directs + transitifs) | `dotnet list package --vulnerable --include-transitive` | À chaque mise à jour de dépendance et avant chaque livraison |
| npm | `npm audit` (dans `frontend/datashare-front`) | Idem |

### Historique et décisions

| Date | Périmètre | Constat | Décision |
|---|---|---|---|
| 31/03/2026 | npm | `qs` (low, bypass `arrayLimit`) et `systeminformation` (high, injection de commande) — dépendances **transitives de Cypress**, jamais déployées | Corrigées par `npm audit fix` (risque réel nul en production, correction immédiate car sans coût) |
| 02/08/2026 | NuGet | `Microsoft.OpenApi` 2.3.12 — [CVE-2026-49451](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc), stack overflow (DoS) au parsing d'un document OpenAPI à référence circulaire | Risque faible (la lib ne fait que *générer* Swagger) mais **mise à jour vers 2.7.5** : mineure, sans rupture, tests verts |
| 02/08/2026 | npm | 16 avis (1 critique, 12 élevés) sur des dépendances transitives (`axios`, `vite`, `rollup`, `postcss`, `eslint`) | Suppression de `react-router-dom` (dépendance morte : le routing est `vue-router`) → 14 avis éliminés ; `npm audit fix` pour le reste ; correction TypeScript d'une ligne suite au durcissement d'`axios` ; `npm run build` vert |
| 13/09/2026 | npm (front) | 4 avis (2 élevés : `js-yaml`, `nanoid` ; 1 modéré : `@humanfs/node` ; 1 faible : `postcss-selector-parser`) — tous sur des dépendances **transitives d'outils de build/lint** (Vite, ESLint, devtools), rien dans le bundle livré | `npm audit fix` (mises à jour de patch), `npm run build` + `npm run lint` verts |
| 13/09/2026 | npm (Cypress) | 9 avis (6 élevés : `form-data`, `lodash`, `qs`, `tmp`, `uuid`… ; 3 modérés dont `systeminformation`) — dépendances de **Cypress uniquement** (outil de test, jamais déployé) | `npm audit fix` : Cypress 15.10 → 15.21.1, 0 vulnérabilité |

**Statut au 13/09/2026 : 0 vulnérabilité connue (frontend et outillage de test) ; backend : 0 au dernier audit du 02/08/2026.**

- `dotnet list package --vulnerable --include-transitive` : aucun package vulnérable signalé pour `DataShare.Api` et `DataShare.Api.Tests` (02/08/2026).
- `npm audit` (`frontend/datashare-front` et `frontend`) : `found 0 vulnerabilities` (13/09/2026).

> À relancer avant la soutenance (les bases d'avis évoluent) : voir la section « Audit » du script `scripts/verify.ps1`.

> Cas d'école conservé volontairement dans l'historique Git : le commit `8f5e793 fix(security): patch vulnerable dependencies` montre le traitement complet d'une alerte (analyse du risque réel, correctif, non-régression).

---

## 5. Procédure en cas de vulnérabilité

1. **Qualifier** : la dépendance est-elle utilisée à l'exécution (production) ou seulement en développement/test ? Le code vulnérable est-il atteignable dans DataShare ?
2. **Corriger** : `dotnet add package <Nom> --version <version corrigée>` ou `npm audit fix` (jamais `--force` sans revue, voir [MAINTENANCE.md](./MAINTENANCE.md)).
3. **Vérifier** : `dotnet test` + `npm run build` + scénario Cypress critique.
4. **Documenter** la décision (corrigée / acceptée / ignorée et pourquoi) dans le tableau du § 4.
5. Si un **secret** a pu fuiter : rotation immédiate de `Jwt:Key` (invalide tous les tokens en cours) et du mot de passe PostgreSQL.

---

## 6. Roadmap sécurité (hors périmètre MVP)

| Amélioration | Intérêt | Piste technique |
|---|---|---|
| **HTTPS / HSTS** | Confidentialité des JWT et mots de passe en transit hors localhost | Terminaison TLS nginx + `UseHttpsRedirection()`/`UseHsts()` derrière `ForwardedHeaders` |
| **Rate limiting** sur `/auth/login` et `/public/files/{token}/download` | Freiner la force brute sur les mots de passe | Middleware natif `AddRateLimiter` (.NET 7+), politique par IP |
| **Refresh tokens** | Réduire la durée de vie des JWT (8 h aujourd'hui) sans dégrader l'UX | Table de refresh tokens révocables |
| **Liste blanche d'extensions / vérification MIME réelle** | Renforcer le filtrage des fichiers | Détection par signature (magic bytes) |
| **Scan antivirus des uploads** | Éviter la diffusion de malwares via la plateforme | ClamAV (`nClam`) en tâche de fond avant activation du lien |
| **En-têtes de sécurité** (CSP, `X-Content-Type-Options`, `Referrer-Policy`) | Réduire la surface XSS côté front | Configuration nginx |
| **Chiffrement au repos** | Protéger les fichiers si le disque est compromis | Chiffrement côté stockage (implémentation `IFileStorage` dédiée) |
