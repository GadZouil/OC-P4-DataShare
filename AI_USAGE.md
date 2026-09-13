# Utilisation de l'IA dans le développement — Projet DataShare (OC P4)

## 1. Posture adoptée

L'IA a été utilisée selon une approche **"assignation de tâche à un développeur junior"** pour la tâche principale (US08), puis en **binômage ponctuel** pour le reste du projet.

### Outils utilisés
| Phase | Outil | Usage |
|-------|-------|-------|
| Début de projet | ChatGPT | Aide générale, premières questions d'architecture |
| Questions complexes | Claude Opus (en ligne, via claude.ai) | Vision globale de l'architecture, décisions structurantes |
| Développement | Claude Opus & Sonnet (via Cursor) | Opus pour les questions complexes nécessitant le contexte complet du codebase, Sonnet pour les questions ciblées sur une zone de code |

L'évolution a été naturelle : ChatGPT pour démarrer, puis Claude Opus en ligne pour les réflexions d'architecture, et enfin Cursor avec le codebase chargé pour le développement concret. Le passage à Cursor a permis à l'IA d'avoir le contexte projet complet, rendant les réponses bien plus pertinentes.

## 2. Tâche principale : US08 — Gestion des tags (frontend)

### Contexte
Le backend supportait déjà les tags (`string[]` à l'upload, renvoyés dans les réponses API). L'US08 consistait à implémenter toute la partie frontend.

### Prompt utilisé
Le prompt complet est disponible dans [`IA_Instructions.txt`](./IA_Instructions.txt). En résumé :
- Ajouter un champ de saisie de tags (chips) sur la page Upload
- Afficher les tags sur la page "Mon espace"
- Implémenter un filtre local par tag (case-insensitive, contains)
- Contraintes : pas de nouvelle librairie, style cohérent, normalisation UI (trim, pas de doublon, max 24 caractères)

### Fichiers modifiés par l'IA
- `src/views/UploadView.vue`
- `src/views/MeView.vue`
- `src/styles/datashare.css`
- (`src/api/files.ts` était autorisé « si nécessaire » : l'envoi des tags en `FormData` existait déjà, aucune modification)

### Traçabilité Git

| Commit | Auteur | Contenu |
|---|---|---|
| `56f411d` `feat(ai): add tags UI and filtering` | Code produit par l'IA, commité tel quel après lecture | Saisie de tags (chips), affichage dans « Mon espace », filtre local |
| `a8c23fc` `fix(tags): show clear UI errors for empty/too long/duplicate tags` | Revue humaine | Messages d'erreur utilisateur, validation renforcée |

## 3. Supervision et corrections humaines

Après réception du code IA, les corrections suivantes ont été apportées manuellement :

| Problème détecté | Correction |
|-----------------|------------|
| Messages d'erreur UI absents pour tags invalides | Ajout de feedback visuel pour chaque cas d'erreur (vide, trop long, doublon) |
| Validation insuffisante côté UI | Renforcement des contrôles (trim, longueur max, unicité case-insensitive) |

### Processus de revue
1. Lecture complète du diff généré par l'IA
2. Test manuel des cas limites (tag vide, tag > 24 chars, doublon exact, doublon casse différente)
3. Identification des lacunes UX (aucun message d'erreur pour l'utilisateur)
4. Correction et commit séparé pour traçabilité

### Points de vigilance vérifiés lors de la revue
- **Sécurité** : le back reste l'autorité (normalisation et déduplication des tags aussi côté API, `Take(20)`) ; l'IA n'a touché ni au back, ni à l'authentification, conformément à la consigne.
- **Maintenabilité** : pas de nouvelle dépendance, styles ajoutés dans `datashare.css` avec le préfixe `ds-` existant, logique de tags isolée dans des fonctions (`addTag`, `removeTag`).
- **Conformité aux maquettes** : chips simples reprenant les couleurs de la charte Figma.

## 4. Autres usages de l'IA

| Usage | Détail |
|-------|--------|
| Audit de code | Revue de sécurité, détection de dépendances inutiles, vérification de cohérence |
| Debugging | Résolution d'erreurs CORS, JWT, configuration Docker |
| Architecture | Conseils sur l'organisation Services/Models/Controllers |
| Recherche technique | Bonnes pratiques .NET, Vue 3, PostgreSQL — remplace efficacement Google/Stack Overflow |
| Documentation | Assistance rédaction des fichiers .md techniques |
| Revue finale avant soutenance (09/2026) | Audit de cohérence code ↔ documentation (OpenAPI périmé, SECURITY.md décrivant des mécanismes absents du code), propositions de tests de sécurité (isolation entre utilisateurs) et de logs structurés, relecture du support de présentation. Chaque proposition a été relue, exécutée (`dotnet test`, `npm run build`) et validée avant commit. |

L'IA s'est révélée particulièrement pratique pour **l'audit**, **les conseils d'architecture** et **les recherches techniques**, où elle remplace efficacement une recherche internet classique avec un contexte projet déjà chargé.

## 5. Apports et limites constatés

### Apports
- **Gain de temps** : US08 complète (~200 lignes, 4 fichiers) produite en ~15 min vs ~2h estimées
- **Audit efficace** : Détection rapide de problèmes de sécurité, de dépendances inutiles
- **Recherche contextuelle** : Réponses adaptées au projet, pas de réponses génériques
- **Cohérence** : Code généré respectant le style existant

### Limites
- **UX négligée** : L'IA n'a pas pensé aux messages d'erreur utilisateur
- **Pas de tests spontanés** : Aucun test proposé pour le code généré
- **Supervision indispensable** : Sans revue humaine, les cas limites auraient été livrés sans feedback

## 6. Conclusion

L'IA s'est révélée efficace comme outil de productivité à tous les niveaux du projet. L'approche "junior supervisé" pour l'US08 a bien fonctionné, et l'usage en binômage pour le reste (audit, debug, recherche) a significativement accéléré le développement. La supervision humaine reste indispensable pour la qualité UX et la couverture de tests.
