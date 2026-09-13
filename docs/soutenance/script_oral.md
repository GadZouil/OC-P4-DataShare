# Script oral — Soutenance DataShare (15 min + 10 min de discussion + 5 min de débrief)

Support : `DataShare_soutenance.pptx` (15 slides, notes du présentateur incluses). Démo : voir `DEMO.md`.

**Cadre OpenClassrooms** : présentation 15 min (±5 — refusée en dessous de 10 ou au-dessus de 20), l'évaluateur joue **Lisa** (responsable produit) pendant la présentation et la discussion, puis débrief hors rôle.
**Ton** : professionnel, phrases courtes, tu parles à Lisa. Chrono lancé au « Bonjour ».

Légende : `[Respire]` = pause 1–2 s · `→` = transition · temps entre parenthèses = cumulé.

---

## Avant de commencer (checklist)

1. Stack lancée : `.\scripts\deploy.ps1` → http://localhost répond (`GET /api/health` → `{"status":"ok"}`).
2. `dotnet test` vert le matin même (41 tests) — chiffre à citer.
3. Sur le bureau : `contrat-client.pdf` (ou tout fichier < 5 Mo), un faux `virus.exe` (fichier texte renommé).
4. Navigateur : onglet 1 = http://localhost (déconnecté), fenêtre privée prête, terminal prêt avec `docker compose logs -f api`.
5. PowerPoint en mode présentateur (les notes reprennent ce script en résumé), chrono visible.
6. Compte de démo : à créer en direct (`lisa@datashare.test` / `Password123`) — montre l'inscription.

---

## SLIDE 1 — Titre (0:00 → 0:30)

Bonjour Lisa.

Je suis Ethan Legros, référent technique senior sur DataShare.

Je vous présente le prototype de plateforme de transfert sécurisé de fichiers, livré pour la démonstration investisseurs.

Je suivrai cet ordre : le contexte, les choix techniques, l'architecture, une démonstration, la documentation et la qualité, puis le pilotage du copilote IA. Quinze minutes, ensuite vos questions.

`[Respire]` → *Le contexte d'abord.*

---

## SLIDE 2 — Contexte et mission (0:30 → 1:30)

DataShare cible des freelances et des petites entreprises qui envoient des fichiers à leurs clients.

Le besoin est simple : un lien de téléchargement temporaire, sans imposer de compte au destinataire.

Vous m'avez demandé un MVP fonctionnel en quatre semaines, prêt pour des investisseurs, et fidèle aux maquettes Figma de l'UX designer.

Mon rôle : concevoir l'architecture et le modèle de données, piloter le développement, garantir la qualité, et superviser un copilote IA sur une user story précise.

Trois contraintes : une stack à choisir parmi les options imposées, les maquettes à respecter, et l'IA générative limitée à une seule US, tracée dans Git.

→ *Voici ce qui a été livré.*

---

## SLIDE 3 — Périmètre (1:30 → 2:15)

Le MVP demandait six user stories : upload avec compte, téléchargement par lien, inscription, connexion, historique, suppression.

J'ai aussi livré les quatre fonctionnalités avancées : upload anonyme, tags, mot de passe sur le fichier, expiration automatique.

Dix user stories sur dix, sur cinq écrans conformes aux maquettes, responsive mobile.

La US08, les tags, a été développée par le copilote IA sous ma supervision — j'y reviens à la fin.

→ *Les choix techniques.*

---

## SLIDE 4 — Choix techniques (2:15 → 3:45)

Parmi les options imposées, j'ai retenu ASP.NET Core 9, Vue 3 avec TypeScript, PostgreSQL 16, le stockage local et Docker Compose. Mon critère : livrer un MVP robuste en quatre semaines sans réinventer ce qui existe.

ASP.NET Core m'apporte Identity — le hachage PBKDF2 et la gestion des comptes sans code maison — Entity Framework Core qui paramètre toutes les requêtes, et un service d'arrière-plan natif pour purger les fichiers expirés. Sous charge, l'upload tient un P95 à 125 millisecondes.

Vue 3 avec TypeScript donne un front typé de bout en bout, un bundle initial de 57 kilo-octets gzip, et du chargement à la demande des vues.

PostgreSQL colle au modèle relationnel utilisateur–fichiers, avec le type natif `text[]` pour les tags.

Le stockage local suffit pour un prototype ; il est derrière une interface `IFileStorage`, donc on bascule vers S3 plus tard sans toucher aux contrôleurs.

