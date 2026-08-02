# Captures d'écran à prendre pour la présentation

Prérequis : l'application tourne (`docker compose up -d` ou `.\scripts\deploy.ps1`).
Astuce : prendre toutes les captures en fenêtre 1920×1080, zoom navigateur 100 %.

## Application (http://localhost)

| # | Capture | Où aller | Utilisée sur |
|---|---------|----------|--------------|
| 1 | Page d'accueil / Upload | http://localhost/ (connecté) | Slides 1, 2 |
| 2 | Formulaire d'upload rempli (fichier + expiration + mot de passe + tags) | http://localhost/ — remplir le formulaire avant capture | Slide 10 (secours démo) |
| 3 | Mon espace : historique avec tags et statuts | http://localhost/me (après avoir uploadé 2-3 fichiers avec tags variés) | Slides 3, 10 |
| 4 | Page de téléchargement public (métadonnées + champ mot de passe) | http://localhost/download/<token> — copier le lien de partage depuis « Mon espace », ouvrir en fenêtre privée | Slide 10 (secours démo) |
| 5 | Message d'erreur : mauvais mot de passe | Même page, saisir un mauvais mot de passe | Slide 10 (secours démo) |
| 6 | Message d'erreur : lien invalide/expiré | http://localhost/download/lien-bidon-123 | Slide 10 (secours démo) |
| 7 | Page de connexion | http://localhost/login | Optionnel |
| 8 | Page d'inscription | http://localhost/register | Optionnel |

## Swagger UI (slide 8)

Swagger n'est activé qu'en environnement Development. Deux options :

**Option 1 (rapide) :** ajouter temporairement `ASPNETCORE_ENVIRONMENT: "Development"`
dans la section `environment` du service `api` du `docker-compose.yml`, puis :

```powershell
docker compose up -d api
# Capturer : http://localhost:5000/swagger
# Puis retirer la ligne et relancer : docker compose up -d api
```

**Option 2 :** lancer le backend en local (voir README, option B) et capturer
http://localhost:5180/swagger

## Fichiers déjà présents dans le repo (rien à faire)

| Visuel | Chemin | Slide |
|--------|--------|-------|
| Rapport de couverture | `docs/coverage-report.png` | 12 |
| Scores Lighthouse | `docs/lighthouse-scores.png` | 12 |
| Architecture globale | `docs/diagrams/architecture_globale.drawio.png` | 6 |
| MCD | `docs/diagrams/mcd.drawio.png` | 7 |
| Flux d'authentification | `docs/diagrams/flux_authentification.drawio.png` | 9 |
| Flux upload/partage | `docs/diagrams/flux_upload_partage.drawio.png` | (option slide 10) |

## Divers

| # | Capture | Où aller | Slide |
|---|---------|----------|-------|
| 9 | README rendu sur GitHub | https://github.com/GadZouil/OC-P4-DataShare (branche develop) | 11 |
| 10 | Historique Git conventional commits | `git log --oneline -15` dans un terminal, ou l'onglet Commits GitHub | 13 |
| 11 | Tableau « Supervision et corrections humaines » | AI_USAGE.md section 3 (rendu GitHub) | 13 |
