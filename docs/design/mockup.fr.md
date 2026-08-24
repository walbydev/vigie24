# Vigie24 — Mockup visuel de référence

> **Document de référence technique/design** · Version de cadrage · 18 août 2026

## 1. Introduction

Vigie24 est un site francophone de **veille technologique** : il agrège, contextualise et classe des articles issus de sources spécialisées, afin de faire émerger chaque jour les signaux les plus utiles. Le produit doit donner une sensation de lecture éditorialisée, rapide et fiable — plus proche d'un poste de pilotage que d'un flux social.

La direction artistique combine une interface éditoriale claire, des repères data sobres et un accent de couleur chaleureux pour les actions importantes.

### 1.1 Charte graphique

| Élément | Valeur / usage |
|---|---|
| Bleu pétrole | `#1B4B5A` — marque, navigation, titres forts, liens actifs |
| Vert-sarcelle | `#5FB5A3` — états positifs, sélection, signaux, score de confiance |
| Vert-sarcelle pâle | `#EEF5F3` — surfaces douces, encarts et filtres sélectionnés |
| Ambre | `#E8A33D` — ranking, alertes éditoriales, appels à l'action secondaires |
| Fond clair | Blanc cassé / gris très léger — hiérarchie et respiration |
| Typographie UI | **Inter** — corps, contrôles, métadonnées |
| Typographie titres | **Space Grotesk** — logo, titres et repères de section |
| Données / scores | **JetBrains Mono** — scores, dates techniques, identifiants et labels |

### 1.2 Principes de composition

- **Hiérarchie avant décoration** : un titre et son contexte sont lisibles en un coup d'œil.
- **Signal explicable** : le score mixte est toujours accompagné d'un détail accessible au survol ou au focus.
- **Densité maîtrisée** : la colonne centrale reste prioritaire ; la sidebar enrichit sans concurrencer le fil.
- **Francophonie visible** : catégories, libellés, dates et microcopies sont en français ; les noms propres des sources ne sont pas traduits.
- **Accessibilité** : contrastes AA, focus clavier, icônes accompagnées d'un libellé ou d'un tooltip, zones tactiles d'au moins 44 px.

## 2. Header persistant

Le header reste visible au scroll (hauteur cible : 68–72 px). Il ancre le produit, donne accès aux thèmes et maintient la recherche à portée de main.

```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  ◉ VIGIE24       Thèmes ▾   IA   Climat   Cybersécurité   Produit   Start-up        ⌕ Rechercher      │
│                                                                                   ◌ Profil   ☼ Mode   │
└──────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### 2.1 Anatomie et comportements

- **Logo** : mot-symbole `VIGIE24` en Space Grotesk, bleu pétrole ; le point/œil en tête est l'élément de vigie. Clic = retour à l'accueil.
- **Navigation par thèmes** : `Thèmes ▾` ouvre un méga-menu organisé par familles ; les 4–5 thèmes les plus consultés peuvent rester visibles. L'état actif est souligné en vert-sarcelle.
- **Recherche** : champ compact avec icône loupe, raccourci clavier affiché (`⌘ K` / `Ctrl K`), recherche plein texte et suggestions de thèmes/sources.
- **Profil** : avatar ou initiales, accès aux articles sauvegardés, préférences, abonnements et déconnexion.
- **Toggle dark mode** : interrupteur avec état explicite `Mode Radar` ; le choix est mémorisé. Le changement de thème ne doit pas déplacer la mise en page.
- **Sticky** : ombre de 1 px ou bordure discrète après 8 px de scroll ; header opaque pour préserver le contraste.

## 3. Bandeau du jour — « Le 24 de Vigie24 »

Le bandeau ouvre la page et donne immédiatement le contexte temporel du digest. Il utilise une surface `#EEF5F3`, une bordure gauche ambre et un rythme plus éditorial que le reste de l'interface.

