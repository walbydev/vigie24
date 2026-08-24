# Vigie24 — Spécification du projet

## Système d'agrégation et de veille technologique francophone

- **Date du document :** 18 août 2026
- **Statut :** Document vivant, évolutif

## Sommaire

1. [Vision et objectifs](#1-vision-et-objectifs)
2. [Identité du site](#2-identité-du-site)
3. [Structure d'un article (fiche article)](#3-structure-dun-article-fiche-article)
4. [Fréquence de publication](#4-fréquence-de-publication)
5. [Portée géographique et personnalisation](#5-portée-géographique-et-personnalisation)
6. [Diffusion — flux RSS/Atom](#6-diffusion--flux-rssatom)
7. [Gestion des sources](#7-gestion-des-sources)
8. [Règles de découverte de nouvelles sources](#8-règles-de-découverte-de-nouvelles-sources)
9. [Système de ranking mixte](#9-système-de-ranking-mixte)
10. [Architecture technique](#10-architecture-technique)
11. [Charte graphique](#11-charte-graphique)
12. [Points ouverts / à trancher ultérieurement](#12-points-ouverts--à-trancher-ultérieurement)
13. [Prochaines étapes proposées](#13-prochaines-étapes-proposées)

## 1. Vision et objectifs

Vigie24 est un système de veille technologique automatisé couvrant :

- IA
- Technique
- Scientifique
- Recherche
- Développement informatique

Les objectifs et principes retenus sont les suivants :

- Priorité géographique : Suisse > France > Europe > Monde.
- Liste de sources évolutive (acquisition et recherche de nouvelles sources en continu).
- Inclusion de sources vidéo (YouTube) et communautaires (Reddit).
- Fonctionnement en tâche de fond, alimentation quotidienne de thèmes technologiques.
- Priorité à la qualité de l'information et au sourcing rigoureux.
- Mise en parallèle d'articles contradictoires pour illustrer un débat/propos.
- Résumés systématiquement en français ; langues des sources originales conservées : français, anglais, allemand.
- Présentation organisée par thématiques > sous-catégories > sujets/propos, favorisant la réflexion, évitant les effets de gros titres.
- Diffusion via site web + flux RSS/Atom pour abonnement.
- En première approche : l'IA (assistant) est l'unique entité alimentant le site.
- Site francophone en première version, versions internationales envisageables ensuite.

## 2. Identité du site

- **Nom retenu :** Vigie24 (domaine `vigie24.info`) — `vigie.tech` étant déjà pris, nom choisi pour rappeler la veille (vigie) + fonctionnement continu/quotidien (24h).
- **Digest quotidien :** « Le 24 de Vigie24 ».

## 3. Structure d'un article (fiche article)

Chaque fiche article contient les champs suivants :

- Illustration visuelle
- Titre
- Date
- Auteur(s) (nom, fonction)
- Source(s) avec lien
- Résumé (en français)
- Lien(s) vers l'original

Fonctionnalités associées :

- Système de ranking de 0 (pas intéressant) à 5 (très intéressant).
- Marquage « lecture ultérieure ».
- Marquage « favori ».
- Un même article peut apparaître dans plusieurs thèmes/sous-catégories selon les sujets traités.

## 4. Fréquence de publication

- 1 digest quotidien (« Le 24 de Vigie24 »).

## 5. Portée géographique et personnalisation

- Site francophone unique (pas de sites séparés par pays/région).
- Détection de géolocalisation + préférence manuelle utilisateur.
- Réordonnancement dynamique du contenu selon le pays (pas de filtrage/masquage, juste réordonnancement).
- Quota souple de visibilité minimum pour régions à faible volume de contenu (Suisse, Belgique, Québec) afin qu'elles ne soient pas noyées.
- Ordre de priorité par défaut : Suisse > France > Belgique > Québec > Francophonie élargie > Europe > Monde.

### Exemple illustratif du mécanisme

Même corpus d'articles du 18/08/2026, réordonné différemment selon vue Suisse / vue France / vue Québec : le meilleur article local remonte en tête grâce au quota, même si son ranking éditorial brut est inférieur à un article international.

### Point encore ouvert

Faut-il que le ranking éditorial prime toujours sur la localité (quota ne s'appliquant qu'en cas d'égalité), ou garder le quota souple tel que décrit ? Pour l'instant, réponse « ok » donnée sans trancher formellement ; mécanisme de quota souple retenu par défaut.

## 6. Diffusion — flux RSS/Atom

- Génération de flux RSS 2.0 **et** Atom en sortie.
- Génération technique via `System.ServiceModel.Syndication` (natif .NET).

### Granularité des flux

- Flux global « Le 24 de Vigie24 ».
- Flux par thème (IA, Dev, etc.).
- Flux par pays/région.
- Flux personnalisé par utilisateur connecté (thèmes suivis + pays préféré).

### Exemples d'URLs

- `vigie24.info/rss/ia`
- `vigie24.info/rss/ch`

## 7. Gestion des sources

### 7.1 Stockage

PostgreSQL (pas de fichier de config statique), pour permettre modification/ajout manuel sans redéploiement, historique de fiabilité, relations many-to-many propres avec thèmes/pays.

### 7.2 Schéma SQL proposé

```sql
CREATE TABLE sources (
    id SERIAL PRIMARY KEY,
    nom VARCHAR(200) NOT NULL,
    url_flux VARCHAR(500),
    url_site VARCHAR(500),
    type VARCHAR(50),
    pays VARCHAR(10)[],
    themes INT[],
    langue VARCHAR(10),
    statut VARCHAR(20) DEFAULT 'actif',
    score_fiabilite DECIMAL(3,2) DEFAULT 0.50,
    date_ajout TIMESTAMP DEFAULT NOW(),
    derniere_verification TIMESTAMP,
    notes TEXT
);

CREATE TABLE themes (
    id SERIAL PRIMARY KEY,
    nom VARCHAR(100),
    theme_parent_id INT REFERENCES themes(id)
);
```

### 7.3 Liste de sources de départ (échantillon initial, à compléter)

#### Tableau IA

| Source | Pays | Type | Notes |
|---|---|---|---|
| EPFL / MILA / INRIA (blogs recherche) | CH/CA/FR | RSS | Recherche académique |
| ICTjournal | CH | RSS | Presse tech suisse |
| Les Échos Tech, Numerama | FR | RSS | Presse généraliste tech |
| L'Écho, DataNews | BE | RSS | Presse tech belge |
| La Presse, Le Devoir (tech) | QC | RSS | Presse québécoise |
| Hacker News (filtré IA) | Monde | API | Discussions communautaires |
| r/MachineLearning | Monde | API Reddit | Communauté technique |
| OpenAI Blog, Anthropic Blog, Mistral Blog | Monde | RSS | Sources primaires labs |
| ArXiv (cs.AI, cs.CL) | Monde | API | Papers académiques |

#### Tableau Développement

| Source | Pays | Type |
|---|---|---|
| Dev.to, Hacker News | Monde | API/RSS |
| Les blogs Microsoft/GitHub | Monde | RSS |
| ICTjournal, Netzwoche | CH | RSS |
| Silicon.fr, JDN | FR | RSS |

> **Note :** liste non exhaustive, à compléter thème par thème dans un document ultérieur dédié.

### 7.4 Interface d'administration

Page protégée avec CRUD simple : liste des sources, formulaire d'ajout, toggle actif/inactif, édition inline.

## 8. Règles de découverte de nouvelles sources

Quatre mécanismes complémentaires, avec validation humaine obligatoire avant activation (pas d'ajout automatique en prod) :

- **Règle A — Suggestion par citation croisée :** si un domaine non répertorié est cité/linké fréquemment par des articles agrégés (seuil ex. 3 citations/30 jours), proposition en file d'attente.
- **Règle B — Suggestion par recherche thématique périodique :** job planifié mensuel effectuant des recherches web ciblées (ex. `"actualité IA" site:.ch`), propose des candidats.
- **Règle C — Suggestion communautaire :** formulaire « Proposer une source » pour les lecteurs, passe en file de modération.
- **Règle D — Suggestion par IA :** analyse périodique (mensuelle) des tendances du secteur par l'assistant IA, proposition motivée de nouvelles sources.

Toutes les suggestions tombent dans une table `sources_candidates` avec statut `en_attente`, validation manuelle obligatoire dans l'admin.

### Checklist de validation avant activation d'une source

- Fréquence de publication suffisante.
- Langue française (ou traduisible proprement).
- Absence de conflit d'intérêt évident / contenu publicitaire déguisé.
- Historique d'au moins 5 articles vérifiables.
- Score de fiabilité initial fixé à 0.5, ajusté ensuite selon performance.

## 9. Système de ranking mixte

### 9.1 Composantes de notation

- **Score IA :** sélection + score initial basé sur qualité/pertinence/nombre de sources croisées.
- **Score de croisement :** nombre de médias indépendants traitant du même sujet.
- **Score lecteurs :** notation communautaire (1-5 étoiles).

### 9.2 Affichage et formule

Décision retenue : affichage des 3 scores séparément + 1 score combiné par défaut affiché en priorité.

Formule du score combiné par défaut (pondération ajustable en configuration sans redéploiement) :

```text
Score_affiché = (0.4 × Score_IA) + (0.3 × Score_croisement) + (0.3 × Score_lecteurs)
```

Le détail des 3 composantes est disponible au clic (« voir le détail du ranking »).

### 9.3 Cas particuliers gérés

- Moins de 5 votes lecteurs → affichage du score IA+croisement seul avec badge « nouveau ».
- Écart de plus de 2 étoiles entre IA et lecteurs → indicateur visuel « Avis partagés » invitant à la discussion/confrontation.

### 9.4 Système de vote lecteur

Notation 1-5 étoiles, un vote par utilisateur connecté par article, anti-brigading basique (limite votes/IP/jour, détection de patterns anormaux).

## 10. Architecture technique

### 10.1 Stack retenue

- **Backend + Frontend unifiés :** ASP.NET Core (LTS, .NET 10 attendu en 2026).
- **Frontend :** Blazor Server ou Blazor WebAssembly recommandé (rester 100% C#, Blazor Server plus simple à déployer sur VPS unique) ; alternative envisageable : Razor Pages + htmx si besoin de plus de légèreté (à trancher selon confort de l'utilisateur).
- **Base de données :** PostgreSQL (choix utilisateur validé) — arrays natifs utiles pour tags pays/thèmes, JSON pour flexibilité.
- **ORM :** Entity Framework Core.

### 10.2 Agrégation / scraping

Service séparé : Worker Service .NET (jobs en arrière-plan) :

- Polling RSS/Atom via bibliothèque type CodeHollow.FeedReader.
- Scraping ciblé si pas de flux RSS disponible (HtmlAgilityPack ou AngleSharp).
- Appels API externes (Reddit API, YouTube Data API, ArXiv API).

Orchestration des jobs planifiés : Hangfire (dashboard de monitoring intégré, adapté à un déploiement Docker), plutôt qu'un cron brut.

### 10.3 Génération RSS/Atom

`System.ServiceModel.Syndication` (natif .NET).

### 10.4 Déploiement

VPS dédié de l'utilisateur, conteneurisation Docker.

```yaml
services:
  vigie24-web:        # ASP.NET Core Blazor
  vigie24-worker:      # Worker Service agrégation
  vigie24-db:          # PostgreSQL
  vigie24-hangfire:    # Dashboard jobs (peut être intégré au worker)
```

### 10.5 Intégration IA

Appel à une API IA (ex. Claude, ou autre) depuis le Worker Service pour :

- Résumer les articles.
- Détecter les doublons / croisements de sources.
- Attribuer le score initial.
- Classer par thème / sous-thème.

## 11. Charte graphique

**Concept directeur :** « Le sérieux qui garde un œil qui pétille » — neutralité, sérieux, apaisement, avec une touche discrète de délire geek technologique.

### 11.1 Palette de couleurs

```text
Fond principal:    #F7F8FA (blanc cassé, doux, pas agressif)
Texte principal:   #1A1D23 (anthracite, pas noir pur - plus doux)
Accent primaire:   #2D5F7C (bleu pétrole profond - sérieux, neutre, "vigie")
Accent secondaire: #4A9B8E (vert-sarcelle doux - touche apaisée)
Highlight/CTA:     #E8A33D (ambre doux - le "délire geek" avec parcimonie)
Alertes/confrontation: #C4634A (terracotta, jamais rouge vif - reste apaisé)
```

### 11.2 Typographie

- **Titres :** Space Grotesk ou IBM Plex Sans (geek assumé, lisible, professionnel).
- **Corps de texte :** Inter ou Source Sans Pro (sobriété, lisibilité longue lecture).
- **Métadonnées techniques (dates, scores, tags) :** JetBrains Mono (clin d'œil dev discret).

### 11.3 Logo — concept

- Loupe/œil stylisé en forme de « 24 », ou radar/sonar minimaliste (cercles concentriques évoquant la vigie bienveillante).
- Style : ligne fine, monochrome bleu pétrole, pas de gradient criard.
- Le « 24 » (heures) intégré subtilement dans la forme.

### 11.4 Éléments d'interface

- Icônes de thème en style outline (type Lucide/Feather) plutôt qu'emoji en production.
- Micro-animations discrètes (fade-in des cards, pas de mouvement excessif).
- Easter egg envisagé : mode sombre nommé « Mode Radar ».

### 11.5 Ambiance générale de référence

S'inspirer de la sobriété SaaS moderne (type Stripe, Linear) plutôt que d'un style trop chargé (type TechCrunch) ou trop austère/brut (type Hacker News).

## 12. Points ouverts / à trancher ultérieurement

- Arbitrage définitif entre priorité au ranking éditorial vs quota souple géographique (voir section 5).
- Choix final entre Blazor Server, Blazor WebAssembly, ou Razor Pages + htmx pour le frontend.
- Choix du fournisseur d'API IA pour l'intégration en production.
- Liste complète et exhaustive des sources par thème (au-delà de l'échantillon de la section 7.3).
- Schéma de base de données complet (au-delà des tables sources/themes déjà spécifiées) : articles, utilisateurs, votes, ranking détaillé.

## 13. Prochaines étapes proposées (non encore réalisées à la date du document)

- Schéma de base de données complet (toutes les tables : articles, sources, thèmes, users, votes, ranking).
- Liste de sources complète, exhaustive, thème par thème (document séparé).
- Mockup visuel textuel plus poussé de la page d'accueil avec la charte graphique appliquée.