Docker Compose, enfin : une démo reproductible en une commande, avec nginx qui sert le front et proxifie l'API.

`[Respire]` → *Comment tout s'assemble.*

---

## SLIDE 5 — Architecture (3:45 → 5:00)

*[Pointer le schéma de gauche à droite.]*

Le navigateur charge la SPA Vue depuis nginx, sur le port 80. Nginx proxifie aussi `/api` vers le backend : même origine, donc pas de problème de CORS en production.

L'API ASP.NET Core est stateless : l'identité est dans le JWT, aucune session serveur — on peut la répliquer horizontalement.

Elle parle à PostgreSQL via EF Core, et écrit les fichiers sur un volume disque sous des noms GUID. Seules les métadonnées vont en base.

Le pipeline HTTP, en bas : un middleware de métriques qui journalise chaque requête, puis routage, CORS, authentification, autorisation, contrôleurs.

Les migrations s'appliquent automatiquement au démarrage : aucune étape manuelle pour installer le schéma.

→ *Le modèle de données derrière.*

---

## SLIDE 6 — Modèle de données (5:00 → 5:45)

Deux entités.

`AppUser`, l'utilisateur Identity : je ne réécris ni le hachage ni les tables de comptes.

`FileItem` porte les métadonnées : nom d'origine pour l'affichage, nom stocké en GUID sur disque, taille, type, token de partage unique, dates de création et d'expiration, mot de passe haché optionnel, tags.

Trois décisions : `OwnerId` est nullable pour l'upload anonyme ; le token de partage est distinct de l'identifiant technique ; et le contenu du fichier n'est jamais en base.

→ *Le contrat entre le front et le back.*

---

## SLIDE 7 — Contrat d'API (5:45 → 6:30)

Dix endpoints REST, écrits en OpenAPI 3 dans le repo — le fichier est validé — et exposés par Swagger en développement.

Deux endpoints publics pour créer un compte et se connecter, qui renvoient un JWT valable huit heures.

Quatre endpoints privés sous JWT pour mes fichiers : upload, historique avec filtre actif / expiré, détail, suppression.

Trois endpoints publics pour les liens : upload anonyme, métadonnées, téléchargement avec le mot de passe dans le corps de la requête.

Un détail qui compte : un fichier qui ne m'appartient pas répond 404, pas 403 — on ne révèle pas son existence.

→ *La sécurité, avant la démo.*

---

## SLIDE 8 — Sécurité et gestion des accès (6:30 → 7:30)

Authentification : JWT signé HMAC, validé à chaque requête ; mots de passe hachés PBKDF2 ; et l'API refuse de démarrer si la clé de signature fait moins de 32 caractères — plus de clé de repli codée en dur.

Isolation : l'identité vient uniquement du token, jamais d'un paramètre client ; chaque requête privée filtre sur le propriétaire ; et c'est couvert par des tests qui font lire et supprimer le fichier d'un autre utilisateur.

Liens : token aléatoire de 32 octets, mot de passe de fichier haché, lien expiré → 410.

Fichiers : renommés en GUID contre le path traversal, exécutables refusés, 1 Go maximum, purge quotidienne.

Validation double — client pour l'UX, serveur comme autorité — et aucun secret dans Git.

Ce qui n'est pas dans le MVP est assumé et documenté : HTTPS sur le reverse proxy, rate limiting, refresh tokens.

`[Respire — gorgée d'eau]` → *Je bascule sur l'application.*

---

## SLIDE 9 — Démonstration (7:30 → 11:30) — 4 minutes, chrono

