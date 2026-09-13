# DataShare API Reference

> Documentation rédigée à partir du code source (`backend/DataShare.Api/Controllers`). Contrat machine : [`openapi.yaml`](./openapi.yaml). Swagger UI disponible lorsque l'API tourne en environnement *Development* (`dotnet run`) sur http://localhost:5180/swagger.

---

## Authentification

Tous les endpoints marqués **Oui** dans la colonne "Auth requise" attendent un header HTTP :

```
Authorization: Bearer <token>
```

Le token JWT est obtenu via `POST /api/auth/register` ou `POST /api/auth/login`. Il expire après **8 heures**.

---

## 1. Auth — `/api/auth`

### POST `/api/auth/register`

| Champ | Valeur |
|---|---|
| **Méthode** | `POST` |
| **Route** | `/api/auth/register` |
| **Description** | Crée un nouveau compte utilisateur et retourne un token JWT. |
| **Auth requise** | Non |

**Body (JSON) :**
```json
{
  "email": "alice@example.com",
  "password": "MonMotDePasse1!"
}
```

**Réponse succès — `200 OK` :**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `400 Bad Request` | Email déjà utilisé (`DuplicateUserName`) ou mot de passe de moins de 8 caractères (`PasswordTooShort`) : corps JSON = tableau d'erreurs Identity `[{ "code": "PasswordTooShort", "description": "Passwords must be at least 8 characters." }]`. Champs manquants : `ValidationProblemDetails` |

---

### POST `/api/auth/login`

| Champ | Valeur |
|---|---|
| **Méthode** | `POST` |
| **Route** | `/api/auth/login` |
| **Description** | Authentifie un utilisateur existant et retourne un token JWT. |
| **Auth requise** | Non |

**Body (JSON) :**
```json
{
  "email": "alice@example.com",
  "password": "MonMotDePasse1!"
}
```

**Réponse succès — `200 OK` :**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `401 Unauthorized` | Email introuvable ou mot de passe incorrect (corps `ProblemDetails`, réponse identique dans les deux cas pour ne pas révéler l'existence du compte) |

---

## 2. Files (authentifié) — `/api/files`

Ces endpoints nécessitent un token JWT valide.

### POST `/api/files`

| Champ | Valeur |
|---|---|
| **Méthode** | `POST` |
| **Route** | `/api/files` |
| **Description** | Upload un fichier pour l'utilisateur connecté. Génère un token de partage unique. |
| **Auth requise** | Oui |

**Body (`multipart/form-data`) :**

| Champ | Type | Obligatoire | Description |
|---|---|---|---|
| `file` | `binary` | Oui | Fichier à uploader (max 1 Go) |
| `expiresInDays` | `integer` | Non | Durée de validité en jours (1–7, défaut : 7) |
| `password` | `string` | Non | Mot de passe de protection (min 6 caractères) |
| `tags` | `string[]` | Non | Tableau de tags (max 20, dédupliqués) |

**Réponse succès — `201 Created` :**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "originalFileName": "rapport.pdf",
  "sizeBytes": 204800,
  "contentType": "application/pdf",
  "createdAt": "2026-03-10T14:00:00Z",
  "expiresAt": "2026-03-17T14:00:00Z",
  "token": "abc123XYZ_urlsafe_base64",
  "passwordRequired": false,
  "tags": ["projet", "2026"]
}
```

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `400 Bad Request` | Corps texte brut (`text/plain`), un message parmi : `File is required.`, `File exceeds 1 GB.`, `ExpiresInDays must be between 1 and 7.`, `Forbidden file type.` (`.exe`, `.bat`, `.cmd`, `.com`, `.msi`, `.scr`, `.ps1`), `Password must be at least 6 characters.` |
| `401 Unauthorized` | Token JWT absent ou invalide |

---

### GET `/api/files`

| Champ | Valeur |
|---|---|
| **Méthode** | `GET` |
| **Route** | `/api/files?status=all\|active\|expired` |
| **Description** | Liste tous les fichiers de l'utilisateur connecté, avec filtre optionnel par statut. |
| **Auth requise** | Oui |

**Paramètres de requête :**

| Paramètre | Valeurs | Défaut | Description |
|---|---|---|---|
| `status` | `all`, `active`, `expired` | `all` | Filtre les fichiers selon leur expiration |

**Réponse succès — `200 OK` :**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "originalFileName": "rapport.pdf",
    "sizeBytes": 204800,
    "contentType": "application/pdf",
    "createdAt": "2026-03-10T14:00:00Z",
    "expiresAt": "2026-03-17T14:00:00Z",
    "token": "abc123XYZ_urlsafe_base64",
    "passwordRequired": false,
    "tags": ["projet"],
    "isExpired": false
  }
]
```

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `401 Unauthorized` | Token JWT absent ou invalide |

