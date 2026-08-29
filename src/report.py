#!/usr/bin/env python3
"""Report builder for vigie24 source discovery: candidates.jsonl -> report.md.

Transforms the raw candidates produced by ``src.discovery`` into a curated,
Telegram-readable Markdown report of 10-20 new sources, majority French.

Pipeline
--------
1. **Relevance scoring** — every candidate is scored against its category's
   keywords (``config/categories.json``): keyword hits in title/description,
   plus a language bonus (fr > en > unknown). Candidates below ``--min-score``
   are dropped (removes off-topic noise such as Bluesky accounts that merely
   matched a fuzzy search).
2. **Deduplication** — by canonical URL (RSS), by channel (YouTube), by
   subreddit (Reddit) and by profile handle (Bluesky), then one RSS source per
   domain (the best-scoring feed wins).
3. **Seen-source filtering** — reads ``state/seen_sources.json`` and drops
   every source already reported. After the report is generated the state file
   is updated so the next run only proposes brand-new sources.
4. **Selection** — between ``--min-sources`` (default 10) and ``--max-sources``
   (default 20) sources, round-robin across categories for diversity, with at
   least ``--fr-ratio`` (default 60 %) of the report in French when the pool
   allows it. If fewer than ``min-sources`` valid candidates remain, the
   available ones are still reported with a warning.
5. **Markdown report** — timestamped, sections per category, each entry with
   title, URL, platform, description and the recommendation reason. Plain
   Markdown so it renders correctly on Telegram.

Usage
-----
    python -m src.report --input candidates.jsonl --output report.md \\
        --state state/seen_sources.json

    python -m src.report --dry-run --min-score 4 --verbose   # inspect only

Exit codes: 0 = report written (a warning is embedded when < min-sources),
1 = no valid source at all, 2 = bad usage / missing config.
"""
from __future__ import annotations

import argparse
import json
import logging
import math
import os
import re
import sys
import tempfile
import unicodedata
from collections import OrderedDict
from datetime import datetime, timezone
from typing import Dict, List, Optional, Sequence, Set, Tuple
from urllib.parse import urlsplit

# Shared pipeline helpers (normalize_url, flatten_categories, detect_language).
from src import discovery as d

__version__ = "1.0.0"

DEFAULT_MIN_SCORE = 3
DEFAULT_MIN_SOURCES = 10
DEFAULT_MAX_SOURCES = 20
DEFAULT_FR_RATIO = 0.6
DESCRIPTION_CAP = 240

FR_DAY_NAMES = ["lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi", "dimanche"]
FR_MONTH_NAMES = [
    "janvier", "février", "mars", "avril", "mai", "juin",
    "juillet", "août", "septembre", "octobre", "novembre", "décembre",
]

PLATFORM_LABELS = {
    "rss": "Flux RSS",
    "youtube": "YouTube",
    "reddit": "Reddit",
    "bluesky": "Bluesky",
}
LANGUAGE_LABELS = {"fr": "Français", "en": "Anglais", "unknown": "Inconnue"}

# Subreddit/platform keying exceptions: a raw "domain" that is really an
# aggregator and must never be used for domain-level dedup.
AGGREGATOR_DOMAINS = frozenset({"news.google.com", "www.reddit.com", "bsky.app", "youtube.com", "www.youtube.com"})


# ---------------------------------------------------------------------------
# Text / keyword helpers
# ---------------------------------------------------------------------------

def unaccent(text: str) -> str:
    """Lowercase and strip diacritics so 'cybersécurité' matches 'cybersecurite'."""
    return "".join(
        c for c in unicodedata.normalize("NFD", (text or "").lower())
        if unicodedata.category(c) != "Mn"
    )


def keyword_set(cat: dict) -> List[Tuple[str, str]]:
    """Return [(normalized_keyword, original_keyword)] for one category.

    Includes ``keywords.fr``/``keywords.en``, the French/English category names
    and all platform-specific terms (RSS/YouTube/Reddit/Bluesky queries).
    """
    pairs: List[Tuple[str, str]] = []
    seen: Set[str] = set()
    for group in ("fr", "en"):
        for kw in cat.get("keywords", {}).get(group, []) or []:
            kw = kw.strip()
            if not kw:
                continue
            norm = unaccent(kw)
            if norm not in seen:
                seen.add(norm)
                pairs.append((norm, kw))
    for name in (cat.get("name", ""), cat.get("name_en", "")):
        norm = unaccent(name)
        if norm and norm not in seen:
            seen.add(norm)
            pairs.append((norm, name))
    for plat in ("rss", "youtube", "reddit", "bluesky"):
        for kw in cat.get("platforms", {}).get(plat, []) or []:
            kw = kw.strip()
            if not kw:
                continue
            norm = unaccent(kw)
            if norm not in seen:
                seen.add(norm)
                pairs.append((norm, kw))
    return pairs


