# Vigie24 — Algorithms

> Companion document: [architecture.md](architecture.md) describes the components
> and data flow. This document is the reference for every formula and matching rule.
> The scoring constants below live in `Vigie24.Domain.ArticleScoring` and are the
> **single source of truth** (shared by the Worker and the Web "precise score" hint).

## 1. Ingestion pipeline

For each source, each worker cycle (`FeedReader:MaxItemsPerFeed` items max, default 15):

1. **Fetch** the feed — RSS/Atom via `SyndicationFeed`, YouTube via
   `https://www.youtube.com/feeds/videos.xml?channel_id=…`, Reddit via JSON API.
   A browser-like `User-Agent` is sent; invalid dates are rejected (a feed that
   cannot be parsed is skipped, the worker never crashes).
2. **Exact dedup** — if an article with the same `CanonicalUrl` already exists, it is skipped.
3. **Semantic dedup** — the new item is compared pairwise with the most recent
   articles of the last 7 days (capped by `FeedReader:MaxDuplicateCheckArticles`,
   default 10) using the AI `IsDuplicateAsync` call. Any match → skip.
4. **AI analysis** — `ProcessArticleAsync(title, content, sourceUrl)` returns:
   French summary, editorial score (0–100), themes, category hierarchy, keywords,
   debate topic + stance, anxiety score (0–10). Token usage is captured for cost tracking.
5. **Illustration** — image from the feed (`media:content`/`enclosure`), otherwise
   scraped from the article page (`FeedReader:ScrapeArticleImages`, default true).
6. **Persistence** — the article is added to the context *immediately* (before any
   intermediate `SaveChanges`) to avoid a foreign-key violation on `article_embeddings`.
   Embedding (title + summary) is computed non-blockingly (failure → warning).
7. **Scoring & publication** — see sections 3–6. The article is published when
   `EditorialScore ≥ 60` (hard-coded threshold).
8. **Attachments** — tags (themes), primary source link, author, category
   (section 8). All `DateTime.Kind` are normalized to UTC before saving.

## 2. Duplicate detection

| Layer | Rule |
|---|---|
| Exact | Same `CanonicalUrl` → skip |
| Semantic | AI pairwise comparison against the last 7 days of articles, capped at `MaxDuplicateCheckArticles` (10) to bound AI calls (rate limits) |

## 3. Editorial score (AI, 0–100)

Produced by the LLM (`editorial_score` in the result contract): how newsworthy the
article is for a French tech audience. **Publication threshold: ≥ 60** (hard-coded).

## 4. Algorithmic score (deterministic, 0–100)

Average of recency and content-density, both 0–100:

```
Recency:  100 if ≤ 1h, 80 if ≤ 6h, 60 if ≤ 24h, 40 if ≤ 48h,
          then max(0, 40 − (hours − 48) / 24 × 10)
Content:  90 if summary ≥ 50 words, 70 if ≥ 30, 50 if ≥ 15, else 30

Algorithmic = (Recency + Content) / 2
```

## 5. Engagement score (0–100)

From anonymous votes (0–5 scale) and clicks. **0 if there are no votes and no clicks**
(ranking unchanged until the audience interacts):

```
voteScore  = clamp(voteAverage / 5 × 100, 0, 100)     (0 if no votes)
clickScore = min(100, clickCount × 10)                  (0 if no clicks)
Engagement = 0.6 × voteScore + 0.4 × clickScore         (votes weigh more)
```

## 6. Ranking score (0–100)

```
Ranking = Editorial × 0.55 + Algorithmic × 0.35 + Engagement × 0.10
```

The three weights are constants (`EditorialWeight`, `AlgorithmicWeight`,
`EngagementWeight`). Engagement starts at 0, so a new article ranks purely on
editorial + algorithmic quality.

## 7. Anxiety score (0–10)

Subjective, editorial measure of how much the topic is likely to worry a French
tech-watch reader (threats, crises, alarmism). Produced by the LLM
(`anxiety_score`), stored as `null` when out of range (guard: `0 ≤ x ≤ 10` — the
model sometimes returns values outside the contract). The UI exposes a
`maxAnxiety` filter (article browser + RSS); articles with `null` always pass.

## 8. Taxonomy mapping

The AI returns a hierarchy `[parent, subcategory]` (e.g. `["IA", "LLM Models"]`).
`GetOrCreateCategoryAsync` maps it onto the fixed taxonomy (8 parents, ~35
subcategories — `Vigie24.Domain.Taxonomy`), returning the subcategory id, else the
parent id. Fallback: legacy mapping by primary theme. Categories outside the
taxonomy created by the model are reconciled periodically by scripts.

## 9. Debates

Debates pair articles covering the same subject with opposing stances. Rebuilt at
the end of every worker cycle from articles of the **last 14 days** carrying a
topic + stance (`RebuildDebatesAsync`).

### 9.1 Topic normalization (`DebateEngine.NormalizeTopic`)

Lowercase, no diacritics (explicit accent table — robust without ICU), punctuation
→ spaces, 2-letter words dropped, stop-words removed, **first 4 significant words
kept**. Example: `"AI Regulation in Europe"` → `"ai regulation europe"`.

### 9.2 Stance normalization (`DebateEngine.NormalizeStance`)

Accepted values and synonyms, mapped to 4 canonical stances:

| Canonical | Synonyms (prefix matching) |
|---|---|
| `pour` (for) | favorable, soutien, pro… |
| `contre` (against) | defavorable, opposition, anti… |
| `nuance` (nuanced) | nuançé, mitigé, prudent, reserve… |
| `neutre` (neutral) | neutral, objectif, information… |

### 9.3 Debate rule

A debate exists when **≥ 2 articles share the same normalized topic AND at least
one `pour` + one `contre` are present**. `nuance` and `neutre` join a debate but do
not trigger one. Stale debates/links are removed on rebuild.

### 9.4 Semantic merge of isolated topics

A topic with a single article (no opponent yet) is matched against existing debate
groups by **embedding cosine similarity ≥ 0.85** (`MergeIsolatedGroupsAsync`); the
closest group wins, which can create the missing opposition. If the merged group
still has no opposing stances, it is discarded.

### 9.5 Embeddings

`GetEmbeddingAsync("«Title» — «Summary»")` → `bge-multilingual-gemma2`, **3584
dimensions**, stored in `article_embeddings.Vector`. Similarity = cosine.

## 10. AI cost tracking

Per-article token usage (`PromptTokens`, `CompletionTokens`) is captured from the
API `usage` fields and stored on the article. Cost in CHF, Infomaniak rates
(configurable prices per million tokens):

```
Cost = promptTokens × inputPricePerMillion / 1_000_000
     + completionTokens × outputPricePerMillion / 1_000_000
```

Aggregated per day/week on the `/couts` page (`CostReportService`).

## 11. Health evaluation (`HealthEvaluator`)

| Level | Condition |
|---|---|
| `Ok` | AI provider reachable AND last ingestion age ≤ `okThreshold` |
| `Degraded` | provider reachable AND age ≤ `degradedThreshold` |
| `Offline` | provider unreachable, OR no article ever ingested, OR age > `degradedThreshold` |

The homepage status block uses `FormatAge` for human-readable freshness
("il y a 12 min", "il y a 3 h 05", "il y a 2 j").

## 12. RSS feed generation (`RssFeedService`)

`/feed` (global) and `/categories/{slug}/feed` share one builder: filters applied
via `ArticleFiltering.ApplyFilterCriteria` (same logic as the web browser), sorted
by date descending, **capped at 50 items**. Responses carry
`Cache-Control: public, max-age=300`.
