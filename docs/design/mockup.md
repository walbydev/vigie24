# Vigie24 — Reference Visual Mockup

> **Technical/design reference document** · Framing version · August 18, 2026

## 1. Introduction

Vigie24 is a French-language **technology watch** website: it aggregates, contextualizes and ranks articles from specialized sources, so that the most useful signals emerge every day. The product should feel like editorialized, fast and reliable reading — closer to a cockpit than to a social feed.

The art direction combines a clear editorial interface, sober data cues and a warm color accent for important actions.

### 1.1 Design guidelines

| Element | Value / usage |
|---|---|
| Petrol blue | `#1B4B5A` — brand, navigation, strong headings, active links |
| Teal | `#5FB5A3` — positive states, selection, signals, trust score |
| Pale teal | `#EEF5F3` — soft surfaces, callouts and selected filters |
| Amber | `#E8A33D` — ranking, editorial alerts, secondary calls to action |
| Light background | Off-white / very light gray — hierarchy and breathing room |
| UI typography | **Inter** — body, controls, metadata |
| Heading typography | **Space Grotesk** — logo, headings and section markers |
| Data / scores | **JetBrains Mono** — scores, technical dates, identifiers and labels |

### 1.2 Composition principles

- **Hierarchy over decoration:** a heading and its context are readable at a glance.
- **Explainable signal:** the mixed score is always accompanied by details accessible on hover or focus.
- **Controlled density:** the central column stays priority; the sidebar enriches without competing with the feed.
- **Visible French identity:** categories, labels, dates and microcopy are in French; source proper names are not translated.
- **Accessibility:** AA contrast, keyboard focus, icons accompanied by a label or tooltip, touch targets of at least 44 px.

## 2. Persistent header

The header stays visible on scroll (target height: 68–72 px). It anchors the product, provides access to themes and keeps search within reach.

```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  ◉ VIGIE24       Thèmes ▾   IA   Climat   Cybersécurité   Produit   Start-up        ⌕ Rechercher      │
│                                                                                   ◌ Profil   ☼ Mode   │
└──────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### 2.1 Anatomy and behaviors

- **Logo:** `VIGIE24` wordmark in Space Grotesk, petrol blue; the leading dot/eye is the watch element. Click = back to home.
- **Theme navigation:** `Thèmes ▾` opens a mega-menu organized by families; the 4–5 most visited themes can remain visible. The active state is underlined in teal.
- **Search:** compact field with magnifying-glass icon, displayed keyboard shortcut (`⌘ K` / `Ctrl K`), full-text search and theme/source suggestions.
- **Profile:** avatar or initials, access to saved articles, preferences, subscriptions and logout.
- **Dark mode toggle:** switch with explicit `Mode Radar` state; the choice is remembered. Changing the theme must not shift the layout.
- **Sticky:** 1 px shadow or discreet border after 8 px of scroll; opaque header to preserve contrast.

## 3. Daily banner — "Le 24 de Vigie24"

The banner opens the page and immediately gives the digest's temporal context. It uses a `#EEF5F3` surface, an amber left border and a more editorial rhythm than the rest of the interface.

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

### 3.1 Specifications

- **Date:** localized long date, right-aligned on desktop; above the subtitle on mobile.
- **Daily stats:** four indicators maximum, in `JetBrains Mono`, with an accessible label. Values are computed over the published digest's scope.
- **RSS:** primary button `Recevoir le flux RSS`, opening the subscription instructions and the feed URL; plan for a copy button and a non-blocking confirmation.
- **Full digest:** secondary link to a filtered view containing the 24 items.
- **Empty / late state:** display "Le 24 sera publié à 08:00" or "Aucun signal suffisamment corroboré" rather than an empty banner.

## 4. Anatomy of an article card in the main feed

The card is the feed's reading unit. It must work without an illustration (color fallback + initials) and keep a clear click target: the title and illustration open the detail view.

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

### 4.1 Component details

1. **Illustration:** 16:10 ratio, 8 px corners, low-saturation color treatment. Descriptive `alt`; otherwise a category pattern.
2. **Category / subcategory:** category in petrol blue (e.g. `IA`), subcategory in gray; `NOUVEAU` badge limited to the last 24 hours.
3. **Title:** Space Grotesk, 20 px on desktop, maximum two lines in the feed; wording kept from the article but case-corrected if necessary.
4. **Summary:** 2–3 lines, 15–16 px Inter, oriented toward "why it matters" rather than a mere repetition of the title.
5. **Metadata:** relative date + absolute date on hover, author when available, primary source with external link and exit icon.
6. **Cross links:** `Croisé avec` lists the other sources covering the same event; click = filter or comparison panel.
7. **Actions:** `Lire`, `Plus tard`, `Favori`, `Noter`. Non-destructive actions are buttons; their active state is persistent and announced to screen readers.

### 4.2 States and interaction

- **Default:** `#DDE8E5` border, off-white background.
- **Hover / focus:** very slight elevation, teal border, no transformation that would harm the feed's stability.
- **Read:** slightly lower title contrast, but still AA; do not remove the score.
- **Pinned / favorite:** amber icon and accessible label `Retirer des favoris`.
- **Media error:** keep the area and show the fallback; no card should shift on load.

## 5. Visual representation of the mixed ranking

The ranking blends an **editorial AI score**, corroboration by several sources and the community signal. The main display must stay readable: a star rating and three complementary markers.

```text
┌───────────────────────────────────────────────────────────────────────────┐
│  ★★★★★  8,7 / 10     ◇ 3 sources croisées     ♡ 42 votes lecteurs        │
│  Ambre = score combiné       ◇ = corroboration       ♡ = communauté       │
└───────────────────────────────────────────────────────────────────────────┘
```

