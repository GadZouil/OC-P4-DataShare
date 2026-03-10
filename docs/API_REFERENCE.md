# DataShare API Reference

> Documentation générée à partir du code source. Swagger UI disponible en développement sur http://localhost:5000/swagger

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
| `400 Bad Request` | Email déjà utilisé, mot de passe trop faible ou champs manquants |

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
| `401 Unauthorized` | Email introuvable ou mot de passe incorrect |

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
| `400 Bad Request` | Fichier absent, taille dépassée, durée invalide, type de fichier interdit (`.exe`, `.bat`, `.cmd`, `.com`, `.msi`, `.scr`, `.ps1`), mot de passe trop court |
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
| **Description** | Variante de listing des fichiers de l'utilisateur connecté (même comportement que `GET /api/files`). |
| **Auth requise** | Oui |

**Paramètres de requête :**

| Paramètre | Valeurs | Défaut | Description |
|---|---|---|---|
| `status` | `all`, `active`, `expired` | `all` | Filtre les fichiers selon leur expiration |

**Réponse succès — `200 OK` :** Même structure que `GET /api/files`.

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
| `400 Bad Request` | Fichier absent, taille dépassée, durée invalide, mot de passe trop court |
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
| `404 Not Found` | Token inconnu |
| `410 Gone` | Lien expiré |

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

**Body (JSON) — optionnel si le fichier n'est pas protégé :**
```json
{
  "password": "monMotDePasse"
}
```

**Réponse succès — `200 OK` :** Stream binaire du fichier avec les headers `Content-Type` et `Content-Disposition` appropriés.

**Codes d'erreur :**

| Code | Raison |
|---|---|
| `401 Unauthorized` | Mot de passe requis mais absent, ou mot de passe incorrect |
| `404 Not Found` | Token inconnu |
| `410 Gone` | Lien expiré |

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