```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  LE 24 DE VIGIE24                                                                  mardi 18 août 2026 │
│  Les signaux tech à retenir aujourd'hui                                                              │
│                                                                                                      │
│  24 articles sélectionnés     12 thèmes     08 sources croisées     ● 6 tendances fortes            │
│                                                                                                      │
│  [ Recevoir le flux RSS ]     [ Voir le digest complet ]                                            │
└──────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### 3.1 Spécifications

- **Date** : date longue localisée, alignée à droite sur desktop ; au-dessus du sous-titre sur mobile.
- **Stats du jour** : quatre indicateurs maximum, en `JetBrains Mono`, avec libellé accessible. Les valeurs sont calculées sur le périmètre du digest publié.
- **RSS** : bouton primaire `Recevoir le flux RSS`, ouvrant la consigne d'abonnement et l'URL du flux ; prévoir un bouton copier et une confirmation non bloquante.
- **Digest complet** : lien secondaire vers une vue filtrée contenant les 24 éléments.
- **État vide / retard** : afficher « Le 24 sera publié à 08:00 » ou « Aucun signal suffisamment corroboré » plutôt qu'un bandeau vide.

## 4. Anatomie d'une card d'article dans le fil principal

La card est l'unité de lecture du fil. Elle doit fonctionner sans illustration (fallback couleur + initiales) et conserver une cible de clic claire : le titre et l'illustration ouvrent le détail.

```text
┌───────────────────────────────────────────────────────────────────────────────────────────────┐
│ ┌───────────────┐  IA  /  Modèles fondamentaux                              ★ 8,7  ◇ 3  ♡ 42 │
│ │               │  NOUVEAU · il y a 2 h                                                        │
│ │  illustration │  Les modèles ouverts changent-ils enfin la donne pour les équipes produit ? │
│ │  16:10        │  Une synthèse des annonces, des capacités observées et des limites à court  │
│ │               │  terme, avec le regard de trois sources indépendantes.                      │
│ └───────────────┘                                                                               │
│                      18 août 2026 · Camille Martin · Tech Horizon                               │
│                      ↗ Croisé avec Le Monde Informatique · Sifted                                │
│                      [ Lire ]   [ ♧ Plus tard ]   [ ☆ Favori ]   [ Noter ]                      │
└───────────────────────────────────────────────────────────────────────────────────────────────┘
```

### 4.1 Détail des composants

1. **Illustration** : ratio 16:10, coins 8 px, traitement colorimétrique peu saturé. `alt` descriptif ; sinon motif de la catégorie.
2. **Catégorie / sous-catégorie** : catégorie en bleu pétrole (ex. `IA`), sous-catégorie en gris ; badge `NOUVEAU` limité aux 24 dernières heures.
3. **Titre** : Space Grotesk, 20 px desktop, deux lignes maximum dans le fil ; formulation conservée depuis l'article mais corrigée pour la casse si nécessaire.
4. **Résumé** : 2–3 lignes, 15–16 px Inter, orienté « pourquoi c'est important » plutôt que simple répétition du titre.
5. **Métadonnées** : date relative + date absolue au survol, auteur si disponible, source principale avec lien externe et icône de sortie.
6. **Liens croisés** : `Croisé avec` liste les autres sources qui traitent le même événement ; clic = filtre ou panneau de comparaison.
7. **Actions** : `Lire`, `Plus tard`, `Favori`, `Noter`. Les actions non destructives sont des boutons ; leur état actif est persistant et annoncé aux lecteurs d'écran.

### 4.2 États et interaction

- **Default** : bordure `#DDE8E5`, fond blanc cassé.
- **Hover / focus** : élévation très légère, bordure vert-sarcelle, aucune transformation qui nuirait à la stabilité du fil.
- **Lu** : titre légèrement moins contrasté, mais toujours AA ; ne pas supprimer le score.
- **Épinglé / favori** : icône ambre et libellé accessible `Retirer des favoris`.
- **Erreur média** : conserver la zone et afficher le fallback ; aucune card ne doit se décaler au chargement.

## 5. Représentation visuelle du ranking mixte

Le ranking mélange un **score IA éditorial**, la corroboration par plusieurs sources et le signal de la communauté. L'affichage principal doit rester lisible : une note en étoiles et trois marqueurs complémentaires.

```text
┌───────────────────────────────────────────────────────────────────────────┐
│  ★★★★★  8,7 / 10     ◇ 3 sources croisées     ♡ 42 votes lecteurs        │
│  Ambre = score combiné       ◇ = corroboration       ♡ = communauté       │
└───────────────────────────────────────────────────────────────────────────┘
```