def _kw_matches(text_norm: str, pairs: Sequence[Tuple[str, str]]) -> List[str]:
    """Return original keywords found in the normalized text, in taxonomy order.

    Short keywords (< 4 chars, no space) are matched on word boundaries so
    "IA" does not match "via" and "AI" does not match "email".
    """
    matched: List[str] = []
    for norm, orig in pairs:
        if not norm:
            continue
        if len(norm) >= 4 or " " in norm:
            found = norm in text_norm
        else:
            found = bool(re.search(r"(?<![a-z0-9])" + re.escape(norm) + r"(?![a-z0-9])", text_norm))
        if found:
            matched.append(orig)
    return matched


def score_candidate(row: dict, pairs: Sequence[Tuple[str, str]]) -> Tuple[int, List[str]]:
    """Score = 2 * keyword_hits + language_weight (fr=2, en=1, unknown=0)."""
    text = unaccent(f"{row.get('title', '')} {row.get('description', '')}")
    matched = _kw_matches(text, pairs)
    lang = row.get("language", "unknown")
    lang_weight = {"fr": 2, "en": 1}.get(lang, 0)
    return 2 * len(matched) + lang_weight, matched


# ---------------------------------------------------------------------------
# Deduplication keys
# ---------------------------------------------------------------------------

def canonical_key(row: dict) -> str:
    """Platform-aware identity: RSS -> normalized URL, YouTube -> channel id /
    handle, Reddit -> subreddit, Bluesky -> profile handle."""
    platform = row.get("platform", "")
    url = row.get("url", "")
    if platform == "youtube":
        m = re.search(r"channel/(UC[\w-]+)", url) or re.search(r"/(@[\w.-]+)", url)
        if m:
            return f"youtube:{m.group(1).lower()}"
        return f"youtube:{d.normalize_url(url)}"
    if platform == "reddit":
        m = re.match(r"/r/([\w-]+)/?", urlsplit(url).path)
        if m:
            return f"reddit:{m.group(1).lower()}"
        return f"reddit:{d.normalize_url(url)}"
    if platform == "bluesky":
        m = re.match(r"/profile/([\w.-]+)/?", urlsplit(url).path)
        if m:
            return f"bluesky:{m.group(1).lower()}"
        return f"bluesky:{d.normalize_url(url)}"
    return f"rss:{d.normalize_url(url)}"


def domain_key(row: dict) -> Optional[str]:
    """Registrable-ish domain for RSS rows; None for other platforms."""
    if row.get("platform") != "rss":
        return None
    netloc = urlsplit(row.get("url", "")).netloc.lower()
    if not netloc or netloc in AGGREGATOR_DOMAINS:
        return None
    if netloc.startswith("www."):
        netloc = netloc[4:]
    return f"domain:{netloc}"


# ---------------------------------------------------------------------------
# Seen-source state
# ---------------------------------------------------------------------------

def load_seen(path: str) -> Tuple[Set[str], List[dict]]:
    """Load state/seen_sources.json -> (keys, entries). Missing file = empty."""
    if not os.path.exists(path):
        return set(), []
    try:
        with open(path, "r", encoding="utf-8") as fh:
            data = json.load(fh)
    except (ValueError, OSError) as exc:
        raise ValueError(f"state file unreadable: {path} ({exc})") from exc
    entries = data.get("seen", []) if isinstance(data, dict) else []
    keys: Set[str] = set()
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        if entry.get("key"):
            keys.add(entry["key"])
        if entry.get("source_id"):
            keys.add(f"source_id:{entry['source_id']}")
    return keys, entries


def update_seen(path: str, entries: List[dict], selected: Sequence[dict], report_date: str) -> int:
    """Append the reported sources to the state file (merged by key, atomic)."""
    existing = {e.get("key"): e for e in entries if isinstance(e, dict) and e.get("key")}
    seen_at = datetime.now(timezone.utc).isoformat(timespec="seconds")
    added = 0
    for row in selected:
        key = canonical_key(row)
        if key in existing:
            continue
        existing[key] = {
            "key": key,
            "source_id": row.get("source_id", ""),
            "title": row.get("title", ""),
            "url": row.get("url", ""),
            "platform": row.get("platform", ""),
            "seen_at": seen_at,
            "report": report_date,
        }
        added += 1
    merged = sorted(existing.values(), key=lambda e: e.get("seen_at", ""))
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    payload = {
        "schema_version": "1.0",
        "updated_at": seen_at,
        "seen": merged,
    }
    fd, tmp = tempfile.mkstemp(dir=os.path.dirname(os.path.abspath(path)) or ".", suffix=".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as fh:
            json.dump(payload, fh, ensure_ascii=False, indent=2)
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)
    return added


