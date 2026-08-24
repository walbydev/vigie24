# Vigie24 — Project Specification

## French-language technology watch and aggregation system

- **Document date:** August 18, 2026
- **Status:** Living, evolving document

## Table of contents

1. [Vision and objectives](#1-vision-and-objectives)
2. [Site identity](#2-site-identity)
3. [Article structure (article card)](#3-article-structure-article-card)
4. [Publication frequency](#4-publication-frequency)
5. [Geographic scope and personalization](#5-geographic-scope-and-personalization)
6. [Distribution — RSS/Atom feeds](#6-distribution--rssatom-feeds)
7. [Source management](#7-source-management)
8. [New source discovery rules](#8-new-source-discovery-rules)
9. [Mixed ranking system](#9-mixed-ranking-system)
10. [Technical architecture](#10-technical-architecture)
11. [Visual design guidelines](#11-visual-design-guidelines)
12. [Open points / to be decided later](#12-open-points--to-be-decided-later)
13. [Proposed next steps](#13-proposed-next-steps)

## 1. Vision and objectives

Vigie24 is an automated technology watch system covering:

- AI
- Technology
- Science
- Research
- Software development

The retained objectives and principles are the following:

- Geographic priority: Switzerland > France > Europe > World.
- Evolving source list (continuous acquisition and discovery of new sources).
- Inclusion of video (YouTube) and community (Reddit) sources.
- Runs as a background task, feeding technology themes daily.
- Priority on information quality and rigorous sourcing.
- Side-by-side presentation of conflicting articles to illustrate a debate/argument.
- Summaries systematically in French; original source languages preserved: French, English, German.
- Presentation organized by themes > subcategories > topics/arguments, encouraging reflection and avoiding headline effects.
- Distribution via website + RSS/Atom feeds for subscription.
- As a first approach: the AI (assistant) is the only entity feeding the site.
- French-language site in the first version, international versions possible later.

## 2. Site identity

- **Chosen name:** Vigie24 (domain `vigie24.info`) — `vigie.tech` being already taken, the name was chosen to evoke watchkeeping (vigie) + continuous/daily operation (24h).
- **Daily digest:** "Le 24 de Vigie24".

## 3. Article structure (article card)

Each article card contains the following fields:

- Visual illustration
- Title
- Date
- Author(s) (name, role)
- Source(s) with link
- Summary (in French)
- Link(s) to the original

Associated features:

- Ranking system from 0 (not interesting) to 5 (very interesting).
- "Read later" marking.
- "Favorite" marking.
- The same article may appear in several themes/subcategories depending on the topics covered.

## 4. Publication frequency

- 1 daily digest ("Le 24 de Vigie24").

## 5. Geographic scope and personalization

- Single French-language site (no separate sites per country/region).
- Geolocation detection + manual user preference.
- Dynamic reordering of content by country (no filtering/hiding, just reordering).
- Soft minimum-visibility quota for low-content regions (Switzerland, Belgium, Quebec) so they are not drowned out.
- Default priority order: Switzerland > France > Belgium > Quebec > Broader Francophonie > Europe > World.

### Illustrative example of the mechanism

Same corpus of articles from 08/18/2026, reordered differently per Switzerland view / France view / Quebec view: the best local article rises to the top thanks to the quota, even if its raw editorial ranking is lower than an international article.

### Still open point

Should the editorial ranking always prevail over locality (the quota applying only in case of a tie), or keep the soft quota as described? For now, an "ok" answer was given without formally deciding; the soft quota mechanism is kept by default.

## 6. Distribution — RSS/Atom feeds

- Generation of RSS 2.0 **and** Atom feeds as output.
- Technical generation via `System.ServiceModel.Syndication` (native .NET).

### Feed granularity

- Global "Le 24 de Vigie24" feed.
- Feed per theme (AI, Dev, etc.).
- Feed per country/region.
- Personalized feed per logged-in user (followed themes + preferred country).

### Example URLs

- `vigie24.info/rss/ia`
- `vigie24.info/rss/ch`

## 7. Source management

### 7.1 Storage

PostgreSQL (no static config file), to allow manual modification/addition without redeployment, reliability history, clean many-to-many relationships with themes/countries.

### 7.2 Proposed SQL schema

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

### 7.3 Starting source list (initial sample, to be completed)

#### AI table

| Source | Country | Type | Notes |
|---|---|---|---|
| EPFL / MILA / INRIA (research blogs) | CH/CA/FR | RSS | Academic research |
| ICTjournal | CH | RSS | Swiss tech press |
| Les Échos Tech, Numerama | FR | RSS | Generalist tech press |
| L'Écho, DataNews | BE | RSS | Belgian tech press |
| La Presse, Le Devoir (tech) | QC | RSS | Quebec press |
| Hacker News (AI-filtered) | World | API | Community discussions |
| r/MachineLearning | World | Reddit API | Technical community |
| OpenAI Blog, Anthropic Blog, Mistral Blog | World | RSS | Primary lab sources |
| ArXiv (cs.AI, cs.CL) | World | API | Academic papers |

#### Development table

| Source | Country | Type |
|---|---|---|
| Dev.to, Hacker News | World | API/RSS |
| Microsoft/GitHub blogs | World | RSS |
| ICTjournal, Netzwoche | CH | RSS |
| Silicon.fr, JDN | FR | RSS |

> **Note:** non-exhaustive list, to be completed theme by theme in a later dedicated document.

### 7.4 Administration interface

Protected page with simple CRUD: source list, add form, active/inactive toggle, inline editing.

## 8. New source discovery rules

Four complementary mechanisms, with mandatory human validation before activation (no automatic addition in production):

- **Rule A — Cross-citation suggestion:** if an unlisted domain is frequently cited/linked by aggregated articles (threshold e.g. 3 citations/30 days), propose it to the queue.
- **Rule B — Periodic thematic search suggestion:** a monthly scheduled job running targeted web searches (e.g. `"AI news" site:.ch`), proposes candidates.
- **Rule C — Community suggestion:** a "Propose a source" form for readers, goes into the moderation queue.
- **Rule D — AI suggestion:** periodic (monthly) analysis of industry trends by the AI assistant, reasoned proposal of new sources.

All suggestions land in a `sources_candidates` table with status `en_attente`, mandatory manual validation in the admin.

### Validation checklist before activating a source

- Sufficient publication frequency.
- French language (or properly translatable).
- No obvious conflict of interest / disguised advertising content.
- History of at least 5 verifiable articles.
- Initial reliability score set to 0.5, then adjusted according to performance.

## 9. Mixed ranking system

### 9.1 Scoring components

- **AI score:** selection + initial score based on quality/relevance/number of cross-referenced sources.
- **Cross-reference score:** number of independent media covering the same topic.
- **Reader score:** community rating (1-5 stars).

### 9.2 Display and formula

Retained decision: display the 3 scores separately + 1 combined score shown by default as priority.

Default combined score formula (weighting adjustable in configuration without redeployment):

```text
Score_affiché = (0.4 × Score_IA) + (0.3 × Score_croisement) + (0.3 × Score_lecteurs)
```

The breakdown of the 3 components is available on click ("view ranking details").

### 9.3 Special cases handled

- Fewer than 5 reader votes → display the AI+cross-reference score alone with a "new" badge.
- Gap of more than 2 stars between AI and readers → "Mixed reviews" visual indicator inviting discussion/confrontation.

### 9.4 Reader voting system

1-5 star rating, one vote per logged-in user per article, basic anti-brigading (votes/IP/day limit, abnormal pattern detection).

## 10. Technical architecture

### 10.1 Retained stack

- **Unified backend + frontend:** ASP.NET Core (LTS, .NET 10 expected in 2026).
- **Frontend:** Blazor Server or Blazor WebAssembly recommended (stay 100% C#, Blazor Server simpler to deploy on a single VPS); possible alternative: Razor Pages + htmx if more lightness is needed (to be decided according to the user's comfort).
- **Database:** PostgreSQL (validated user choice) — native arrays useful for country/theme tags, JSON for flexibility.
- **ORM:** Entity Framework Core.

### 10.2 Aggregation / scraping

Separate service: .NET Worker Service (background jobs):

- RSS/Atom polling via a library such as CodeHollow.FeedReader.
- Targeted scraping when no RSS feed is available (HtmlAgilityPack or AngleSharp).
- External API calls (Reddit API, YouTube Data API, ArXiv API).

Scheduled job orchestration: Hangfire (built-in monitoring dashboard, well suited to a Docker deployment), rather than raw cron.

### 10.3 RSS/Atom generation

`System.ServiceModel.Syndication` (native .NET).

### 10.4 Deployment

User's dedicated VPS, Docker containerization.

```yaml
services:
  vigie24-web:        # ASP.NET Core Blazor
  vigie24-worker:      # Worker Service - aggregation
  vigie24-db:          # PostgreSQL
  vigie24-hangfire:    # Jobs dashboard (can be embedded in the worker)
```

### 10.5 AI integration

Call to an AI API (e.g. Claude, or another) from the Worker Service to:

- Summarize articles.
- Detect duplicates / source cross-references.
- Assign the initial score.
- Classify by theme / subtheme.

## 11. Visual design guidelines

**Guiding concept:** "Seriousness with a twinkle in its eye" — neutrality, seriousness, calmness, with a discreet touch of geek-tech quirkiness.

### 11.1 Color palette

```text
Main background:  #F7F8FA (off-white, soft, not aggressive)
Main text:        #1A1D23 (anthracite, not pure black - softer)
Primary accent:   #2D5F7C (deep petrol blue - serious, neutral, "vigie")
Secondary accent: #4A9B8E (soft teal - calming touch)
Highlight/CTA:    #E8A33D (soft amber - the "geek flair", used sparingly)
Alerts/confrontation: #C4634A (terracotta, never bright red - stays calm)
```

### 11.2 Typography

- **Headings:** Space Grotesk or IBM Plex Sans (embraced geek style, readable, professional).
- **Body text:** Inter or Source Sans Pro (sobriety, long-read readability).
- **Technical metadata (dates, scores, tags):** JetBrains Mono (discreet dev nod).

### 11.3 Logo — concept

- Stylized magnifying glass/eye shaped like a "24", or a minimalist radar/sonar (concentric circles evoking the benevolent watch).
- Style: thin line, monochrome petrol blue, no garish gradient.
- The "24" (hours) subtly integrated into the shape.

### 11.4 Interface elements

- Theme icons in outline style (Lucide/Feather type) rather than emoji in production.
- Discreet micro-animations (card fade-ins, no excessive motion).
- Planned easter egg: dark mode named "Mode Radar".

### 11.5 General reference atmosphere

Draw inspiration from modern SaaS sobriety (Stripe, Linear type) rather than an overloaded style (TechCrunch type) or an overly austere/raw one (Hacker News type).

## 12. Open points / to be decided later

- Definitive arbitration between editorial ranking priority vs soft geographic quota (see section 5).
- Final choice between Blazor Server, Blazor WebAssembly, or Razor Pages + htmx for the frontend.
- Choice of the AI API provider for the production integration.
- Complete and exhaustive source list per theme (beyond the section 7.3 sample).
- Complete database schema (beyond the already specified sources/themes tables): articles, users, votes, detailed ranking.

## 13. Proposed next steps (not yet carried out as of the document date)

- Complete database schema (all tables: articles, sources, themes, users, votes, ranking).
- Complete, exhaustive source list, theme by theme (separate document).
- More advanced textual visual mockup of the homepage with the design guidelines applied.
