# Diagrammes Mermaid — DataShare

Versions Mermaid des diagrammes du projet (sources draw.io/PlantUML dans `docs/diagrams/`).
Utilisables directement dans Gamma, GitHub, ou exportables en PNG via https://mermaid.live.

## Architecture globale

```mermaid
flowchart LR
    subgraph Client
        B[Navigateur]
    end

    subgraph Docker["Docker Compose"]
        subgraph FRONT["frontend (nginx, port 80)"]
            SPA[SPA Vue 3 + TypeScript]
            PROXY["Proxy /api"]
        end
        subgraph API["api (ASP.NET Core 9, port 5000)"]
            CTRL["Controllers<br/>Auth / Files / PublicFiles"]
            SVC["Services<br/>IFileStorage / ExpiredFilesCleanup"]
            EF["EF Core + Identity"]
        end
        DB[("PostgreSQL 16<br/>port 5433")]
        VOL[/"Volume fichiers<br/>Storage/Uploads (GUID)"/]
    end

    B -->|HTTP :80| SPA
    B -->|"/api/* (JWT Bearer)"| PROXY
    PROXY --> CTRL
    CTRL --> SVC
    CTRL --> EF
    EF --> DB
    SVC --> VOL
```

## Modèle de données (MCD)

```mermaid
erDiagram
    AppUser ||--o{ FileItem : "possede (0,N)"

    AppUser {
        Guid Id PK
        string Email UK
        string PasswordHash
    }

    FileItem {
        Guid Id PK
        Guid OwnerId FK "nullable (upload anonyme)"
        string OriginalFileName "max 255"
        string StoredFileName "GUID + extension"
        string ContentType
        long SizeBytes
        string Token UK "32 octets aleatoires, Base64Url"
        DateTimeOffset CreatedAt
        DateTimeOffset ExpiresAt "1 a 7 jours"
        string PasswordHash "nullable, PBKDF2"
        string_array Tags "text[] PostgreSQL"
    }
```

## Flux d'authentification (JWT)

```mermaid
sequenceDiagram
    participant C as Navigateur (Vue)
    participant A as API ASP.NET Core
    participant I as Identity (PBKDF2)
    participant D as PostgreSQL

    C->>A: POST /api/auth/register {email, password}
    A->>I: Hachage du mot de passe
    I->>D: INSERT AspNetUsers
    A-->>C: 200 + JWT

    C->>A: POST /api/auth/login {email, password}
    A->>D: SELECT utilisateur par email
    A->>I: Verification du hash
    A-->>C: 200 + JWT (signe HS256)

    C->>A: GET /api/files/me (Authorization: Bearer JWT)
    A->>A: Validation signature + expiration
    A->>D: SELECT ... WHERE OwnerId = userId (extrait du JWT)
    A-->>C: 200 + fichiers de l'utilisateur uniquement
```

## Flux upload et partage

```mermaid
sequenceDiagram
    participant U as Emetteur (connecte)
    participant A as API
    participant S as Stockage local
    participant D as PostgreSQL
    participant R as Destinataire (lien)

    U->>A: POST /api/files (multipart: fichier, expiration, mdp?, tags?)
    A->>A: Validations (taille <= 1 Go, extension, expiration 1-7j, mdp >= 6)
    A->>A: Generation token CSPRNG 32 octets
    A->>S: Ecriture sous nom GUID
    A->>D: INSERT FileItem (metadonnees + hash mdp)
    A-->>U: 201 + token -> lien /download/{token}

    R->>A: GET /api/public/files/{token}
    A-->>R: Metadonnees (nom, taille, expiration, mdp requis ?)
    R->>A: POST /api/public/files/{token}/download {password?}
    A->>A: Verif expiration (410 si expire) + mdp (401 si faux)
    A->>S: Lecture du fichier
    A-->>R: 200 FileStream (Content-Disposition: attachment)
```