### 5.1 Règle d'affichage proposée

- **Étoiles / score combiné** : `★` ambre plein, `☆` contour ; score sur 10 avec une décimale (`8,7`). Le score combiné est présenté comme une aide au tri, jamais comme une vérité.
- **Sources croisées** : icône `◇` vert-sarcelle, suivie du nombre de sources distinctes et indépendantes. Au-delà de 9, afficher `9+`.
- **Votes lecteurs** : icône `♡` bleu pétrole, suivie du nombre de votes ; distinguer « votes » de « note » pour éviter toute confusion.
- **Position** : sur desktop, à droite de la ligne de card ou dans la zone de titre ; sur mobile, sous le titre dans une ligne compacte.

### 5.2 Tooltip au survol ou au focus

```text
                 ┌──────────────────────────────────────────────┐
                 │ RANKING MIXTE                                │
                 │ Score IA éditorial       8,9 / 10            │
                 │ Sources croisées         3 indépendantes    │
                 │ Note lecteurs            4,4 / 5 (42 votes)  │
                 │                                              │
                 │ Score indicatif : pertinence, fraîcheur et   │
                 │ corroboration. En savoir plus →              │
                 └──────────────────────────────────────────────┘
```

Le tooltip apparaît au survol, au focus clavier et au tap prolongé ; il ne doit pas masquer le titre. « En savoir plus » renvoie à la méthode de calcul. Si une donnée manque, afficher `Non disponible` et non `0`.

## 6. Sidebar droite desktop

Visible à partir de 1 120 px environ, la sidebar fait 288–320 px et reste sticky sous le header. Elle accompagne le fil sans le pousser hors de la zone de lecture.

```text
┌───────────────────────────────┐
│ TENDANCES DU JOUR             │
│ 01  Agents autonomes     ↑ 24%│
│ 02  Souveraineté cloud    ↑ 18%│
│ 03  Batteries sodium-ion  ↑ 12%│
│ 04  Régulation IA         →     │
│ [ Voir toutes les tendances ]   │
├───────────────────────────────┤
│ SOURCES ACTIVES                │
│ ● 42 sources surveillées       │
│   18 francophones · 24 monde  │
│   Dernière collecte : 17:20    │
│ [ Gérer les sources ]          │
├───────────────────────────────┤
│ RECEVOIR VIGIE24               │
│ [ S'abonner au RSS global ]    │
│ [ RSS par thème            ▾ ] │
│ [ Recevoir la newsletter    ]  │
└───────────────────────────────┘
```

### 6.1 Blocs

- **Tendances du jour** : 4 à 6 sujets, progression `↑`, recul `↓` ou stable `→`, plus un lien vers le classement détaillé.
- **Sources actives** : volume, répartition francophone/internationale, fraîcheur de collecte et lien de gestion. Ne pas exposer de fausse précision si le dernier passage est en erreur.
- **Abonnement RSS global** : CTA direct, avec URL copiable et confirmation.
- **RSS par thème** : sélecteur multi-choix (ex. IA + Cybersécurité), aperçu du flux généré.
- **Newsletter** : champ email, consentement séparé et fréquence clairement indiquée ; le bloc est visuellement distinct du RSS.

## 7. Vue détail d'un article

La page dédiée recentre l'attention sur un seul signal et permet d'en comprendre la provenance. La source originale reste clairement attribuée ; Vigie24 ne doit pas se substituer à l'article source.

