# Vigie24 — Architecture

> Document compagnon : [algorithmes.fr.md](algorithmes.fr.md) détaille toutes les
> formules de scoring et de rapprochement. Le schéma de base de données est dans
> [design/database-schema.fr.md](design/database-schema.fr.md).

## 1. Vue d'ensemble

Vigie24 est un système de veille RSS intelligent pour l'actualité technologique
francophone. Il agrège 214 sources vérifiées (208 actives ; presse, blogs, chaînes YouTube,
communautés Reddit), analyse chaque article avec un LLM, publie le meilleur
contenu sur un site Blazor Server et expose des flux RSS granulaires — le tout en
français, la langue du produit.

```
┌────────────┐   ┌──────────────┐   ┌───────────┐   ┌──────────────┐
│  RSS/Atom  │──▶│              │──▶│           │──▶│  PostgreSQL  │
│  YouTube   │──▶│    Worker    │   │  APIs IA  │   │     16       │
│  Reddit    │──▶│ (boucle 15 m)│   │ Infomaniak│   └──────┬───────┘
└────────────┘   │              │   │ DeepSeek  │          │
                 └──────────────┘   └───────────┘          ▼
                                              ┌─────────────────────┐
                                              │  Web (Blazor Server)│
                                              │  + flux RSS + sitemap
                                              └─────────────────────┘
```

## 2. Structure de la solution