---

### GET `/api/files/me`

| Champ | Valeur |
|---|---|
| **Méthode** | `GET` |
| **Route** | `/api/files/me?status=all\|active\|expired` |
| **Description** | Variante de listing des fichiers de l'utilisateur connecté, utilisée par le front « Mon espace » (même filtre `status`, mais sans le champ `isExpired`). |
| **Auth requise** | Oui |

**Paramètres de requête :**

| Paramètre | Valeurs | Défaut | Description |
|---|---|---|---|
| `status` | `all`, `active`, `expired` | `all` | Filtre les fichiers selon leur expiration |

**Réponse succès — `200 OK` :** Même structure que `GET /api/files`, **sans** le champ `isExpired` (le front déduit l'expiration de `expiresAt`).

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `401 Unauthorized` | Token JWT absent ou invalide |

---

### GET `/api/files/{id}`

| Champ | Valeur |
|---|---|
| **Méthode** | `GET` |
| **Route** | `/api/files/{id}` |
| **Description** | Récupère les métadonnées d'un fichier appartenant à l'utilisateur connecté. |
| **Auth requise** | Oui |

**Paramètres de chemin :**

| Paramètre | Type | Description |
|---|---|---|
| `id` | `uuid` | Identifiant du fichier |

**Réponse succès — `200 OK` :**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "originalFileName": "rapport.pdf",
  "sizeBytes": 204800,
  "contentType": "application/pdf",
  "createdAt": "2026-03-10T14:00:00Z",
  "expiresAt": "2026-03-17T14:00:00Z",
  "token": "abc123XYZ_urlsafe_base64",
  "passwordRequired": false,
  "tags": ["projet"]
}
```

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `401 Unauthorized` | Token JWT absent ou invalide |
| `404 Not Found` | Fichier introuvable ou n'appartient pas à l'utilisateur |

---

### DELETE `/api/files/{id}`

| Champ | Valeur |
|---|---|
| **Méthode** | `DELETE` |
| **Route** | `/api/files/{id}` |
| **Description** | Supprime définitivement un fichier (stockage disque + base de données). |
| **Auth requise** | Oui |

**Paramètres de chemin :**

| Paramètre | Type | Description |
|---|---|---|
| `id` | `uuid` | Identifiant du fichier à supprimer |

**Réponse succès — `204 No Content` :** Corps vide.

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `401 Unauthorized` | Token JWT absent ou invalide |
| `404 Not Found` | Fichier introuvable ou n'appartient pas à l'utilisateur |

---

## 3. Liens de partage publics — `/api/public/files`

Ces endpoints sont accessibles sans authentification via un token de partage opaque.

### POST `/api/public/files`

| Champ | Valeur |
|---|---|
| **Méthode** | `POST` |
| **Route** | `/api/public/files` |
| **Description** | Upload anonyme d'un fichier (sans compte). Réservé aux utilisateurs non authentifiés. |
| **Auth requise** | Non (interdit si authentifié) |

**Body (`multipart/form-data`) :**

| Champ | Type | Obligatoire | Description |
|---|---|---|---|
| `file` | `binary` | Oui | Fichier à uploader (max 1 Go) |
| `expiresInDays` | `integer` | Non | Durée de validité en jours (1–7, défaut : 7) |
| `password` | `string` | Non | Mot de passe de protection (min 6 caractères) |
| `tags` | `string[]` | Non | Tableau de tags (max 20, dédupliqués) |

**Réponse succès — `201 Created` :**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "originalFileName": "photo.jpg",
  "sizeBytes": 512000,
  "contentType": "image/jpeg",
  "createdAt": "2026-03-10T14:00:00Z",
  "expiresAt": "2026-03-17T14:00:00Z",
  "token": "xyz789_urlsafe_base64",
  "passwordRequired": false,
  "tags": []
}
```

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `400 Bad Request` | Corps texte brut, mêmes messages que `POST /api/files` : `File is required.`, `File exceeds 1 GB.`, `ExpiresInDays must be between 1 and 7.`, `Forbidden file type.`, `Password must be at least 6 characters.` |
| `403 Forbidden` | L'utilisateur est déjà authentifié |

