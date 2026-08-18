# Vigie24 — Système d'Agrégation et de Veille Technologique Francophone

![Vigie24](https://via.placeholder.com/400x100/1B4B5A/FFFFFF?text=VIGIE24)

**Vigie24** est un agrégateur intelligent de veille technologique francophone qui surveille, analyse et synthétise les signaux les plus pertinents dans les domaines de l'IA, du développement informatique, de la science, de la cybersécurité et du hardware.

🌐 **Site web** : [vigie24.info](https://vigie24.info) (à venir)  
📰 **Digest quotidien** : « Le 24 de Vigie24 »  
📡 **Flux RSS** : Disponible par thème et région

---

## 🎯 Vision et Objectifs

Vigie24 répond au besoin croissant de **veille technologique de qualité** dans l'espace francophone, avec une attention particulière portée à :

- **Intelligence Artificielle** : modèles, recherche, applications
- **Développement informatique** : pratiques, outils, architectures
- **Science et recherche** : découvertes, publications académiques
- **Cybersécurité** : alertes, vulnérabilités, bonnes pratiques
- **Hardware** : semi-conducteurs, composants, infrastructure

### Principes directeurs

- **Priorité géographique** : Suisse 🇨🇭 > France 🇫🇷 > Belgique 🇧🇪 > Québec 🇨🇦 > Europe 🇪🇺 > Monde 🌍
- **Qualité avant quantité** : sélection rigoureuse, sourcing vérifié
- **Neutralité éditoriale** : mise en perspective de sources contradictoires
- **Transparence** : scores de ranking explicables, sources citées
- **Francophonie inclusive** : contenu en français, sources originales en FR/EN/DE

---

## 🏗️ Architecture Technique

### Stack technologique

| Composant | Technologie | Justification |
|-----------|-------------|---------------|
| **Backend + Frontend** | ASP.NET Core (.NET 10) | Unification, performance, écosystème mature |
| **Frontend** | Blazor Server | 100% C#, déploiement simplifié sur VPS unique |
| **Base de données** | PostgreSQL 15+ | Arrays natifs, JSONB, intégrité référentielle |
| **ORM** | Entity Framework Core | Productivité, migrations, requêtes type-sécurisées |
| **Jobs planifiés** | Hangfire | Dashboard intégré, persistance, retries |
| **Ingestion RSS** | CodeHollow.FeedReader | Bibliothèque .NET éprouvée |
| **Scraping** | AngleSharp | Alternative moderne à HtmlAgilityPack |
| **Génération RSS** | System.ServiceModel.Syndication | Natif .NET, compatibilité maximale |

### Conteneurisation

Le projet est déployé via Docker avec les services suivants :

```yaml
services:
  vigie24-web:      # ASP.NET Core Blazor — interface utilisateur
  vigie24-worker:   # Worker Service — agrégation et traitement IA
  vigie24-db:       # PostgreSQL — stockage persistant
  vigie24-hangfire: # Dashboard de monitoring des jobs (intégré au worker)
```

### Intégration IA

Un assistant IA (Claude, ou équivalent) est utilisé pour :
- **Résumer** les articles dans un français naturel
- **Détecter** les doublons et sujets similaires
- **Croiser** les sources pour vérifier la corroboration
- **Classer** les articles par thème et sous-thème
- **Attribuer** un score initial de pertinence éditoriale

---

## 🗃️ Structure de la Base de Données

Le schéma PostgreSQL est conçu pour :
- **L'évolutivité** : UUID non séquentiels, prêts pour une architecture distribuée
- **L'internationalisation** : tables de traduction séparées (`*_translations`)
- **La traçabilité** : logs d'ingestion complets avec métadonnées JSONB
- **Le ranking** : scores matérialisés pour performance des requêtes

### Tables principales

| Table | Description |
|-------|-------------|
| `users` | Comptes lecteurs, préférences, authentification |
| `sources` | Référentiel des médias, blogs, chaînes YouTube, communautés Reddit |
| `categories` | Thèmes hiérarchiques (IA, Dev, Science, Cyber, Hardware) |
| `tags` | Mots-clés libres réutilisables (#LLM, #open-source, etc.) |
| `authors` | Auteurs normalisés, liés aux articles |
| `articles` | Entité éditoriale canonique — un événement = un article |
| `article_sources` | Liaison many-to-many article↔source (corroboration) |
| `article_authors` | Auteurs d'un article (multiple, avec ordre) |
| `article_tags` | Tags associés à un article |
| `votes` | Notation 1-5 étoiles par utilisateur connecté |
| `bookmarks` | Favoris personnels des utilisateurs |
| `subscriptions` | Abonnements newsletter et RSS |
| `daily_digests` | « Le 24 de Vigie24 » — digest quotidien par locale |
| `digest_articles` | Articles inclus dans un digest, avec position éditoriale |
| `ingestion_logs` | Historique complet des collectes de sources |
| `locales` | Référentiel des langues supportées (fr-FR, en-US, etc.) |

### Schéma conceptuel

```
users ──< votes >── articles ──< article_sources >── sources ──< ingestion_logs
  │                    │  │                           │
  │                    │  └──< article_authors >── authors
  │                    │
  │                    ├──< article_tags >── tags
  │                    ├── belongs_to ── categories (hiérarchique)
  │                    ├──< bookmarks >── users
  │                    └──< digest_articles >── daily_digests
  │
  └──< subscriptions

articles ──< article_translations >── locales
categories ──< category_translations >── locales
tags ──< tag_translations >── locales
sources ──< source_translations >── locales
```

---

## 📊 Système de Ranking Mixte

Le score affiché combine **trois composantes indépendantes** :

```
Score_affiché = (0.40 × Score_IA) + (0.25 × Score_algorithmique) + 
                (0.20 × Score_lecteurs) + (0.15 × Score_croisement)
```

### Composantes détaillées

| Composante | Poids | Description |
|------------|-------|-------------|
| **Score IA éditorial** | 40% | Pertinence, fraîcheur, qualité du résumé attribué par l'IA |
| **Score algorithmique** | 25% | Facteurs techniques (fraîcheur, diversité des sources, etc.) |
| **Score lecteurs** | 20% | Moyenne bayésienne des votes 1-5 étoiles (anti-bruit pour nouveaux articles) |
| **Score croisement** | 15% | Nombre de sources indépendantes couvrant le même événement |

### Affichage

- **Étoiles ambre** : score combiné sur 10 (ex: ★★★★★ 8,7/10)
- **Icône ◇** : nombre de sources croisées (ex: ◇ 3)
- **Icône ♡** : nombre de votes lecteurs (ex: ♡ 42)

Le détail complet est accessible au survol/tooltip.

---

## 🌐 Sources

Vigie24 agrège **plus de 100 sources** dans 5 catégories principales :

### 1. Intelligence Artificielle

- **Labs de recherche** : OpenAI, Anthropic, Google DeepMind, Mistral AI, Meta AI, Microsoft Research, Hugging Face, NVIDIA, IBM, Apple ML
- **Institutions académiques** : EPFL, ETH Zürich, INRIA, CNRS, Mila (Québec), Max Planck, Stanford HAI, Berkeley AI
- **Presse spécialisée** : ActuIA, Le Monde Informatique, Numerama, MIT Technology Review, VentureBeat AI
- **Académique** : arXiv (cs.AI, cs.CL, cs.LG, stat.ML), Papers with Code, Semantic Scholar, HAL
- **YouTube** : Two Minute Papers, Yannic Kilcher, AI Explained, Underscore_, Science Étonnante
- **Reddit** : r/MachineLearning, r/artificial, r/LocalLLaMA, r/singularity

### 2. Développement informatique

- **Presse suisse** : ICTjournal, Netzwoche, inside-it.ch, Le Temps, RTS Sciences
- **Presse française** : Numerama, Next INpact, Clubic, JDN, Le Monde Informatique, 01net, Silicon.fr
- **Presse belge/québécoise** : RTBF, Le Soir, L'Echo, Data News, Le Devoir, Radio-Canada
- **Internationale** : TechCrunch, The Verge, Ars Technica, Hacker News, InfoQ, WIRED, IEEE Spectrum
- **Blogs développeurs** : DEV Community, Stack Overflow Blog, GitHub Blog, Martin Fowler, AWS Architecture Blog
- **YouTube** : Fireship, ThePrimeagen, Grafikart, Underscore_, Computerphile
- **Reddit** : r/programming, r/technology, r/devops, r/webdev, r/opensource

### 3. Science et recherche

- **Institutions** : CNRS, INRIA, EPFL, ETH Zürich, CERN, Max Planck, ESA, MILA
- **Presse scientifique** : Pour la Science, La Recherche, Science & Vie, Nature News, Science Magazine, Quanta Magazine
- **YouTube** : Science Étonnante, Le Vortex (ARTE), DirtyBiology, Veritasium, Kurzgesagt, 3Blue1Brown
- **Reddit** : r/science, r/askscience, r/Physics, r/space

### 4. Cybersécurité

- **Autorités** : NCSC Suisse, CERT-FR/ANSII, CERT-EU, ENISA, CISA, NVD (CVE), MITRE ATT&CK
- **Spécialisée** : LeMagIT Sécurité, ZATAZ, Krebs on Security, The Hacker News, BleepingComputer, Dark Reading, Schneier on Security
- **Reddit** : r/netsec, r/cybersecurity, r/AskNetsec

### 5. Hardware et semi-conducteurs

- **Presse** : Tom's Hardware, Les Numériques, Ars Technica Hardware, ServeTheHome, IEEE Spectrum, SemiAnalysis
- **Fabricants** : TSMC, ASML, Arm, RISC-V International
- **Reddit** : r/hardware, r/buildapc, r/embedded

> **Note** : La liste complète des sources avec URLs, statuts et méthodes d'ingestion est disponible dans [`docs/plan/vigie24-sources.md`](docs/plan/vigie24-sources.md).

---

## 🎨 Charte Graphique

### Concept directeur

> « Le sérieux qui garde un œil qui pétille » — neutralité, sérieux, apaisement, avec une touche discrète de délire geek technologique.

### Palette de couleurs

| Rôle | Couleur (clair) | Couleur (Mode Radar) |
|------|-----------------|----------------------|
| **Fond principal** | `#F7F8FA` | `#0D2027` |
| **Surface** | `#FFFFFF` | `#132E38` |
| **Texte principal** | `#1A1D23` | `#F2F7F6` |
| **Bleu pétrole (marque)** | `#2D5F7C` | `#5E9EAE` |
| **Vert-sarcelle (positif)** | `#4A9B8E` | `#6CCBB4` |
| **Ambre (accent/CTA)** | `#E8A33D` | `#F2B755` |
| **Terracotta (alerte)** | `#C4634A` | `#D9765E` |

### Typographie

- **Titres** : Space Grotesk (geek assumé, professionnel)
- **Corps de texte** : Inter (sobriété, lisibilité longue lecture)
- **Métadonnées techniques** : JetBrains Mono (clin d'œil dev discret)

### Mode sombre — « Mode Radar »

Le mode sombre est une adaptation de contraste, pas une simple inversion. Il privilégie un fond bleu nuit avec des signaux ambre/vert-sarcelle adaptés.

---

## 📱 Interface Utilisateur

### Page d'accueil

- **Header sticky** : logo, navigation par thèmes, recherche, profil, toggle Mode Radar
- **Bandeau du jour** : « Le 24 de Vigie24 » avec stats du jour (articles, thèmes, sources croisées, tendances)
- **Fil principal** : cards d'articles avec illustration, titre, résumé, métadonnées, ranking mixte, actions (Lire, Plus tard, Favori, Noter)
- **Sidebar droite** : tendances du jour, sources actives, abonnements RSS/Newsletter

### Vue détail d'article

- Illustration pleine largeur
- Résumé éditorial (« EN BREF »)
- Source principale avec lien direct
- Articles croisés (autres sources couvrant le même sujet)
- Système de notation 1-5 étoiles
- Tags et métadonnées complètes

### Responsive

- **Desktop** : layout 3 colonnes (sidebar gauche optionnelle, fil central, sidebar droite)
- **Tablette** : layout 2 colonnes, sidebar droite intégrée dans un panneau
- **Mobile** : fil plein écran, sidebar dans un bottom sheet, menu hamburger

---

## 🚀 Fonctionnalités Clés

### 1. Digest quotidien « Le 24 de Vigie24 »

Chaque jour, une sélection éditoriale de **~24 articles** les plus pertinents, organisée par thèmes, avec une introduction contextuelle.

### 2. Personnalisation géographique

Détection automatique du pays + préférence manuelle. Le contenu est **réordonné** (pas filtré) selon la priorité :
1. Suisse 🇨🇭
2. France 🇫🇷
3. Belgique 🇧🇪
4. Québec 🇨🇦
5. Francophonie élargie
6. Europe 🇪🇺
7. Monde 🌍

Un **quota souple** garantit une visibilité minimale pour les régions à faible volume.

### 3. Flux RSS/Atom granulaires

- **Flux global** : `vigie24.info/rss/global`
- **Par thème** : `vigie24.info/rss/ia`, `vigie24.info/rss/dev`, etc.
- **Par pays** : `vigie24.info/rss/ch`, `vigie24.info/rss/fr`, etc.
- **Personnalisé** : pour les utilisateurs connectés (thèmes suivis + pays préféré)

### 4. Découverte de nouvelles sources

Quatre mécanismes complémentaires, tous avec **validation humaine obligatoire** :

- **Règle A** : Suggestion par citation croisée (seuil : 3 citations/30 jours)
- **Règle B** : Recherche thématique périodique (mensuelle)
- **Règle C** : Formulaire « Proposer une source » pour les lecteurs
- **Règle D** : Analyse par IA des tendances du secteur

### 5. Administration

Interface protégée avec CRUD simple pour :
- Liste des sources
- Ajout/édition de sources
- Toggle actif/inactif
- Édition inline des métadonnées
- File d'attente des sources candidates

---

## 📂 Structure du Projet

```
vigie24/
├── docs/
│   └── plan/
│       ├── vigie24.md                 # Spécification principale
│       ├── vigie24-database-schema.md # Schéma PostgreSQL complet
│       ├── vigie24-mockup.md          # Mockup visuel détaillé
│       └── vigie24-sources.md         # Liste exhaustive des sources
├── src/
│   ├── Vigie24.Web/                   # ASP.NET Core Blazor (UI)
│   ├── Vigie24.Worker/                # Worker Service (agrégation)
│   ├── Vigie24.Domain/                # Entités, interfaces, règles métier
│   ├── Vigie24.Infrastructure/        # EF Core, repositorios, ingestion
│   └── Vigie24.Services/              # Services IA, ranking, RSS
├── tests/
│   ├── Vigie24.UnitTests/
│   └── Vigie24.IntegrationTests/
├── scripts/
│   ├── seed_sources.py                # Initialisation des sources
│   └── verify_feeds.sh                # Vérification des flux RSS
├── docker-compose.yml
├── Dockerfile
├── README.md
└── ...
```

---

## 🛠️ Installation et Développement

### Prérequis

- .NET 10 SDK (ou version LTS la plus récente)
- PostgreSQL 15+
- Docker + Docker Compose (pour le déploiement)
- Clé API pour l'assistant IA (Claude ou équivalent)

### Démarrage rapide

```bash
# 1. Cloner le repository
git clone https://github.com/philippe-rjf/vigie24.git
cd vigie24

# 2. Lancer la stack Docker
docker-compose up -d

# 3. Initialiser la base de données
dotnet ef database update --project src/Vigie24.Infrastructure

# 4. Seed des sources initiales
python scripts/seed_sources.py

# 5. Lancer l'application de développement
dotnet run --project src/Vigie24.Web
```

### Configuration

Les paramètres sensibles (clés API, connexions DB) doivent être stockés dans :
- Variables d'environnement
- Fichier `appsettings.Production.json` (non versionné)
- Azure Key Vault / HashiCorp Vault (production)

---

## 📝 Roadmap

### Phase 1 — MVP (courant)

- [x] Spécifications complètes
- [x] Schéma de base de données
- [x] Liste des sources initiales
- [x] Mockup visuel
- [ ] Structure du projet .NET
- [ ] Ingestion RSS de base
- [ ] Génération de résumés IA
- [ ] Affichage des articles (Blazor)
- [ ] Système de votes et bookmarks
- [ ] Flux RSS/Atom

### Phase 2 — Fonctionnalités avancées

- [ ] Détection de doublons sémantiques
- [ ] Calcul du ranking mixte
- [ ] Personnalisation géographique
- [ ] Mode Radar (dark mode)
- [ ] Recherche plein texte
- [ ] Authentification utilisateurs
- [ ] Newsletter quotidienne

### Phase 3 — Expansion

- [ ] Sources supplémentaires (completer la liste)
- [ ] API publique pour développeurs
- [ ] Applications mobiles (MAUI / Flutter)
- [ ] Versions internationales (en, de)
- [ ] Analytics et tableaux de bord

---

## 🤝 Contribution

Vigie24 est un projet **open source**. Les contributions sont les bienvenues !

### Comment contribuer

1. Fork le repository
2. Créer une branche pour votre feature (`git checkout -b feature/amazing-feature`)
3. Commit vos changements (`git commit -m 'Add amazing feature'`)
4. Push vers la branche (`git push origin feature/amazing-feature`)
5. Ouvrir une Pull Request

### Zones prioritaires

- **Ingestion** : améliorer la détection de doublons, ajouter des connecteurs API
- **IA** : affiner les prompts de résumé, réduire les hallucinations
- **UI/UX** : améliorer l'accessibilité, optimiser le mobile
- **Sources** : proposer de nouvelles sources validées (voir docs/plan/vigie24-sources.md)

---

## 📄 Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour les détails.

---

## 🙏 Remerciements

- **Communauté francophone** : pour l'inspiration et les retours
- **Sources primaires** : tous les médias, laboratoires et créateurs dont nous agrégons le contenu
- **Outils open source** : .NET, PostgreSQL, Hangfire, et tous les packages utilisés

---

## 📞 Contact

- **Auteur** : Philippe Ryf
- **Email** : philippe@walby.dev
- **Projet** : [GitHub](https://github.com/philippe-rjf/vigie24)

---

**Vigie24** — *La veille technologique, enfin claire.*