```text
┌───────────────────────────────────────────────────────────────────────────────────────────────┐
│ ← Retour au fil          IA  /  Modèles fondamentaux                         ★ 8,7  ◇ 3  ♡ 42 │
│                                                                                               │
│ Les modèles ouverts changent-ils enfin la donne pour les équipes produit ?                    │
│ 18 août 2026 · Camille Martin · Mis à jour il y a 2 h                                         │
│                                                                                               │
│ ┌─────────────────────────────────────────────────────────────────────────────────────────┐ │
│ │                                      illustration                                          │ │
│ └─────────────────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                               │
│ EN BREF                                                                                       │
│ Une synthèse éditoriale du signal, de son contexte et de ses conséquences pour les équipes.  │
│                                                                                               │
│ SOURCE PRINCIPALE  Tech Horizon   [ Lire l'article ↗ ]                                       │
│                                                                                               │
│ ARTICLES CROISÉS                                                                              │
│  ◇ Le Monde Informatique — angle marché       ◇ Sifted — angle start-up                     │
│                                                                                               │
│ VOTRE AVIS       [ ☆ ☆ ☆ ☆ ☆ ]  [ Envoyer la note ]                                          │
│ TAGS             #open-source  #LLM  #produit                                                  │
└───────────────────────────────────────────────────────────────────────────────────────────────┘
```

La page détail comprend : fil d'Ariane, titre, statut de publication, média principal, résumé éditorial, méthodologie/score dépliable, liens croisés, notation, tags et actions (`Plus tard`, `Favori`, partager). Sur mobile, les articles croisés passent sous le résumé et la source devient un bouton pleine largeur.

## 8. Filtres avancés

Les filtres s'ouvrent depuis le bouton `Filtrer` au-dessus du fil. Ils sont combinables, réinitialisables indépendamment et reflétés dans l'URL pour permettre le partage.

```text
┌──────────────────────────────────────────────┐
│ FILTRES AVANCÉS                         [×]  │
├──────────────────────────────────────────────┤
│ THÈME                                        │
│ [☑ IA] [☐ Climat] [☐ Cybersécurité] [☐ Produit]│
│                                              │
│ PAYS / RÉGION                                │
│ [ Tous les pays                         ▾ ]  │
│                                              │
│ SCORE MINIMUM                                │
│ 0 ├────────────●──────────────────────┤ 10  │
│                                  ≥ 7,0       │
│                                              │
│ TYPE DE SOURCE                               │
│ [☑ Média] [☐ Blog expert] [☐ Recherche]     │
│ [☐ Institution] [☐ Communauté]               │
│                                              │
│ [ Réinitialiser ]             [ Appliquer ]  │
└──────────────────────────────────────────────┘
```

- **Thème** : multi-sélection, avec recherche si la taxonomie s'étend.
- **Pays / région** : France, Europe, Amérique du Nord, Afrique, Asie-Pacifique, International ; prévoir `Non renseigné` pour ne pas exclure silencieusement des articles.
- **Score minimum** : curseur et champ numérique synchronisés, pas de valeur par défaut restrictive.
- **Type de source** : média, blog expert, recherche, institution, communauté ; la source principale et les sources croisées peuvent avoir des types différents.
- Les filtres actifs deviennent des chips au-dessus du fil : `IA ×`, `≥ 7,0 ×`, etc.

## 9. Dark mode — « Mode Radar »

Le Mode Radar est une adaptation de contraste, pas une simple inversion. Il privilégie un fond bleu nuit, les surfaces superposées et les signaux ambre/vert-sarcelle.

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│  ◉ VIGIE24  ·  MODE RADAR       IA   Climat   Cybersécurité     ⌕   ◌   ☾        │
├──────────────────────────────────────────────────────────────────────────────────┤
│  LE 24 DE VIGIE24                                      mardi 18 août 2026         │
│  24 articles · 08 sources croisées             [ Recevoir le flux RSS ]           │
├──────────────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │ IA / Modèles       ★ 8,7   ◇ 3   ♡ 42                                    │  │
│  │ Les modèles ouverts changent-ils enfin la donne ?                          │  │
│  │ Une synthèse des annonces et des limites à court terme.                    │  │
│  │ 18 août · Tech Horizon                     [ Lire ] [ ☆ ] [ ♧ ]             │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### 9.1 Palette et animation

| Rôle | Couleur Mode Radar | Usage |
|---|---|---|
| Fond global | `#0D2027` | canevas, header sombre |
| Surface | `#132E38` | cards, sidebar |
| Surface élevée | `#1B3D47` | tooltip, menu, filtre ouvert |
| Texte principal | `#F2F7F6` | titres et contenu |
| Texte secondaire | `#A9C1BF` | métadonnées, aide |
| Bordure | `#2B5360` | séparateurs |
| Bleu pétrole de marque | `#5E9EAE` | liens et focus adaptés au sombre |
| Vert-sarcelle | `#6CCBB4` | états actifs, corroboration |
| Vert pâle | `#B9E2D7` | surfaces sélectionnées / texte sur accent |
| Ambre | `#F2B755` | ranking, CTA, alertes |