### 5.1 Proposed display rule

- **Stars / combined score:** `★` solid amber, `☆` outline; score out of 10 with one decimal (`8.7`). The combined score is presented as a sorting aid, never as a truth.
- **Cross-referenced sources:** `◇` teal icon, followed by the number of distinct, independent sources. Beyond 9, display `9+`.
- **Reader votes:** `♡` petrol-blue icon, followed by the number of votes; distinguish "votes" from "rating" to avoid confusion.
- **Position:** on desktop, at the right of the card row or in the title area; on mobile, under the title in a compact line.

### 5.2 Tooltip on hover or focus

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

The tooltip appears on hover, keyboard focus and long press; it must not cover the title. "En savoir plus" links to the calculation method. If a piece of data is missing, display `Non disponible` rather than `0`.

## 6. Desktop right sidebar

Visible from about 1,120 px, the sidebar is 288–320 px wide and stays sticky under the header. It accompanies the feed without pushing it out of the reading area.

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

### 6.1 Blocks

- **Trends of the day:** 4 to 6 topics, rising `↑`, falling `↓` or stable `→`, plus a link to the detailed ranking.
- **Active sources:** volume, French/international split, collection freshness and a management link. Do not expose false precision if the last run is in error.
- **Global RSS subscription:** direct CTA, with copyable URL and confirmation.
- **RSS by theme:** multi-select picker (e.g. IA + Cybersécurité), preview of the generated feed.
- **Newsletter:** email field, separate consent and clearly stated frequency; the block is visually distinct from RSS.

## 7. Article detail view

The dedicated page refocuses attention on a single signal and makes its provenance understandable. The original source remains clearly credited; Vigie24 must not substitute for the source article.

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

The detail page includes: breadcrumb, title, publication status, main media, editorial summary, expandable methodology/score, cross links, rating, tags and actions (`Plus tard`, `Favori`, share). On mobile, cross-referenced articles move under the summary and the source becomes a full-width button.

## 8. Advanced filters

Filters open from the `Filtrer` button above the feed. They are combinable, independently resettable and reflected in the URL for sharing.

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

- **Theme:** multi-selection, with search if the taxonomy grows.
- **Country / region:** France, Europe, North America, Africa, Asia-Pacific, International; plan for `Non renseigné` so articles are not silently excluded.
- **Minimum score:** synchronized slider and numeric field, no restrictive default value.
- **Source type:** media, expert blog, research, institution, community; the primary source and cross-referenced sources can have different types.
- Active filters become chips above the feed: `IA ×`, `≥ 7,0 ×`, etc.

## 9. Dark mode — "Mode Radar"

Mode Radar is a contrast adaptation, not a simple inversion. It favors a night-blue background, layered surfaces and amber/teal signals.

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

### 9.1 Palette and animation

| Role | Mode Radar color | Usage |
|---|---|---|
| Global background | `#0D2027` | canvas, dark header |
| Surface | `#132E38` | cards, sidebar |
| Elevated surface | `#1B3D47` | tooltip, menu, open filter |
| Primary text | `#F2F7F6` | headings and content |
| Secondary text | `#A9C1BF` | metadata, help |
| Border | `#2B5360` | separators |
| Brand petrol blue | `#5E9EAE` | links and focus adapted to dark |
| Teal | `#6CCBB4` | active states, corroboration |
| Pale green | `#B9E2D7` | selected surfaces / text on accent |
| Amber | `#F2B755` | ranking, CTAs, alerts |

The logo animation is discreet: the eye/dot performs a horizontal sweep of 1.2 s on activation then stays still. Honor `prefers-reduced-motion: reduce` (animation disabled); no infinite pulse should distract from reading.

## 10. Mobile responsive

The reference breakpoint is 768 px, with an intermediate adaptation around 1,024 px. The sidebar disappears on mobile and its contents are accessible in panels or via the trends button.

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

- **Simplified card:** illustration hidden by default or placed as a 72×72 thumbnail; summary limited to 2 lines; metadata on one line; actions reduced to `Lire`, favorite and `⋯` menu.
- **Hamburger menu:** themes, sources, filters, subscriptions and profile; the panel is full-screen or side with trapped focus.
- **Search:** icon opening a full-width field under the header.
- **Floating trends button:** persistent round button at bottom right, accessible label `Ouvrir les tendances du jour`; it opens the sidebar as a bottom sheet.
- **Daily banner:** stats stacked or in a 2×2 grid, full-width CTA.
- Horizontal scrolling is forbidden; headings and buttons must remain readable at 320 px wide.

## 11. Open points to validate

1. **Mixed ranking position:** keep it on the top line, right of the title, or move it under the summary to better explain the score? Decision expected after a comprehension test on mobile and desktop.
2. **Feed density:** validate an editorial card presentation (illustration + summary) or a compact Hacker News-style view (more visible items, less context). Recommended option: cards on the homepage, compact toggle for advanced users.
3. **Sidebar trends calculation basis:** define the scope (mention volume, growth speed, average score, clicks/reads, or a weighted combination), the time window (24 h / 7 days) and the minimum source threshold.
4. **Ranking transparency:** confirm the weights between editorial AI score, corroboration and reader rating, as well as the anti-bias policy for topics that are heavily voted but lightly sourced.
5. **Taxonomy:** arbitrate the number of top-level themes and the governance of subcategories to avoid overly long filters.
6. **Subscriptions:** choose whether theme RSS is generated on the fly or limited to preconfigured feeds; specify newsletter frequency and consent.
7. **Sources:** define the "active" status, the acceptable freshness delay and how to handle feeds in error.

> **Design validation criterion:** someone discovering Vigie24 must understand in under 10 seconds what is new today, why an article is highly ranked, where it comes from and how to save it.