# ---------------------------------------------------------------------------
# Selection
# ---------------------------------------------------------------------------

def rank_candidates(candidates: Sequence[dict]) -> List[dict]:
    """French first, then score, then audience, then title."""
    return sorted(
        candidates,
        key=lambda c: (
            c.get("language") != "fr",          # fr first
            -c.get("_score", 0),                 # relevance
            -int(c.get("subscriber_count") or 0),  # audience
            c.get("title", "").lower(),
        ),
    )


def select_sources(
    candidates: Sequence[dict],
    *,
    min_sources: int = DEFAULT_MIN_SOURCES,
    max_sources: int = DEFAULT_MAX_SOURCES,
    fr_ratio: float = DEFAULT_FR_RATIO,
) -> Tuple[List[dict], List[str]]:
    """Pick up to ``max_sources`` candidates with category diversity and a
    French majority. Returns (selected, warnings)."""
    warnings: List[str] = []
    ranked = rank_candidates(candidates)
    total = len(ranked)
    if total == 0:
        return [], ["Aucune source valide trouvée dans les candidats."]
    if total < min_sources:
        warnings.append(
            f"Seulement {total} source(s) valide(s) — rapport partiel "
            f"(minimum souhaité : {min_sources})."
        )

    n = min(max_sources, total)
    fr_target = math.ceil(fr_ratio * n)

    # Round-robin across categories for a diverse report.
    groups: "OrderedDict[str, List[dict]]" = OrderedDict()
    for c in ranked:
        groups.setdefault(c.get("category", "?"), []).append(c)
    selected: List[dict] = []
    i = 0
    while len(selected) < n:
        took = False
        for group in groups.values():
            if i < len(group):
                selected.append(group[i])
                took = True
                if len(selected) >= n:
                    break
        if not took:
            break
        i += 1

    # Enforce the French majority: swap the weakest selected non-French source
    # for the best remaining French one until the target ratio is met.
    fr_taken = sum(1 for c in selected if c.get("language") == "fr")
    if fr_taken < fr_target:
        untaken_fr = [c for c in ranked if c.get("language") == "fr" and c not in selected]
        selected_non_fr = [c for c in selected if c.get("language") != "fr"]
        while fr_taken < fr_target and untaken_fr and selected_non_fr:
            # Worst non-FR selected = last by rank order; best FR = first remaining.
            worst = selected_non_fr[-1]
            best_fr = untaken_fr[0]
            selected.remove(worst)
            selected.append(best_fr)
            selected_non_fr.pop()
            untaken_fr.pop(0)
            fr_taken += 1
    if fr_taken < fr_target:
        warnings.append(
            f"Moins de {int(round(fr_ratio * 100))} % de sources francophones dans le "
            f"rapport ({fr_taken}/{len(selected)}) — pool francophone insuffisant."
        )

    # Stable final order: by category (report section order), then rank.
    selected = sorted(selected, key=lambda c: (c.get("category", "?"), ranked.index(c)))
    return selected, warnings


# ---------------------------------------------------------------------------
# Report rendering
# ---------------------------------------------------------------------------

def _french_date(dt: datetime) -> str:
    return f"{FR_DAY_NAMES[dt.weekday()]} {dt.day} {FR_MONTH_NAMES[dt.month - 1]} {dt.year}"


def _french_short_date(dt: datetime) -> str:
    return f"{dt.day:02d}/{dt.month:02d}/{dt.year}"


def _clip(text: str, cap: int = DESCRIPTION_CAP) -> str:
    text = (text or "").strip().replace("\n", " ")
    if len(text) <= cap:
        return text
    return text[: cap - 1].rstrip() + "…"


def _display_keywords(matched: Sequence[str], cap: int = 4) -> List[str]:
    """Keep the most specific keywords for display: sort longest-first and drop
    any keyword that is a substring of an already-kept one (e.g. keep
    'développement logiciel', drop 'Développement')."""
    kept: List[str] = []
    for kw in sorted(matched, key=len, reverse=True):
        norm = unaccent(kw)
        if any(norm in unaccent(k) for k in kept):
            continue
        kept.append(kw)
        if len(kept) >= cap:
            break
    return kept