| Projet | Responsabilité |
|---|---|
| `Vigie24.Domain` | Entités, formules de scoring, taxonomie, moteur de débats, builders RSS/sitemap, coûts et santé — **aucune dépendance d'infrastructure** |
| `Vigie24.Services` | Abstraction IA (`IAiService`), fournisseurs (Infomaniak, DeepSeek, mock), factory |
| `Vigie24.Infrastructure` | EF Core + Npgsql, `DbContext`, migrations (via `DesignTimeDbContextFactory`), requêtes (filtrage, service RSS, rapports de coûts) |
| `Vigie24.Services` | Abstraction IA (`IAiService`), fournisseurs (Infomaniak, DeepSeek, mock), factory |
| `Vigie24.Web` | Frontend Blazor Server (prerender SSR, SEO, filtres, endpoints d'engagement, mode zen) |
| `Vigie24.Worker` | `BackgroundService` : polling RSS + pipeline d'analyse IA |
| `tests/Vigie24.Tests` | Tests unitaires, fonctionnels et d'intégration |

## 3. Composants d'exécution

### 3.1 Worker — le pipeline d'ingestion

Un `BackgroundService` qui se réveille toutes les `FeedReader:PollingIntervalMinutes`
(défaut **15 min**, 20 min en staging/production). Chaque cycle :

1. Charge les sources actives en base (requête suivie — `LastIngestedAt` doit être persisté).
2. Ignore les sources pas encore dues (`SourcePollingSchedule`) : YouTube/Reddit sont
   pollés à des créneaux fixes UTC **08:00/20:00** (2×/jour) ; RSS/Atom sur intervalle
   régulier (`FeedReader:RssPollingIntervalMinutes`, repli sur `PollingIntervalMinutes`).
   Une source jamais récupérée (ex. fraîchement réactivée) est toujours due — rattrapage.
3. Pour chaque source due, récupère son flux (RSS/Atom via `SyndicationFeed`, ou YouTube/Reddit via leurs API), plafonné par `FeedReader:MaxItemsPerFeed` (défaut 15).
4. Traite chaque élément : dédup exacte par URL canonique → dédup sémantique → analyse IA → persistance (voir [algorithmes.fr.md](algorithmes.fr.md)).
5. Met à jour `LastIngestedAt`, puis reconstruit les débats.

Le worker est résilient : une source en échec est journalisée et ignorée, un
article en échec est ignoré, et les échecs d'embedding sont des avertissements non
bloquants.

**Politique d'échecs de sources.** `SourceFailurePolicy` classe les échecs de
récupération : seuls les échecs **définitifs** (404/410, XML invalide, DNS mort)
peuvent désactiver une source ; les erreurs transitoires (429, 403, 5xx, timeouts)
jamais. Les échecs déclenchent un backoff exponentiel (5 min → 6 h par source, suivi
via `LastFetchAttemptAt`) ; après `FeedReader:MaxFetchFailures` (défaut 8) échecs
définitifs, la source est désactivée (`DisabledAt`). `ProbeDisabledSourcesAsync`
re-teste les sources désactivées après `FeedReader:RecoveryProbeAfterHours` (24 h) et
les réactive automatiquement — la sonde ne récupère que le flux, sans coût IA.

### 3.2 Web — Blazor Server

ASP.NET Core Blazor Server avec rendu `ServerPrerendered` (HTML initial
indexable). Surfaces clés :

- **Pages** : `/` (navigateur d'articles), `/articles`, `/categories`, `/themes`, `/debats` + `/debats/{id}` (dossiers vivants), `/briefing`, `/radar`, `/top`, `/il-y-a-un-an`, `/journal`, `/barometre`, `/articles/{id}` (mode lecture), `/sources` (rapport d'échecs + réactivation), `/projet`, `/couts`, `/charte-graphique`, `/mentions-legales`, `/confidentialite`, `/licence`.
- **Endpoints RSS** : `/feed` (global) et `/categories/{slug}/feed` (par catégorie) — construits par `RssFeedService`, `Cache-Control: public, max-age=300`.
- **Mode zen** : filtre d'anxiété global (toggle de nav, persisté en localStorage + cookie) via `ZenModeService` ; `/feed/zen` expose un flux RSS anxiété ≤ 6.
- **Helpers partagés** : `ArticlePalette` (palette de couleurs par catégorie pour les cartes et vignettes), `HealthCheckService` (âge de l'ingestion + ping du fournisseur IA pour la status-card d'accueil), `SitemapBuilder` (sitemap XML).
- **SEO** : `sitemap.xml` dynamique, `robots.txt`, Open Graph / Twitter Cards, JSON-LD (WebSite, Organization, BreadcrumbList).
- **Engagement** : votes anonymes (0–5) et suivi des clics, persistés et intégrés au classement.

### 3.3 Base de données — PostgreSQL 16

EF Core + Npgsql, tables créées par migrations. Principaux agrégats : `articles`
(flux + résultats IA + scores), `sources`, `categories`, `tags` + `article_tags`,
`article_embeddings` (vecteurs sémantiques), `debates` + `debate_articles`,
`authors`, métriques d'engagement et usage de tokens par article. `sources` porte des
colonnes de suivi d'échec (`DisabledAt`, `LastFetchAttemptAt`). Schéma complet :
[design/database-schema.fr.md](design/database-schema.fr.md).

## 4. Modèle de configuration

Règle d'or :

- **`appsettings.json` + `appsettings.{Env}.json`** — toute la configuration non sensible (polling, scores, modèles, timeouts, URLs). Versionnée.
- **`environments/<env>/.env`** — secrets uniquement (clés API, mot de passe DB) + variables de substitution compose. Jamais commité ; les gabarits `.env.example` sont versionnés.
- Les surcharges par environnement sont **montées en volume** dans les conteneurs (pas de rebuild d'image pour changer une config).

Environnements : **Development** (IA réelles, port 5000), **Mock** (IA simulées, tests), **Staging** (préproduction sur un hôte LAN), **Production** (VPS public). Chacun vit dans un dossier autonome `environments/<env>/` avec son propre `docker-compose.yml`.

## 5. Abstraction des fournisseurs IA

`IAiService` (dans `Vigie24.Services`) définit : `ProcessArticleAsync` (résumé,
score éditorial, thèmes, catégories, mots-clés, topic, stance, score d'anxiété),
`IsDuplicateAsync` (dédup sémantique), `GetEmbeddingAsync` (vecteur multilingue),
`IsHealthyAsync`. La factory sélectionne le fournisseur via `AiService:Provider` :

| Fournisseur | Rôle | Modèle |
|---|---|---|
| Infomaniak (principal) | Compatible OpenAI, datacenters suisses | `google/gemma-4-31B-it` |
| DeepSeek | Secours | modèle deepseek |
| Mock | Tests / environnement mock | — |

Embeddings : `bge-multilingual-gemma2`, 3584 dimensions (via le fournisseur principal).

## 6. Modèle de déploiement

- **Staging / Production** : stacks Docker Compose (postgres + worker + web) déployées avec `make staging-pub` / `make prod-pub` — le dépôt est empaqueté avec `tar` (excluant `.git`, `bin`, `obj`, `.env*`) et streamé en SSH ; les fichiers distants **ne sont pas** supprimés (les fichiers renommés/supprimés doivent être nettoyés manuellement).
- **Web** : Kestrel sert HTTP directement (staging), Caddy reverse proxy avec TLS en production.
- **Base de données** : conteneur PostgreSQL 16, persistance par volume ; `make prod-backup` pour les dumps.
- **Migrations** : `dotnet ef migrations add` passe par `DesignTimeDbContextFactory` (Infrastructure) — le host Web ne se construit pas en design-time ; le worker applique les migrations en attente au démarrage.
- **Supervision** : watchdogs cron au niveau hôte (`vigie24-ratelimit-watchdog`, `vigie24-source-spike-watchdog`, scripts dans `/opt/data/scripts/`) qui analysent les logs du worker et alertent sur Telegram/Discord — silence = OK.

## 7. Stratégie de test

- Tests unitaires de la logique Domain (scoring, taxonomie, moteur de débats, coûts, santé) — sans infrastructure.
- Tests fonctionnels de la couche web.
- Tests d'intégration nécessitant Docker (exclus du run par défaut via `--filter "FullyQualifiedName!~Integration"`).
- Les requêtes EF sont testées avec un contexte `InMemory` construit directement (Npgsql + InMemory dans un même conteneur DI sont incompatibles).

## 8. Surface publique & processus de release

- **Dépôt** : Gitea est la source principale (tous les commits) ; GitHub (`walbydev/vigie24`) reçoit les versions majeures via `make release VERSION=x.y.z` (tag annoté poussé vers les deux remotes).
- **Site** : vigie24.info (production), 192.168.30.160 (staging).
- **Contact** : contact@vigie24.info.
