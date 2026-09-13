# Performance — DataShare

Budget de performance, résultats des tests de charge (k6) et d'audit navigateur (Lighthouse), journalisation structurée des métriques clés et pistes d'optimisation.

---

## 1. Budget de performance

| Indicateur | Cible | Pourquoi |
|---|---|---|
| **API — P95 upload 100 Ko** (`POST /api/files`) | < 500 ms | Ressenti « instantané » pour un fichier courant |
| **API — P95 connexion** (`POST /api/auth/login`) | < 500 ms | Le hachage PBKDF2 est volontairement coûteux ; il ne doit pas devenir un goulot |
| **API — taux d'erreur sous charge** | < 1 % | Stabilité de la démo et du service |
| **Front — bundle JS initial (gzip)** | < 100 Ko | Chargement rapide sur mobile / réseau moyen |
| **Front — bundle CSS (gzip)** | < 20 Ko | idem |
| **Front — Lighthouse Performance** | ≥ 90 | Référence standard |
| **Front — LCP** | < 2,5 s | Seuil « bon » des Core Web Vitals |
| **Front — CLS** | < 0,1 | Pas de saut de mise en page |
| **Front — TBT** | < 200 ms | Interface réactive |

---

## 2. Backend — test de charge k6

### Scripts

| Script | Scénario | Compte de test |
|---|---|---|
| `perf/k6-upload-test.js` | 20 utilisateurs virtuels × 30 s, chaque itération = 1 upload de 100 Ko (JWT) | Créé automatiquement dans `setup()` |
| `perf/k6-login-test.js` | 20 VU × 30 s, chaque itération = 1 connexion | idem |

```bash
# API Docker (défaut : http://localhost:5000)
k6 run perf/k6-upload-test.js
# API lancée avec dotnet run, plus de charge, fichiers plus gros
k6 run -e BASE_URL=http://localhost:5180 -e VUS=50 -e DURATION=60s -e FILE_KB=1024 perf/k6-upload-test.js
```