L'animation du logo est discrète : l'œil/point effectue un balayage horizontal de 1,2 s à l'activation puis reste immobile. Respecter `prefers-reduced-motion: reduce` (animation désactivée) ; aucune pulsation infinie ne doit distraire de la lecture.

## 10. Responsive mobile

Le breakpoint de référence est 768 px, avec adaptation intermédiaire vers 1 024 px. La sidebar disparaît sur mobile et ses contenus sont accessibles dans des panneaux ou via le bouton tendances.

```text
┌───────────────────────────────┐
│ ☰   ◉ VIGIE24       ⌕   ◌     │
├───────────────────────────────┤
│ LE 24 DE VIGIE24              │
│ mardi 18 août 2026            │
│ 24 articles · 08 sources      │
│ [ Recevoir le RSS ]           │
├───────────────────────────────┤
│ IA · Modèles        ★ 8,7 ◇ 3 │
│ Les modèles ouverts changent  │
│ enfin la donne pour le produit│
│ Une synthèse des annonces…    │
│ 18 août · Tech Horizon        │
│ [ Lire ]   ☆   ♧   ⋯          │
├───────────────────────────────┤
│ Climat · Énergie     ★ 8,2 ◇ 2│
│ ...                           │
└───────────────────────────────┘
                         ╭─────╮
                         │↗    │  Tendances
                         ╰─────╯
```

- **Card simplifiée** : illustration masquée par défaut ou placée en vignette 72×72 ; résumé limité à 2 lignes ; métadonnées sur une ligne ; actions réduites à `Lire`, favori et menu `⋯`.
- **Menu hamburger** : thèmes, sources, filtres, abonnements et profil ; le panneau est plein écran ou latéral avec focus piégé.
- **Recherche** : icône ouvrant un champ pleine largeur sous le header.
- **Bouton tendances flottant** : bouton rond persistant en bas à droite, libellé accessible `Ouvrir les tendances du jour`; il ouvre la sidebar en bottom sheet.
- **Bandeau du jour** : stats empilées ou en grille 2×2, CTA pleine largeur.
- Le scroll horizontal est interdit ; les titres et boutons doivent rester lisibles à 320 px de large.

## 11. Points ouverts à valider

1. **Position du ranking mixte** : faut-il le garder sur la ligne haute, à droite du titre, ou le déplacer sous le résumé pour mieux expliquer le score ? Décision attendue après test de compréhension sur mobile et desktop.
2. **Densité du fil** : valider une présentation en cards éditoriales (illustration + résumé) ou une vue compacte façon Hacker News (plus d'items visibles, moins de contexte). Option recommandée : cards sur l'accueil, bascule compacte pour les utilisateurs avancés.
3. **Base de calcul des tendances sidebar** : définir le périmètre (volume de mentions, vitesse de croissance, score moyen, clics/lectures, ou combinaison pondérée), la fenêtre temporelle (24 h / 7 jours) et le seuil minimal de sources.
4. **Transparence du ranking** : confirmer les pondérations entre score IA éditorial, corroboration et note lecteurs, ainsi que la politique anti-biais pour les sujets très votés mais peu sourcés.
5. **Taxonomie** : arbitrer le nombre de thèmes de premier niveau et la gouvernance des sous-catégories pour éviter les filtres trop longs.
6. **Abonnements** : choisir si le RSS par thème est généré à la volée ou limité à des flux préconfigurés ; préciser la fréquence et le consentement newsletter.
7. **Sources** : définir le statut « active », le délai acceptable de fraîcheur et le traitement des flux en erreur.

> **Critère de validation design** : une personne qui découvre Vigie24 doit comprendre en moins de 10 secondes ce qui est nouveau aujourd'hui, pourquoi un article est haut classé, d'où il provient et comment le sauvegarder.
