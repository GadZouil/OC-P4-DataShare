# Scénario de démonstration — DataShare (≈ 4 minutes)

Objectif : montrer les fonctionnalités clés, l'expérience utilisateur et la gestion des erreurs, sur la stack Docker complète.

---

## 1. Préparation (la veille, puis 10 min avant)

```powershell
git checkout develop ; git pull
.\scripts\verify.ps1 -Quick        # dotnet test + build front : tout doit être vert
.\scripts\deploy.ps1 -Reset        # stack propre : base vide, aucun fichier
```

Vérifier : http://localhost affiche l'accueil, http://localhost:5000/api/health renvoie `{"status":"ok"}`.

Sur le bureau, dans un dossier `demo/` :

| Fichier | Rôle |
|---|---|
| `contrat-client.pdf` (ou n'importe quel PDF/image < 5 Mo) | Fichier « métier » à partager |
| `virus.exe` (un fichier texte renommé) | Démonstration du refus des exécutables |
| `photo-vacances.jpg` (optionnel) | Second fichier pour montrer le filtre par tag |

Fenêtres :

1. Navigateur normal, onglet http://localhost, **déconnecté** (vider le localStorage si besoin : F12 → Application → Local Storage → supprimer `jwt`).
2. Fenêtre de navigation privée vide, prête à recevoir le lien.
3. Terminal : `docker compose logs -f api` (logs JSON en direct).

Identifiants de démo à créer en direct : `lisa@datashare.test` / `Password123`.

---

## 2. Déroulé (parler, cliquer, montrer)

### Étape 1 — Compte et connexion (≈ 40 s)

1. Cliquer **Se connecter** → **Créer un compte**.
2. *Montrer une erreur* : saisir `lisa@datashare.test` et un mot de passe de 5 caractères → message « Mot de passe : minimum 8 caractères. ».
3. Saisir `Password123` deux fois → compte créé, redirection vers la connexion.
4. *Montrer une erreur* : se connecter avec un mauvais mot de passe → « Email ou mot de passe incorrect. » (même message si l'email n'existe pas).
5. Se connecter correctement → retour à l'accueil, le bouton d'en-tête devient **Mon Espace**.

> À dire : « Le JWT est stocké côté client et envoyé sur chaque appel privé ; il expire au bout de 8 heures. »

### Étape 2 — Upload (≈ 50 s)

1. Cliquer sur le bouton rond d'upload → choisir `contrat-client.pdf`.
2. Renseigner : mot de passe `secret1`, expiration **3 jours**, tags `contrat` puis `client` (Entrée après chacun).
3. *Montrer les contrôles de tags* : retaper `Contrat` → « Ce tag est déjà présent. » (insensible à la casse).
4. **Téléverser** → message de succès « conservé pendant 3 jours », lien affiché → **Copier le lien**.

> À dire : « Le fichier est renommé en GUID sur le disque, le token du lien est aléatoire et distinct de l'identifiant. Ligne de log dans le terminal : `File uploaded … bytes`. »

### Étape 3 — Mon espace (≈ 30 s)

1. Cliquer **Mon Espace**.
2. Montrer la ligne du fichier : icône cadenas (protégé), « Expire dans 3 jours », chips `contrat` `client`.
3. Filtrer par tag `cli` → la ligne reste ; `xyz` → « Aucun fichier. » ; effacer.
4. Onglets **Tous / Actifs / Expiré**.
5. (Optionnel) **Ajouter des fichiers** → `photo-vacances.jpg` sans mot de passe, tag `perso` → revenir et filtrer.

### Étape 4 — Côté destinataire (≈ 45 s)

1. Passer sur la **fenêtre privée**, coller le lien `http://localhost/download/<token>`.
2. Montrer : nom du fichier, taille, type, date d'expiration, champ mot de passe.
3. *Montrer une erreur* : **Télécharger** sans mot de passe → « Mot de passe requis. » ; avec `mauvais1` → « Mot de passe incorrect. ».
4. Saisir `secret1` → le fichier se télécharge **avec son nom d'origine**.

> À dire : « Le destinataire n'a pas de compte. Le mot de passe voyage dans le corps d'une requête POST, jamais dans l'URL. Dans les logs : `File downloaded …` et, pour l'échec, une ligne `Warning`. »

### Étape 5 — Gestion des erreurs (≈ 40 s)

1. Onglet connecté → accueil → uploader `virus.exe` → **Téléverser** → « Type de fichier interdit : les exécutables (.exe, .bat, …) ne sont pas acceptés. ».
2. Fenêtre privée → modifier un caractère du token dans l'URL → « Lien invalide ou expiré. ».
3. (Si le temps) Terminal : montrer la ligne `HTTP POST /api/files -> 400 in … ms` en `Warning`.

> À dire : « Double validation : le front prévient, le serveur décide. Chaque message d'erreur de l'API est traduit pour l'utilisateur. »

### Étape 6 — Suppression et observabilité (≈ 30 s)

1. **Mon Espace** → **Supprimer** sur `contrat-client.pdf` → confirmation → la ligne disparaît.
2. Fenêtre privée → recharger le lien → « Lien invalide ou expiré. ».
3. Terminal : `File deleted …`, puis la requête publique en `404`.

> À dire : « Suppression disque + base ; le lien est mort. Chaque requête laisse une ligne JSON exploitable : méthode, route, statut, durée, octets. »

Retour aux slides.

---

## 3. Plan B (si la stack ne démarre pas)

1. `docker compose logs api` — l'erreur la plus fréquente : port 80 / 5000 / 5433 déjà utilisé → arrêter le service concurrent ou changer le port dans `docker-compose.yml`.
2. Si l'API tourne mais pas le front : montrer Swagger n'est pas possible en Docker (Production) → lancer `cd backend/DataShare.Api && dotnet run` et ouvrir http://localhost:5180/swagger.
3. Sinon, dérouler le parcours sur les captures `screenshots/` (accueil, connexion, inscription, Mon espace) et commenter les codes de réponse depuis `docs/API_REFERENCE.md`.

---

## 4. Commandes utiles pendant la discussion

```bash
docker compose ps                                  # état + healthchecks
docker compose logs api | grep '"LogLevel":"Warning"'   # erreurs 4xx tracées
docker compose logs api | grep -o '"ElapsedMs":[0-9.]*' | tail -20   # durées récentes
cd backend && dotnet test                          # 41 tests
k6 run perf/k6-upload-test.js                      # charge (si k6 installé)
```
