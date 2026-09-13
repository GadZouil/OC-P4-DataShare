# datashare-front

SPA Vue 3 + TypeScript + Vite de DataShare. Voir le [README racine](../../README.md) pour l'installation complète (API + base de données).

## Commandes

```bash
npm install          # dépendances
npm run dev          # serveur de dev Vite : http://localhost:5173 (API attendue sur http://localhost:5180)
npm run build        # type-check (vue-tsc) + build de production dans dist/
npm run preview      # sert dist/ en local (http://localhost:4173) — utilisé pour les audits Lighthouse
npm run lint         # ESLint
```

## Configuration

| Variable | Défaut | Rôle |
|---|---|---|
| `VITE_API_URL` | `http://localhost:5180` | URL de base de l'API. En Docker : `/api` (proxy nginx, même origine). |

Créer un fichier `.env.local` (ignoré par Git) pour surcharger, ex. `VITE_API_URL=http://localhost:5000`.

## Structure

```
src/
├── api/        # appels HTTP typés (auth.ts, files.ts) + mapping des erreurs API en messages FR
├── services/   # instance Axios (baseURL, injection du JWT, gestion du 401)
├── router/     # routes + garde d'authentification (/me)
├── stores/     # store Pinia (état d'authentification)
├── layouts/    # PublicLayout (en-tête / pied de page des pages publiques)
├── views/      # UploadView, DownloadView, LoginView, RegisterView, MeView
└── styles/     # datashare.css : charte issue des maquettes Figma
```

Les tests end-to-end Cypress vivent dans `../cypress` (voir [TESTING.md](../../TESTING.md)).