*[Quitter PowerPoint → navigateur. Parcours détaillé dans `DEMO.md`. Parler moins vite que d'habitude.]*

1. **Compte et connexion** — « Je crée un compte : email, mot de passe de huit caractères minimum. Si je me trompe, le message est explicite. Je me connecte : le front stocke le JWT. »
2. **Upload** — « Je téléverse un contrat : expiration trois jours, mot de passe, deux tags — la US08 livrée avec l'IA. Le lien est copié en un clic. »
3. **Mon espace** — « Je retrouve le fichier avec ses tags, je filtre, j'accède au lien. »
4. **Destinataire** — « En navigation privée, sans compte : le nom et la taille s'affichent, le mot de passe est demandé, le fichier arrive avec son nom d'origine. »
5. **Erreurs** — « Mauvais mot de passe : message clair. Un `.exe` : refusé par le serveur, message traduit. Un lien inventé : lien invalide ou expiré. Jamais d'écran blanc. »
6. **Suppression et logs** — « Je supprime : confirmation, le lien ne fonctionne plus. Dans le terminal, chaque requête est journalisée avec sa durée et son volume. »

« Voilà le parcours de bout en bout : émetteur, destinataire, sécurité, erreurs, observabilité. »

`[Respire]` → *Retour aux slides : la qualité en chiffres.*

---

## SLIDE 10 — Qualité : tests (11:30 → 12:15)

Quarante-et-un tests backend : deux unitaires sur la purge, trente-neuf d'intégration HTTP sur tous les endpoints, avec une base en mémoire et un schéma d'authentification de test.

Couverture : 91 % du code métier, migrations exclues — le seuil demandé était 70.

Cinq scénarios Cypress end-to-end contre la vraie stack Docker : cycle de vie d'un fichier, tags, mot de passe, limites, parcours complet.

Et zéro erreur ESLint, type-check strict, nullable activé en C#.

Le plan de tests liste chaque user story avec son critère d'acceptation — par exemple : le fichier d'un autre utilisateur répond 404 et reste intact.

→ *Performance et observabilité.*

---

## SLIDE 11 — Performance et observabilité (12:15 → 13:00)

k6, vingt utilisateurs pendant trente secondes : cinq cents uploads, P95 à 125 millisecondes, zéro erreur.

Front : 57 kilo-octets de JavaScript gzip au chargement, Lighthouse 99 en performance.

L'accessibilité est passée de 76 à 88 avec les corrections hors charte — langue, titre, meta. Le reste tient aux contrastes de la charte Figma : je l'ai remonté à l'UX designer plutôt que de modifier la charte seul.

Et l'observabilité : chaque requête produit une ligne JSON avec méthode, route, statut, durée, volume ; chaque upload, téléchargement ou suppression est tracé avec l'identifiant et la taille. Exploitable par grep aujourd'hui, par un collecteur demain.

→ *La documentation et la maintenance.*

---

## SLIDE 12 — Documentation et maintenance (13:00 → 13:45)

Tout est dans le dépôt GitHub.

Le README lance la stack en une commande. La documentation technique PDF couvre architecture, choix, modèle, API, sécurité, qualité, installation et IA.

Le contrat OpenAPI est validé, doublé d'une référence lisible.

Quatre fichiers de suivi : tests, sécurité, performance, maintenance. Des scripts de déploiement, de sauvegarde, de restauration, et un script de vérification qui enchaîne tests, couverture et audits.

Côté maintenance : audit des dépendances à chaque livraison, patchs mensuels, majeures sur branche dédiée, jamais de `npm audit fix --force`. Un cas réel est traité : une CVE sur Microsoft.OpenApi en août, analysée, patchée, testée.

→ *Le pilotage de l'IA.*

---

## SLIDE 13 — Pilotage du copilote IA (13:45 → 14:45)

Une seule user story confiée à l'IA, comme à un développeur junior : les tags côté front.

Le prompt est versionné dans `IA_Instructions.txt` : périmètre borné au front, fichiers autorisés, pas de nouvelle librairie, style existant, règles de normalisation.

L'IA a livré en un quart d'heure environ deux cents lignes.

En relecture : le back était intact, pas de dépendance ajoutée — mais aucun message d'erreur pour l'utilisateur, et une validation incomplète sur les cas limites. J'ai corrigé dans un commit séparé : contribution IA, puis revue humaine, visibles dans l'historique.

Bilan : l'IA accélère et respecte le style ; elle ne propose ni tests ni UX d'erreur. La supervision décide. J'ai gardé la même posture pour l'audit, le débogage et la documentation.

→ *Les difficultés rencontrées.*

---

## SLIDE 14 — Difficultés et solutions (14:45 → 15:30)

Cinq difficultés concrètes.

L'upload anonyme : le modèle exigeait un propriétaire ; j'ai rendu `OwnerId` nullable par migration — leçon : concevoir le modèle avec les US avancées en tête.

Les vulnérabilités : une CVE NuGet et seize avis npm — analyse du risque réel, suppression d'une dépendance morte, mise à jour, tests.

Une mise à jour d'axios a cassé le type-check — détectée par le build, corrigée en une ligne : le typage est un filet de sécurité.

Tester l'isolation entre utilisateurs avec un seul utilisateur de test — j'ai enrichi le handler d'authentification de test pour incarner un second compte.

Et l'accessibilité liée à la charte, traitée avec l'UX plutôt que contre elle.

→ *Je conclus.*

---

## SLIDE 15 — Bilan et perspectives (15:30 → 16:00)

En quatre semaines : dix user stories, une application conteneurisée, testée, sécurisée, documentée — et un cas réel de vulnérabilité traité.

Prochaines itérations : HTTPS et rate limiting, stockage S3 grâce à l'abstraction en place, atelier accessibilité avec l'UX, antivirus, et collecte des logs.

Merci Lisa. Je suis prêt pour vos questions.

`[Respire — souris — silence 1 seconde]`

---

## Régulation du temps

| Situation | Action |
|---|---|
| 7:00 atteint avant la démo | Slide 8 : ne lire que les trois premières cartes |
| 12:30 en sortie de démo | Fusionner slides 10 et 11 : citer 41 tests, 91 %, P95 125 ms, logs — 45 s |
| 15:00 avant la slide 14 | Ne donner que deux difficultés (upload anonyme, vulnérabilités) |
| Terminé à 12:00 | Développer : IFileStorage → S3 (30 s), tests d'isolation (30 s), lecture d'une ligne de log (30 s) |
| Démo cassée | Captures `screenshots/` et raconter le parcours ; montrer `docker compose logs api` si l'API tourne |

---

# Discussion (10 min) — questions probables et réponses

L'évaluateur reste **Lisa**. Réponses courtes, ancrées dans le code et les documents.

### Compréhension métier

**« Pourquoi un freelance utiliserait DataShare plutôt que WeTransfer ? »**
« Le contrôle : compte, historique, expiration choisie, mot de passe, tags. Et une base technique qu'on peut faire évoluer — SSO, S3, antivirus, quotas — sans réécrire. »

**« L'upload anonyme ne vide-t-il pas l'intérêt du compte ? »**
« C'est l'entrée sans friction. L'anonyme obtient un lien mais pas d'historique ni de suppression. Le compte devient le bénéfice : retrouver, organiser, révoquer. »

**« Que se passe-t-il au bout de 7 jours ? »**
« Le lien répond 410 immédiatement à l'expiration ; le service de purge supprime le fichier et ses métadonnées au démarrage de l'API puis toutes les 24 heures. »

### Méthodologie et architecture

**« Comment est organisé le code ? »**
« Contrôleurs fins, services derrière des interfaces, modèles et migrations. Front : vues, clients API typés, store d'authentification, un seul fichier de styles issu de la charte. Faible couplage, testable. »

**« Pourquoi JWT plutôt que des sessions ? »**
« API stateless : pas d'état serveur à synchroniser, adapté à une SPA et à la montée en charge. Contrepartie : révocation impossible avant expiration — d'où les refresh tokens en roadmap et la durée de 8 h. »

**« Pourquoi 404 et pas 403 sur le fichier d'un autre ? »**
« Un 403 confirmerait que le fichier existe. Le 404 ne donne aucune information — et c'est testé. »

**« Comment gérez-vous les erreurs ? »**
« Côté API : codes HTTP explicites, messages texte ou JSON, jamais de 500 sur une entrée invalide. Côté front : chaque message API est traduit en français, le serveur injoignable et le 413 sont gérés, un 401 sur un endpoint privé renvoie à la connexion avec un message. »

**« Deux utilisateurs envoient un fichier du même nom ? »**
« Aucun conflit : le nom d'origine ne sert qu'à l'affichage ; sur disque chaque fichier porte un GUID. »

### Documentation

**« Pour qui est écrite la documentation ? »**
« Trois publics : le README pour qui clone le projet, la doc technique pour un architecte ou un investisseur technique, les quatre fichiers de suivi pour l'exploitation. »

**« OpenAPI est-il aligné avec le code ? »**
« Oui : réécrit d'après les contrôleurs et validé par un linter OpenAPI ; les routes, champs et codes de réponse correspondent — vous pouvez comparer avec Swagger en développement. »

### Qualité et maintien en conditions opérationnelles

**« 70 % de couverture : vous êtes à combien ? »**
« 91 % en lignes et 73 % en branches sur le code métier, migrations EF exclues via Coverlet — elles sont générées. Brut, on serait autour de 18 % à cause des deux mille lignes de migrations, ce qui ne mesure rien. »

**« Vos tests end-to-end couvrent quoi ? »**
« Cinq scénarios Cypress contre la stack Docker : cycle de vie, tags, mot de passe, limites, parcours complet inscription → upload → partage → suppression. »

**« Comment vérifiez-vous la sécurité ? »**
« Tests d'intégration dédiés : lecture et suppression croisées → 404, endpoints privés sans token → 401, lien expiré → 410, métadonnées publiques sans token ni propriétaire. Plus un audit de dépendances à chaque livraison. »

**« Comment maintenez-vous l'application ? »**
« MAINTENANCE.md : lancement et logs Docker, sauvegarde base + volume de fichiers ensemble, mise à jour des dépendances avec fréquence et risques, procédure de correction de bug, checklist de mise en production. »

**« Que montrent vos logs ? »**
« Une ligne JSON par requête — méthode, route, statut, durée, octets — et une par événement métier : upload, téléchargement, suppression, avec identifiant et taille. On en tire un P95 par route et un volume transféré avec grep, ou avec Seq ou Loki en production. »

**« Et si le disque est plein ? »**
« Aujourd'hui l'upload échouerait en 500 ; la parade MVP est la limite de 1 Go et la purge. Prochaine étape : quota par utilisateur et alerte sur le volume via les logs. »

### IA et supervision

**« Pourquoi une seule US à l'IA ? »**
« C'était la consigne, et c'est une bonne gouvernance : tâche bornée, prompt précis, revue obligatoire. Le reste, je l'ai écrit — l'IA a servi en binôme pour l'audit, le débogage, la recherche et la doc, et c'est documenté. »

**« Qu'est-ce que l'IA a raté ? »**
« Le feedback utilisateur : tags invalides sans message. Et elle n'a proposé aucun test. Corrigé en revue, commits séparés. »

**« Comment prouvez-vous la supervision ? »**
« Prompt versionné, commit `feat(ai)` puis commit `fix` de revue, section dédiée dans AI_USAGE.md et dans la doc technique, points de vigilance listés. »

### Pièges préparés

**« HTTPS ? »**
« Pas dans le code, volontairement : le prototype tourne sur localhost. En production, terminaison TLS sur nginx, puis redirection et HSTS côté API — documenté en roadmap, avec le rate limiting. »

**« CORS ? »**
« Politique restreinte aux origines configurées — par défaut le serveur de dev Vite. En Docker, nginx sert le front et proxifie l'API en même origine, CORS n'intervient pas. »

**« Accessibilité 88 ? »**
« Mesuré, documenté : 76 en mars, 88 en septembre après les corrections qui ne touchent pas la charte (langue, titre, meta). Le reste — contrastes de la charte, nom accessible du bouton d'upload — est remonté à l'UX. »

**« Une vulnérabilité dans vos dépendances ? »**
« Oui, traitée deux fois : CVE Microsoft.OpenApi en août — risque faible, patch mineur, tests verts — et des avis npm sur des dépendances transitives d'outils, corrigés par `npm audit fix`. Zéro vulnérabilité connue aujourd'hui, décisions dans SECURITY.md. »

**« Pourquoi pas S3 tout de suite ? »**
« Quatre semaines, une démo locale : le disque suffit. L'interface `IFileStorage` est en place ; S3 est une implémentation de plus, sans changer l'API. »

**« Comment empêcher qu'un utilisateur télécharge le fichier d'un autre ? »**
« Trois couches : filtre propriétaire depuis le JWT côté serveur, token de partage aléatoire pour le public, mot de passe haché si protégé. Couvert par les tests de sécurité. »

**« Que ferait un attaquant avec un JWT volé ? »**
« Il agit pendant 8 h maximum sur ce compte. Parades : HTTPS pour éviter le vol, durée plus courte avec refresh tokens, rotation de la clé de signature qui invalide tous les tokens. »

---

## Débrief (5 min, hors rôle)

- Ce dont je suis satisfait : périmètre complet, qualité mesurée, sécurité testée, IA tracée, documentation alignée sur le code.
- Ce que je ferais différemment : lancer l'audit de dépendances plus tôt et en continu ; prévoir les tests d'isolation dès la première US ; challenger la charte avec l'UX dès la première maquette.
- Question à poser si on me laisse l'initiative : « Qu'est-ce qui vous a le plus convaincu — ou inquiété — pour la suite du produit ? »

---

## Carte chronométrique

```
0:00  1  Titre
0:30  2  Contexte et mission
1:30  3  Périmètre
2:15  4  Choix techniques
3:45  5  Architecture
5:00  6  Modèle de données
5:45  7  API
6:30  8  Sécurité
7:30  9  DÉMO (4 min)
11:30 10 Qualité : tests
12:15 11 Performance et observabilité
13:00 12 Documentation et maintenance
13:45 13 Pilotage IA
14:45 14 Difficultés et solutions
15:30 15 Bilan → questions (16:00)
```
