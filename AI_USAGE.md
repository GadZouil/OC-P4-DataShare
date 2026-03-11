# Documentation de l'utilisation de l'IA — Projet DataShare (OC P5)
# Utilisation de l'IA dans le développement — DataShare

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
- `src/api/files.ts`

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

## 4. Autres usages de l'IA

| Usage | Détail |
|-------|--------|
| Audit de code | Revue de sécurité, détection de dépendances inutiles, vérification de cohérence |
| Debugging | Résolution d'erreurs CORS, JWT, configuration Docker |
| Architecture | Conseils sur l'organisation Services/Models/Controllers |
| Recherche technique | Bonnes pratiques .NET, Vue 3, PostgreSQL — remplace efficacement Google/Stack Overflow |
| Documentation | Assistance rédaction des fichiers .md techniques |

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