Seuils k6 (le run échoue s'ils sont dépassés) : `http_req_duration p(95) < 2000 ms`, `http_req_failed < 1 %`, checks upload > 99 %.

### Résultats — upload (17/02/2026, machine de développement, API + PostgreSQL en Docker)

| Métrique | Valeur | Cible | Statut |
|---|---|---|---|
| Itérations (uploads) | 500 | > 100 | ✅ |
| Débit | ~32 req/s | > 10 req/s | ✅ |
| Temps moyen | 107,9 ms | < 500 ms | ✅ |
| **P95** | **125,2 ms** | < 500 ms | ✅ |
| Taux d'erreur | 0,00 % | < 1 % | ✅ |
| Données transférées | 52 Mo | — | — |

**Analyse** : à 20 utilisateurs simultanés, l'upload complet (authentification JWT, validation, écriture disque, insertion PostgreSQL) reste autour de 100 ms sans dégradation sur 30 s. Les I/O sont entièrement asynchrones (`CopyToAsync`, `SaveChangesAsync`) : le serveur n'immobilise pas de thread pendant l'écriture. La limite réelle est le disque et la bande passante, pas le code applicatif.

> Le script ayant été rendu autonome le 13/09/2026 (création du compte dans `setup()`, `BASE_URL` paramétrable), la logique de mesure est inchangée ; relancer `k6 run perf/k6-upload-test.js` avant la soutenance pour disposer de chiffres frais sur la machine de démo.

---

## 3. Logs structurés et métriques clés

### Ce qui est journalisé

Depuis le 13/09/2026, l'API produit des **logs structurés** (placeholders nommés capturés comme propriétés par `ILogger`) :

| Source | Événement | Propriétés |
|---|---|---|
| `RequestMetricsMiddleware` (en tête du pipeline) | **Chaque requête HTTP** | `Method`, `Path`, `StatusCode`, `ElapsedMs`, `RequestBytes`, `ResponseBytes` (en-têtes `Content-Length` : taille réelle pour les uploads et les téléchargements, 0 pour les réponses JSON envoyées en *chunked*) — niveau `Information` (2xx/3xx), `Warning` (4xx), `Error` (5xx, avec la trace de l'exception) |
| `FilesController` / `PublicFilesController` | Upload réussi | `FileId`, `SizeBytes`, `ContentType`, `UserId` (ou anonyme), `ExpiresAt`, `PasswordProtected`, `TagCount` |
| `PublicFilesController` | Téléchargement via lien | `FileId`, `SizeBytes`, `ContentType` |
| `PublicFilesController` | Mot de passe refusé | `FileId` (niveau `Warning`) |
| `FilesController` | Suppression | `FileId`, `SizeBytes`, `UserId` |
| `ExpiredFilesCleanupService` | Purge quotidienne | `Count` de fichiers purgés, erreurs de stockage |

Format : **JSON une ligne par événement** hors développement (conteneur Docker, `AddJsonConsole`), texte lisible avec `dotnet run`. Exemple simplifié (la sortie réelle contient aussi la clé `{OriginalFormat}` dans `State`) :

```json
{"Timestamp":"2026-09-13T14:02:11.318Z","EventId":0,"LogLevel":"Information","Category":"DataShare.Api.Controllers.FilesController","Message":"File uploaded 3fa85f64-... (204800 bytes, application/pdf) by user 9b1d..., expires 09/20/2026 14:02:11 +00:00, passwordProtected=True, tags=2","State":{"FileId":"3fa85f64-...","SizeBytes":204800,"ContentType":"application/pdf","UserId":"9b1d...","ExpiresAt":"2026-09-20T14:02:11+00:00","PasswordProtected":true,"TagCount":2}}
{"Timestamp":"2026-09-13T14:02:11.322Z","EventId":0,"LogLevel":"Information","Category":"DataShare.Api.Middleware.RequestMetricsMiddleware","Message":"HTTP POST /api/files -> 201 in 96.4 ms (request 205112 B, response 0 B)","State":{"Method":"POST","Path":"/api/files","StatusCode":201,"ElapsedMs":96.4,"RequestBytes":205112,"ResponseBytes":0}}
```

### Comment les exploiter

```bash
# Suivre les métriques en direct
docker compose logs -f api

# Temps de réponse des uploads (P95 approximatif avec les outils standard)
docker compose logs api | grep '"Path":"/api/files"' | grep '"Method":"POST"' \
  | grep -o '"ElapsedMs":[0-9.]*' | cut -d: -f2 | sort -n | awk '{a[NR]=$1} END {print "n="NR, "p95="a[int(NR*0.95)]" ms"}'

# Volume total transféré (octets) sur la période
docker compose logs api | grep '"SizeBytes"' | grep -o '"SizeBytes":[0-9]*' | cut -d: -f2 | awk '{s+=$1} END {print s" octets"}'

# Erreurs 4xx/5xx
docker compose logs api | grep -E '"LogLevel":"(Warning|Error)"'
```

En production, ces lignes JSON se branchent sans transformation sur un collecteur (Seq, Grafana Loki, ELK) pour obtenir dashboards et alertes (P95 par route, taux d'erreur, volume par jour).

### Métriques de référence par endpoint

| Endpoint | Source | Moyenne | P95 |
|---|---|---|---|
| `POST /api/files` (100 Ko) | k6, 20 VU (17/02/2026) | ~108 ms | ~125 ms |
| `POST /api/auth/login` | `perf/k6-login-test.js` | à mesurer sur la machine de démo (`k6 run perf/k6-login-test.js`) | — |
| `GET /api/files/me` | logs `RequestMetricsMiddleware` | requête indexée (`OwnerId`), quelques ms sur une base de démo | — |
| `POST /api/public/files/{token}/download` | logs `RequestMetricsMiddleware` | dominé par la taille du fichier (streaming) | — |

---

## 4. Frontend — budget et résultats

### Poids du bundle (build de production, 13/09/2026, Vite 7)

| Fichier | Brut | Gzip | Budget | Statut |
|---|---|---|---|---|
| `index-*.js` (Vue, router, Pinia, Axios + vues initiales) | 149,0 Ko | **57,0 Ko** | < 100 Ko | ✅ |
| `index-*.css` (charte Figma) | 18,2 Ko | **3,8 Ko** | < 20 Ko | ✅ |
| `MeView-*.js` (lazy) | 8,8 Ko | 2,9 Ko | — | chargé à la demande |
| `UploadView-*.js` (lazy) | 5,5 Ko | 2,4 Ko | — | chargé à la demande |
| `DownloadView-*.js` (lazy) | 2,6 Ko | 1,4 Ko | — | chargé à la demande |
| **Total initial (HTML + JS + CSS)** | | **≈ 61 Ko gzip** | | ✅ |

Optimisations en place : tree-shaking et minification Vite, **lazy loading** des vues Upload / Download / Mon espace (`() => import(...)` dans le routeur), CSS unique minifié, polices Google en `preconnect`.

### Lighthouse (run du 13/09/2026 — Lighthouse 13.4.1 en mode headless, build de production servi par `vite preview`, page d'accueil ; audit précédent du 31/03/2026 en référence)

| Catégorie | Run du 13/09/2026 | Référence 31/03/2026 | Cible | Statut |
|---|---|---|---|---|
| Performance | **99** | 96 | ≥ 90 | ✅ |
| Accessibility | **88** | 76 | ≥ 90 | ⚠️ |
| Best Practices | **100** | 100 | ≥ 90 | ✅ |
| SEO | **91** | 82 | ≥ 80 | ✅ |

| Métrique | Run du 13/09/2026 | Référence 31/03/2026 | Budget | Statut |
|---|---|---|---|---|
| FCP | 1,7 s | 2,1 s | < 1,5 s | ⚠️ |
| LCP | 1,8 s | 2,3 s | < 2,5 s | ✅ |
| TBT | 0 ms | 0 ms | < 200 ms | ✅ |
| CLS | 0,001 | 0,001 | < 0,1 | ✅ |
| Speed Index | 1,7 s | 2,1 s | — | — |

Rapport complet du 13/09/2026 : [`docs/lighthouse-report.html`](docs/lighthouse-report.html). La capture ci-dessous date de l'audit du 31/03/2026.

![Scores Lighthouse](docs/lighthouse-scores.png)

**Analyse et actions**

- **Accessibility 88 (76 en mars)** : les corrections hors charte du 13/09/2026 (attribut `lang="fr"`, titre de page, meta description — l'ancien `index.html` Vite avait `lang=""` et le titre « Vite App ») ont fait gagner 12 points. Seul audit encore en échec sur la page d'accueil : `button-name` (le bouton rond d'ajout de fichier n'a pas de nom accessible ; un `aria-label` suffirait). Les contrastes de couleurs proviennent de la charte Figma fournie (texte clair sur dégradé orangé) : le point reste remonté à l'UX designer plutôt que corrigé unilatéralement côté développement.
- **FCP 1,7 s** (2,1 s en mars) : encore au-dessus du budget de 1,5 s, dû au chargement bloquant des polices Google Fonts. Piste : auto-héberger les polices (`font-display: swap`) ou précharger la police principale.
- **SEO 91** : seul audit en échec `robots-txt` (aucun `robots.txt` servi par `vite preview`), sans objet pour une application dont les pages utiles sont derrière authentification.
- Reproduire : `npm run build && npm run preview` puis Chrome DevTools → Lighthouse, ou `npx lighthouse http://localhost:4173 --output=html --output-path=docs/lighthouse-report.html`.

---

## 5. Pistes d'optimisation (par ordre d'impact estimé)

| Piste | Gain attendu | Effort |
|---|---|---|
| **Streaming upload** sans mise en tampon complète (`IFormFile` charge le fichier avant l'action) — lecture multipart en flux pour les gros fichiers | Mémoire serveur constante sur 1 Go | Moyen |
| **Compression des réponses** (`UseResponseCompression`) | JSON `/files/me` 3–5× plus léger | Faible |
| **Pagination** de `/files/me` (curseur `createdAt`) | Temps de réponse stable au-delà de quelques centaines de fichiers | Faible |
| **Index composite** `(OwnerId, CreatedAt DESC)` sur `Files` | Tri côté base sans scan | Faible |
| **Polices auto-hébergées** | FCP < 1,5 s | Faible |
| **Stockage objet (S3/MinIO) + CDN** derrière `IFileStorage` | Téléchargements servis hors de l'API, scalabilité horizontale | Moyen |
| **Cache court** (30–60 s) sur `/files/me` | Moins de requêtes SQL en cas de rafraîchissements répétés | Faible |