def render_report(
    selected: Sequence[dict],
    categories_by_slug: Dict[str, dict],
    *,
    stats: Optional[dict] = None,
    warnings: Sequence[str] = (),
    generated_at: Optional[datetime] = None,
) -> str:
    """Render the final Markdown report (Telegram-friendly, plain markdown)."""
    now = generated_at or datetime.now(timezone.utc)
    stats = stats or {}
    total = stats.get("total", 0)
    kept = stats.get("kept", 0)
    seen_removed = stats.get("seen_removed", 0)
    fr_count = sum(1 for c in selected if c.get("language") == "fr")
    ratio = int(round(100 * fr_count / len(selected))) if selected else 0

    lines: List[str] = []
    lines.append(f"# 📡 Nouvelles sources vigie24 — {_french_date(now)}")
    lines.append("")
    lines.append(
        f"Rapport généré le {_french_short_date(now)} à {now:%H:%M} UTC · "
        f"**{len(selected)} nouvelles sources** · {fr_count} FR / "
        f"{len(selected) - fr_count} autres ({ratio} % francophone)"
    )
    lines.append("")
    for warning in warnings:
        lines.append(f"⚠️ **Avertissement :** {warning}")
        lines.append("")

    # Group by category (keep taxonomy order, fall back to slug).
    by_cat: "OrderedDict[str, List[dict]]" = OrderedDict()
    for c in selected:
        by_cat.setdefault(c.get("category", "?"), []).append(c)
    ordered_cats = sorted(
        by_cat.keys(),
        key=lambda slug: (categories_by_slug.get(slug, {}).get("name", slug) or slug).lower(),
    )

    for slug in ordered_cats:
        cat = categories_by_slug.get(slug, {})
        name = cat.get("name") or slug
        lines.append(f"## {name}")
        lines.append("")
        for c in by_cat[slug]:
            platform = PLATFORM_LABELS.get(c.get("platform", ""), c.get("platform", ""))
            lang = LANGUAGE_LABELS.get(c.get("language", "unknown"), "Inconnue")
            matched = c.get("_matched") or []
            reason_kws = _display_keywords(matched)
            reason = f"Mots-clés détectés : {', '.join(reason_kws)}" if reason_kws else "Correspondance par langue et catégorie"
            lines.append(f"### [{c.get('title', 'Sans titre')}]({c.get('url', '')})")
            lines.append(f"- **Plateforme :** {platform} · **Langue :** {lang}")
            desc = _clip(c.get("description", ""))
            if desc:
                lines.append(f"- **Description :** {desc}")
            lines.append(f"- **Pourquoi :** {reason}")
            lines.append("")

    lines.append("---")
    lines.append("")
    lines.append(
        f"*Pipeline vigie24 — {total} candidats analysés, {kept} pertinents "
        f"après filtrage, {seen_removed} déjà signalés retirés. "
        f"Prochain rapport dans 3 jours.*"
    )
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def load_candidates(path: str) -> List[dict]:
    rows: List[dict] = []
    with open(path, "r", encoding="utf-8") as fh:
        for line in fh:
            line = line.strip()
            if not line:
                continue
            try:
                rows.append(json.loads(line))
            except ValueError as exc:
                raise ValueError(f"invalid JSONL line in {path}: {exc}") from exc
    return rows


