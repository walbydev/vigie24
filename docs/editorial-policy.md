# Vigie24 — Editorial policy for sources

Vigie24 aggregates francophone technology news. Candidate sources are reviewed
against the criteria below; there is no paid placement, no link exchange and no
partnership of any kind. A source added to Vigie24 grants nothing and receives
nothing beyond the link to its original articles.

## Accepted

- News outlets, specialised press, institutional and academic sources.
- Expert blogs, newsletters and technical channels whose main output is
  information rather than promotion.
- YouTube channels and subreddits moderated by humans (polled 2×/day).

## Rejected

- **Commerce-first content**: buying guides, tool/service comparisons,
  "avis" or "tests" of a provider, offer landing pages — anything whose main
  purpose is to drive an affiliate click or a sign-up. Independently of
  writing quality, length or traffic.
- **AI-generated content farms**: undifferentiated volume, single author,
  no editorial responsibility stated.
- **Republishing-only feeds**: full copies of another outlet's articles.

## Two enforcement layers

1. **Score cap (AI)**: the analysis prompt caps `editorial_score` at 45
   (publication threshold is 60) for any commercially motivated content and
   requires the reason to appear in `justification`. This catches a commercial
   article published by an otherwise legitimate source.
2. **Source exclusion (database)**: `sources."EditorialPolicy" = 'Commercial'`
   removes the source from polling altogether — see
   `Vigie24.Domain.SourceEditorialPolicy` and `scripts/exclude-commercial-sources.sql`.
   A separate column from `IsActive` on purpose: the 24 h recovery probe
   re-activates auto-disabled sources, and must never undo an editorial
   exclusion.

## Answering a solicitation

Source suggestions usually arrive wrapped in a request for a link. The reply is
short and does not open a negotiation:

- sources are selected on editorial criteria, with no counterpart and no
  link-building logic;
- Vigie24 never displays article content: title, AI-generated summary from the
  feed, link to the original, and the feed thumbnail;
- terms that forbid the summary or the thumbnail (e.g. "titles, short excerpts
  and links only, no images") are incompatible with the service;
- criteria are published here, and are not negotiable per requester.