---

### GET `/api/public/files/{token}`

| Champ | Valeur |
|---|---|
| **Méthode** | `GET` |
| **Route** | `/api/public/files/{token}` |
| **Description** | Récupère les métadonnées publiques d'un fichier via son token de partage. Indique si un mot de passe est requis pour le téléchargement. |
| **Auth requise** | Non |

**Paramètres de chemin :**

| Paramètre | Type | Description |
|---|---|---|
| `token` | `string` | Token de partage opaque (Base64Url, 32 octets) |

**Réponse succès — `200 OK` :**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "originalFileName": "photo.jpg",
  "sizeBytes": 512000,
  "contentType": "image/jpeg",
  "createdAt": "2026-03-10T14:00:00Z",
  "expiresAt": "2026-03-17T14:00:00Z",
  "passwordRequired": true
}
```

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `404 Not Found` | Token inconnu (corps `ProblemDetails`) |
| `410 Gone` | Lien expiré — corps `{ "message": "Link expired." }` |

---

### POST `/api/public/files/{token}/download`

| Champ | Valeur |
|---|---|
| **Méthode** | `POST` |
| **Route** | `/api/public/files/{token}/download` |
| **Description** | Télécharge le fichier associé au token. Si le fichier est protégé par mot de passe, celui-ci doit être fourni dans le body. |
| **Auth requise** | Non |

**Paramètres de chemin :**

| Paramètre | Type | Description |
|---|---|---|
| `token` | `string` | Token de partage opaque |

**Body (JSON) — obligatoire (au minimum `{}` ou `{ "password": null }`) ; `password` n'est requis que si le fichier est protégé :**
```json
{
  "password": "monMotDePasse"
}
```

**Réponse succès — `200 OK` :** Stream binaire du fichier. `Content-Type` = type MIME enregistré à l'upload (`application/octet-stream` si inconnu), `Content-Disposition: attachment; filename=<nom d'origine>`.

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `401 Unauthorized` | Mot de passe requis mais absent (`{ "message": "Password required." }`) ou incorrect (`{ "message": "Invalid password." }`) |
| `404 Not Found` | Token inconnu ou fichier supprimé (corps `ProblemDetails`) |
| `410 Gone` | Lien expiré (`{ "message": "Link expired." }`) |

---

## 4. System — `/api/health`

### GET `/api/health`

| Champ | Valeur |
|---|---|
| **Méthode** | `GET` |
| **Route** | `/api/health` |
| **Description** | État de l'API, utilisé par le healthcheck Docker Compose et les scripts de déploiement (défini dans `Program.cs`). |
| **Auth requise** | Non |

**Réponse succès — `200 OK` :**
```json
{ "status": "ok" }
```

---

## Résumé des endpoints

| Méthode | Route | Description | Auth requise |
|---|---|---|---|
| `POST` | `/api/auth/register` | Inscription | Non |
| `POST` | `/api/auth/login` | Connexion | Non |
| `POST` | `/api/files` | Upload authentifié | Oui |
| `GET` | `/api/files` | Lister ses fichiers | Oui |
| `GET` | `/api/files/me` | Lister ses fichiers (variante) | Oui |
| `GET` | `/api/files/{id}` | Détail d'un fichier | Oui |
| `DELETE` | `/api/files/{id}` | Supprimer un fichier | Oui |
| `POST` | `/api/public/files` | Upload anonyme | Non (interdit si connecté) |
| `GET` | `/api/public/files/{token}` | Métadonnées via lien | Non |
| `POST` | `/api/public/files/{token}/download` | Télécharger via lien | Non |
| `GET` | `/api/health` | État de l'API (healthcheck) | Non |