def build_report(
    candidates_path: str,
    output_path: str,
    state_path: str,
    config_path: str = "config/categories.json",
    *,
    min_score: int = DEFAULT_MIN_SCORE,
    min_sources: int = DEFAULT_MIN_SOURCES,
    max_sources: int = DEFAULT_MAX_SOURCES,
    fr_ratio: float = DEFAULT_FR_RATIO,
    update_state: bool = True,
    logger: Optional[logging.Logger] = None,
) -> Tuple[int, dict]:
    """Run the full pipeline. Returns (exit_code, report_meta)."""
    log = logger or logging.getLogger("report")
    if not os.path.exists(config_path):
        log.error("config file not found: %s", config_path)
        return 2, {"error": "missing config"}
    cfg = d.load_config(config_path)
    categories = d.flatten_categories(cfg)
    by_slug = {c["slug"]: c for c in categories}
    kw_index = {c["slug"]: keyword_set(c) for c in categories}

    candidates = load_candidates(candidates_path)
    log.info("loaded %d candidates from %s", len(candidates), candidates_path)

    seen_keys, seen_entries = load_seen(state_path)
    log.info("state %s: %d seen entries", state_path, len(seen_entries))

    kept: List[dict] = []
    seen_removed = 0
    dropped_no_cat = 0
    dropped_score = 0
    for row in candidates:
        if not all(row.get(f) is not None for f in d.REQUIRED_FIELDS):
            continue
        slug = row.get("category", "")
        pairs = kw_index.get(slug)
        if pairs is None:
            dropped_no_cat += 1
            continue
        score, matched = score_candidate(row, pairs)
        if score < min_score:
            dropped_score += 1
            continue
        row = dict(row)
        row["_score"] = score
        row["_matched"] = matched
        # Seen-source filter (canonical key or source_id).
        key = canonical_key(row)
        if key in seen_keys or f"source_id:{row.get('source_id', '')}" in seen_keys:
            seen_removed += 1
            continue
        kept.append(row)

    # Dedup within the batch: canonical key first, then one RSS source per domain.
    by_key: Dict[str, dict] = {}
    for row in kept:
        key = canonical_key(row)
        prev = by_key.get(key)
        if prev is None or row["_score"] > prev["_score"]:
            by_key[key] = row
    deduped = list(by_key.values())
    by_domain: Dict[str, dict] = {}
    for row in deduped:
        dkey = domain_key(row)
        if dkey is None:
            continue
        prev = by_domain.get(dkey)
        if prev is None or row["_score"] > prev["_score"]:
            by_domain[dkey] = row
    final: List[dict] = []
    for row in deduped:
        dkey = domain_key(row)
        if dkey is None or by_domain[dkey] is row:
            final.append(row)
    deduped = final
    log.info(
        "kept %d after score filter, %d after URL/channel dedup, %d after domain dedup "
        "(seen removed: %d)",
        len(kept), len(by_key), len(deduped), seen_removed,
    )

    selected, warnings = select_sources(
        deduped, min_sources=min_sources, max_sources=max_sources, fr_ratio=fr_ratio
    )
    for warning in warnings:
        log.warning("%s", warning)
    if not selected:
        log.error("no valid source to report")
        return 1, {"selected": 0, "warnings": warnings}

    stats = {"total": len(candidates), "kept": len(deduped), "seen_removed": seen_removed}
    report = render_report(selected, by_slug, stats=stats, warnings=warnings)
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as fh:
        fh.write(report)
    log.info("wrote report (%d sources) to %s", len(selected), output_path)

    if update_state:
        report_date = datetime.now(timezone.utc).strftime("%Y-%m-%d")
        added = update_seen(state_path, seen_entries, selected, report_date)
        log.info("state updated: %d new seen entries", added)

    fr_count = sum(1 for c in selected if c.get("language") == "fr")
    meta = {
        "selected": len(selected),
        "fr_count": fr_count,
        "fr_ratio": round(fr_count / len(selected), 3) if selected else 0.0,
        "warnings": warnings,
        "output": os.path.abspath(output_path),
    }
    return 0, meta


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        prog="src.report",
        description="Build the vigie24 10-20 source report from candidates.jsonl.",
    )
    parser.add_argument("--input", default="candidates.jsonl", help="candidates JSONL path")
    parser.add_argument("--output", default="report.md", help="output Markdown path")
    parser.add_argument("--state", default="state/seen_sources.json", help="seen-sources state JSON path")
    parser.add_argument("--config", default="config/categories.json", help="category taxonomy JSON")
    parser.add_argument("--min-score", type=int, default=DEFAULT_MIN_SCORE, help="minimum relevance score (default %(default)s)")
    parser.add_argument("--min-sources", type=int, default=DEFAULT_MIN_SOURCES, help="minimum sources before warning (default %(default)s)")
    parser.add_argument("--max-sources", type=int, default=DEFAULT_MAX_SOURCES, help="maximum sources in report (default %(default)s)")
    parser.add_argument("--fr-ratio", type=float, default=DEFAULT_FR_RATIO, help="minimum French share (default %(default)s)")
    parser.add_argument("--dry-run", action="store_true", help="generate the report without updating state")
    parser.add_argument("--verbose", action="store_true", help="debug logging")
    args = parser.parse_args(argv)

    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
    )
    log = logging.getLogger("report")

    if not 0 < args.fr_ratio <= 1:
        log.error("--fr-ratio must be in (0, 1]")
        return 2

    code, meta = build_report(
        args.input,
        args.output,
        args.state,
        args.config,
        min_score=args.min_score,
        min_sources=args.min_sources,
        max_sources=args.max_sources,
        fr_ratio=args.fr_ratio,
        update_state=not args.dry_run,
        logger=log,
    )
    if code == 0:
        log.info("report OK: %d sources (%d FR)", meta["selected"], meta["fr_count"])
    return code


if __name__ == "__main__":
    sys.exit(main())
