#!/usr/bin/env python3
"""Multi-platform source discovery for vigie24 (candidate generation).

Queries four platforms from the vigie24 category taxonomy
(``config/categories.json``) and normalizes every result into JSONL rows with a
fixed schema:

    source_id, title, url, platform, category, language, description,
    subscriber_count, discovered_at

Platforms
---------
- **rss**      Google News RSS (French edition) for keyword queries, plus feed
               link extraction (``<link rel="alternate" type="application/rss+xml">``)
               from a curated list of French technology sites, with common feed
               path probing as a fallback.
- **youtube**  YouTube Data API v3 ``search`` + ``channels`` (list) using an
               OAuth refresh token (no API key needed). Queries are biased
               toward French channels (``relevanceLanguage=fr``,
               ``regionCode=FR``). Tokens are read from a JSON file pointed to
               by ``--yt-tokens`` or the ``YT_TOKENS_PATH`` environment
               variable; if absent, the connector is skipped with a warning.
- **reddit**   Reddit JSON API (subreddit search + ``about.json``). Reddit
               blocks many datacenter IPs with a bot-wall (HTTP 403 + HTML);
               in that case the connector degrades to a curated offline list
               built from the ``platforms.reddit`` terms of the taxonomy,
               logging the fallback.
- **bluesky**  AT Protocol public API (``app.bsky.actor.searchActors``), no
               authentication required. Best-effort: failures are logged and
               skipped.

All connectors handle timeouts, retries, per-platform rate limiting, and the
pipeline deduplicates candidates by normalized URL. Every connector is
individually callable (``discover_rss``, ``discover_youtube``,
``discover_reddit``, ``discover_bluesky``).

Usage
-----
    python -m src.discovery --config config/categories.json --output candidates.jsonl
    python -m src.discovery --platform rss --platform bluesky --verbose
    python -m src.discovery --platform youtube --yt-tokens /opt/data/.yt_tokens.json

Exit codes: 0 = wrote at least one candidate, 1 = no candidates, 2 = bad usage.
"""
from __future__ import annotations

import argparse
import json
import logging
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from typing import Dict, List, Optional, Tuple

__version__ = "1.0.0"

PLATFORMS = ("rss", "youtube", "reddit", "bluesky")
REQUIRED_FIELDS = (
    "source_id",
    "title",
    "url",
    "platform",
    "category",
    "language",
    "description",
    "subscriber_count",
    "discovered_at",
)

USER_AGENT = "vigie24-discovery/1.0 (+https://vigie24.info; contact: philippe@walby.dev)"
DEFAULT_TIMEOUT = 12
DEFAULT_MIN_INTERVAL = 0.4

GOOGLE_NEWS_RSS = "https://news.google.com/rss/search"
YOUTUBE_TOKEN_URL = "https://oauth2.googleapis.com/token"
YOUTUBE_SEARCH_URL = "https://www.googleapis.com/youtube/v3/search"
YOUTUBE_CHANNELS_URL = "https://www.googleapis.com/youtube/v3/channels"
REDDIT_SUBREDDIT_SEARCH = "https://www.reddit.com/subreddits/search.json"
REDDIT_ABOUT = "https://www.reddit.com/r/{name}/about.json"
BSKY_SEARCH_ACTORS = "https://public.api.bsky.app/xrpc/app.bsky.actor.searchActors"

# Curated French technology sites used for RSS feed discovery
# (name, home URL, best-fit vigie24 parent category slug).
SEED_SITES = [
    ("Next INpact", "https://www.nextinpact.com/", "cybersecurite"),
    ("Numerama", "https://www.numerama.com/", "science"),
    ("01net", "https://www.01net.com/", "hardware"),
    ("Les Numeriques", "https://www.lesnumeriques.com/", "hardware"),
    ("ZDNet France", "https://www.zdnet.fr/", "developpement"),
    ("Developpez.com", "https://www.developpez.com/", "developpement"),
    ("Korben", "https://korben.info/", "divertissements"),
    ("Le Monde Informatique", "https://www.lemondeinformatique.fr/", "developpement"),
    ("Journal du Net", "https://www.journaldunet.com/", "developpement"),
    ("Clubic", "https://www.clubic.com/", "hardware"),
    ("Silicon.fr", "https://www.silicon.fr/", "developpement"),
    ("Le Big Data", "https://www.lebigdata.fr/", "ia"),
    ("ITespresso.fr", "https://www.itespresso.fr/", "developpement"),
    ("Frandroid", "https://www.frandroid.com/", "hardware"),
    ("PhonAndroid", "https://www.phonandroid.com/", "hardware"),
    ("Presse-citron", "https://www.presse-citron.net/", "hardware"),
    ("Generation NT", "https://www.generation-nt.com/", "hardware"),
    ("MacGeneration", "https://www.macg.co/", "hardware"),
    ("Siecle Digital", "https://siecledigital.fr/", "ia"),
    ("Framablog", "https://framablog.org/", "developpement"),
    ("LinuxFr", "https://linuxfr.org/", "developpement"),
]

# Common feed paths probed when a site's HTML exposes no <link> feed hint.
FEED_PROBES = ["/feed", "/feed.xml", "/rss", "/rss.xml", "/atom.xml", "/index.xml"]

# Subreddits known to be French-speaking (from the taxonomy or well known).
FR_SUBREDDITS = frozenset(
    {
        "programmation",
        "france",
        "sysadmin_fr",
        "actutech",
        "developpeurs",
        "developpement",
        "francedevelop",
        "informatiquefr",
    }
)

FR_STOPWORDS = frozenset(
    {
        "le", "la", "les", "des", "une", "un", "du", "au", "aux", "est", "sont",
        "dans", "pour", "sur", "avec", "que", "qui", "ce", "cette", "ces",
        "vous", "nous", "leur", "plus", "mais", "par", "en", "de", "se", "il",
        "elle", "pas", "aux", "tout", "sans", "son", "ses", "leur", "leurs",
    }
)
EN_STOPWORDS = frozenset(
    {
        "the", "and", "of", "to", "in", "is", "for", "on", "with", "as", "by",
        "at", "from", "are", "was", "be", "has", "have", "this", "that", "it",
        "you", "we", "they", "not", "but", "or", "an", "will", "can",
    }
)

_TRACKING_PARAMS = ("utm_", "fbclid", "gclid", "mc_cid", "mc_eid")


# ---------------------------------------------------------------------------
# Small utilities
# ---------------------------------------------------------------------------

def _log(logger: Optional[logging.Logger]) -> logging.Logger:
    return logger or logging.getLogger("discovery")


def _quiet_logger() -> logging.Logger:
    lg = logging.getLogger("discovery.quiet")
    if not lg.handlers:
        lg.addHandler(logging.NullHandler())
    return lg


def _slugify(text: str, max_len: int = 80) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")
    return slug[:max_len] or "unknown"


def _localname(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def detect_language(text: str) -> str:
    """Very light French/English detection based on stopword counts."""
    words = re.findall(r"[a-zàâäéèêëîïôöùûüç'-]+", (text or "").lower())
    if not words:
        return "unknown"
    fr_hits = sum(1 for w in words if w in FR_STOPWORDS)
    en_hits = sum(1 for w in words if w in EN_STOPWORDS)
    if fr_hits > en_hits and fr_hits > 0:
        return "fr"
    if en_hits > fr_hits and en_hits > 0:
        return "en"
    return "unknown"


def normalize_url(url: str) -> str:
    """Canonical URL: lowercase scheme/host, drop www., default ports, trailing
    slash on path and common tracking parameters. Keeps fragments? No."""
    if not url:
        return ""
    u = url.strip()
    if u.startswith("//"):
        u = "https:" + u
    parts = urllib.parse.urlsplit(u)
    scheme = (parts.scheme or "https").lower()
    netloc = parts.netloc.lower()
    if scheme == "http" and netloc.endswith(":80"):
        netloc = netloc[:-3]
    elif scheme == "https" and netloc.endswith(":443"):
        netloc = netloc[:-4]
    if netloc.startswith("www."):
        netloc = netloc[4:]
    path = parts.path.rstrip("/") or "/"
    query = "&".join(
        kv for kv in parts.query.split("&")
        if kv and not kv.split("=", 1)[0].lower().startswith(_TRACKING_PARAMS)
    )
    return urllib.parse.urlunsplit((scheme, netloc, path, query, ""))


def normalize(
    platform: str,
    source_id: str,
    title: str,
    url: str,
    category: str,
    language: str,
    description: str = "",
    subscriber_count: Optional[int] = None,
    discovered_at: Optional[str] = None,
) -> dict:
    """Build a normalized candidate row. Every required field is validated."""
    if platform not in PLATFORMS:
        raise ValueError(f"unknown platform: {platform!r}")
    if not source_id:
        raise ValueError("source_id is required")
    if not title or not str(title).strip():
        raise ValueError("title is required")
    canon = normalize_url(url)
    parsed = urllib.parse.urlsplit(canon)
    if not parsed.netloc:
        raise ValueError(f"url must be absolute: {url!r}")
    if not category:
        raise ValueError("category is required")
    lang = language or detect_language(f"{title} {description}")
    try:
        subs = int(subscriber_count or 0)
    except (TypeError, ValueError):
        subs = 0
    if subs < 0:
        subs = 0
    return {
        "source_id": str(source_id).strip(),
        "title": str(title).strip(),
        "url": canon,
        "platform": platform,
        "category": str(category),
        "language": lang if lang in ("fr", "en") else "unknown",
        "description": (description or "").strip(),
        "subscriber_count": subs,
        "discovered_at": discovered_at or datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }


class RateLimiter:
    """Min-interval gate so callers never hammer a platform."""

    def __init__(self, min_interval: float = DEFAULT_MIN_INTERVAL):
        self.min_interval = min_interval
        self._last = 0.0

    def wait(self) -> None:
        now = time.monotonic()
        delta = self._last + self.min_interval - now
        if delta > 0:
            time.sleep(delta)
        self._last = time.monotonic()


class Deduper:
    """URL-keyed deduplication over normalized URLs."""

    def __init__(self):
        self._seen = set()

    def add(self, url: str) -> bool:
        key = normalize_url(url)
        if not key:
            return False
        if key in self._seen:
            return False
        self._seen.add(key)
        return True

    def __len__(self) -> int:
        return len(self._seen)


# ---------------------------------------------------------------------------
# HTTP helpers
# ---------------------------------------------------------------------------

def fetch_bytes(
    url: str,
    params: Optional[dict] = None,
    headers: Optional[dict] = None,
    timeout: float = DEFAULT_TIMEOUT,
    retries: int = 1,
    limiter: Optional[RateLimiter] = None,
) -> Tuple[Optional[bytes], Optional[int]]:
    """GET and return (body, http_status). (None, status) on any failure."""
    if limiter is not None:
        limiter.wait()
    if params:
        url = url + ("&" if "?" in url else "?") + urllib.parse.urlencode(params)
    hdrs = {"User-Agent": USER_AGENT, "Accept": "*/*"}
    if headers:
        hdrs.update(headers)
    last_status = None
    for attempt in range(retries + 1):
        try:
            req = urllib.request.Request(url, headers=hdrs)
            with urllib.request.urlopen(req, timeout=timeout) as resp:
                last_status = resp.getcode()
                return resp.read(), last_status
        except urllib.error.HTTPError as e:
            last_status = e.code
            time.sleep(0.4 * (attempt + 1))
        except Exception:
            time.sleep(0.5 * (attempt + 1))
    return None, last_status


def fetch_json(
    url: str,
    params: Optional[dict] = None,
    headers: Optional[dict] = None,
    timeout: float = DEFAULT_TIMEOUT,
    retries: int = 1,
    limiter: Optional[RateLimiter] = None,
) -> Tuple[Optional[dict], Optional[int]]:
    """GET and parse a JSON object. Returns (data, status); data is None when
    the response is not JSON (e.g. Reddit bot-wall HTML) or on failure."""
    body, status = fetch_bytes(url, params=params, headers=headers, timeout=timeout, retries=retries, limiter=limiter)
    if body is None:
        return None, status
    try:
        return json.loads(body.decode("utf-8", "replace")), status
    except (ValueError, UnicodeDecodeError):
        return None, status


# ---------------------------------------------------------------------------
# Config loading
# ---------------------------------------------------------------------------

def load_config(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as fh:
        return json.load(fh)


def flatten_categories(cfg: dict) -> List[dict]:
    """Flatten parents + subcategories into a list of category dicts."""
    flat: List[dict] = []
    for cat in cfg.get("categories", []):
        flat.append(cat)
        flat.extend(cat.get("subcategories", []))
    return flat


# ---------------------------------------------------------------------------
# RSS connector (Google News RSS + feed link extraction)
# ---------------------------------------------------------------------------

def _child_text(item: ET.Element, name: str) -> str:
    for ch in item:
        if _localname(ch.tag) == name:
            return (ch.text or "").strip()
    return ""


def parse_google_news_xml(
    raw: bytes,
    *,
    category: str,
    query: str,
    discovered_at: Optional[str] = None,
) -> List[dict]:
    """Parse a Google News RSS document into normalized candidates. Each item
    becomes a candidate: the outlet itself when a <source url> is present,
    otherwise the article URL."""
    out: List[dict] = []
    try:
        root = ET.fromstring(raw)
    except ET.ParseError:
        return out
    for item in root.iter():
        if _localname(item.tag) != "item":
            continue
        title = _child_text(item, "title")
        link = _child_text(item, "link")
        src_name, src_url = "", ""
        for ch in item:
            if _localname(ch.tag) == "source":
                src_name = (ch.text or "").strip()
                src_url = (ch.attrib.get("url") or "").strip()
                break
        url = src_url or link
        if not url:
            continue
        if src_name:
            cand_title = src_name
            desc = f"Source « {src_name} » détectée via Google Actualités (requête « {query} »)"
        else:
            cand_title = title
            desc = f"Article détecté via Google Actualités (requête « {query} »)"
        out.append(
            normalize(
                "rss",
                f"rss:{_slugify(url)}",
                cand_title,
                url,
                category,
                "fr",
                desc,
                0,
                discovered_at,
            )
        )
    return out


_FEED_LINK_RE = re.compile(
    r'<link[^>]+type\s*=\s*["\']application/(?:rss|atom)\+xml["\'][^>]*>', re.IGNORECASE
)
_HREF_RE = re.compile(r'href\s*=\s*["\']([^"\']+)["\']', re.IGNORECASE)


def extract_feed_links(html: str, base_url: str) -> List[str]:
    """Find RSS/Atom feed URLs declared via <link> tags in HTML."""
    links: List[str] = []
    for match in _FEED_LINK_RE.finditer(html):
        href = _HREF_RE.search(match.group(0))
        if href:
            links.append(urllib.parse.urljoin(base_url, href.group(1)))
    # de-duplicate while preserving order
    seen = set()
    unique = []
    for link in links:
        key = normalize_url(link)
        if key and key not in seen:
            seen.add(key)
            unique.append(link)
    return unique


def probe_feed_paths(
    site_url: str,
    *,
    limiter: Optional[RateLimiter] = None,
    logger: Optional[logging.Logger] = None,
    budget: int = 4,
) -> List[str]:
    """Probe common feed paths when no <link> hint was found."""
    log = _log(logger)
    found: List[str] = []
    base = site_url.rstrip("/")
    for path in FEED_PROBES:
        if len(found) >= budget:
            break
        url = base + path
        body, status = fetch_bytes(url, limiter=limiter, headers={"Accept": "application/rss+xml, application/xml, text/xml, */*"})
        if body is None:
            continue
        head = body[:512].decode("utf-8", "replace").lower()
        if "<rss" in head or "<feed" in head or "<rdf:rdf" in head:
            found.append(url)
            log.debug("feed probe hit: %s", url)
    return found


def discover_rss(
    categories: List[dict],
    *,
    limiter: Optional[RateLimiter] = None,
    logger: Optional[logging.Logger] = None,
    discovered_at: Optional[str] = None,
    max_queries: int = 36,
    seed_sites: Optional[List[Tuple[str, str, str]]] = None,
    probe_feeds: bool = True,
    probe_budget: int = 4,
) -> List[dict]:
    """Discover RSS/Atom sources: Google News RSS queries (French edition) plus
    feed link extraction from a curated list of French technology sites."""
    log = _log(logger)
    out: List[dict] = []

    # Phase A: Google News RSS. Parents get two queries, subcategories one.
    queries: List[Tuple[str, str]] = []
    for cat in categories:
        kws = cat.get("platforms", {}).get("rss", [])
        if not kws:
            continue
        if cat.get("parent_slug") is None and len(kws) > 1:
            queries.append((cat["slug"], kws[0]))
            queries.append((cat["slug"], kws[1]))
        else:
            queries.append((cat["slug"], kws[0]))
    for cat_slug, kw in queries[:max_queries]:
        params = {"q": kw, "hl": "fr", "gl": "FR", "ceid": "FR:fr"}
        body, status = fetch_bytes(
            GOOGLE_NEWS_RSS,
            params=params,
            headers={"Accept": "application/rss+xml, application/xml, text/xml, */*"},
            limiter=limiter,
        )
        if body is None:
            log.debug("google news query failed (status=%s): %s", status, kw)
            continue
        out.extend(parse_google_news_xml(body, category=cat_slug, query=kw, discovered_at=discovered_at))
    log.info("rss: %d candidates from Google News (fr)", len(out))

    # Phase B: feed link extraction from curated French tech sites.
    for name, site, site_cat in (seed_sites or SEED_SITES):
        body, status = fetch_bytes(site, limiter=limiter)
        if body is None:
            log.debug("seed site unreachable (%s): %s", status, site)
            continue
        html = body.decode("utf-8", "replace")
        links = extract_feed_links(html, site)
        if not links and probe_feeds:
            links = probe_feed_paths(site, limiter=limiter, logger=logger, budget=probe_budget)
        for link in links[:3]:
            out.append(
                normalize(
                    "rss",
                    f"rss:{_slugify(name)}",
                    f"{name} (flux RSS)",
                    link,
                    site_cat,
                    "fr",
                    f"Flux RSS de {name} — site technologique francophone (catégorie « {site_cat} ») détecté par extraction de liens feed.",
                    0,
                    discovered_at,
                )
            )
    log.info("rss: %d total candidates (Google News + %d seed feeds)", len(out), len(seed_sites or SEED_SITES))
    return out


# ---------------------------------------------------------------------------
# YouTube connector (Data API v3 via OAuth refresh token)
# ---------------------------------------------------------------------------

def _yt_refresh_access_token(tokens_path: str, timeout: float = DEFAULT_TIMEOUT) -> Optional[str]:
    """Exchange the stored refresh token for a fresh access token (no deps)."""
    try:
        with open(tokens_path, "r", encoding="utf-8") as fh:
            tok = json.load(fh)
    except (OSError, ValueError):
        return None
    body = urllib.parse.urlencode(
        {
            "grant_type": "refresh_token",
            "refresh_token": tok.get("refresh_token", ""),
            "client_id": tok.get("client_id", ""),
            "client_secret": tok.get("client_secret", ""),
        }
    ).encode("utf-8")
    req = urllib.request.Request(
        tok.get("token_uri", YOUTUBE_TOKEN_URL),
        data=body,
        headers={"Content-Type": "application/x-www-form-urlencoded", "User-Agent": USER_AGENT},
    )
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            data = json.loads(resp.read().decode("utf-8", "replace"))
            return data.get("access_token")
    except Exception:
        return None


def discover_youtube(
    categories: List[dict],
    *,
    limiter: Optional[RateLimiter] = None,
    logger: Optional[logging.Logger] = None,
    discovered_at: Optional[str] = None,
    tokens_path: Optional[str] = None,
    max_queries: int = 18,
    max_results: int = 8,
    max_pages: int = 2,
) -> List[dict]:
    """Discover YouTube channels via the Data API v3 search endpoint, biased
    toward French channels (relevanceLanguage=fr, regionCode=FR)."""
    log = _log(logger)
    if not tokens_path or not os.path.exists(tokens_path):
        log.warning("youtube: no OAuth tokens file (use --yt-tokens or YT_TOKENS_PATH) — skipping")
        return []
    token = _yt_refresh_access_token(tokens_path)
    if not token:
        log.warning("youtube: token refresh failed — skipping")
        return []
    auth = {"Authorization": f"Bearer {token}"}

    queries: List[Tuple[str, str]] = []
    for cat in categories:
        kws = cat.get("platforms", {}).get("youtube", [])
        if kws:
            queries.append((cat["slug"], kws[0]))

    channel_ids: List[str] = []
    cat_of: Dict[str, str] = {}
    kw_of: Dict[str, str] = {}
    for cat_slug, kw in queries[:max_queries]:
        params = {
            "part": "snippet",
            "type": "channel",
            "q": kw,
            "maxResults": max_results,
            "relevanceLanguage": "fr",
            "regionCode": "FR",
        }
        page = 0
        page_token = None
        while page < max_pages:
            if page_token:
                params["pageToken"] = page_token
            data, status = fetch_json(YOUTUBE_SEARCH_URL, params=params, headers=auth, limiter=limiter)
            if data is None:
                if status in (403, 429):
                    log.warning("youtube: API returned %s (quota or rate limit) — stopping YouTube this run", status)
                    return _youtube_build(channel_ids, cat_of, kw_of, auth, limiter, discovered_at, log)
                log.warning("youtube: search failed (status=%s) for query %r — skipping query", status, kw)
                break
            for item in data.get("items", []):
                cid = (item.get("id") or {}).get("channelId")
                if cid and cid not in channel_ids:
                    channel_ids.append(cid)
                    cat_of[cid] = cat_slug
                    kw_of[cid] = kw
            page_token = data.get("nextPageToken")
            page += 1
            if not page_token:
                break
    log.info("youtube: %d unique channels found", len(channel_ids))
    return _youtube_build(channel_ids, cat_of, kw_of, auth, limiter, discovered_at, log)


def _youtube_build(
    channel_ids: List[str],
    cat_of: Dict[str, str],
    kw_of: Dict[str, str],
    auth: dict,
    limiter: Optional[RateLimiter],
    discovered_at: Optional[str],
    log: logging.Logger,
) -> List[dict]:
    """Fetch channel metadata + statistics and normalize candidates."""
    out: List[dict] = []
    default_cat = next(iter(cat_of.values()), "ia")
    for i in range(0, len(channel_ids), 50):
        batch = channel_ids[i : i + 50]
        data, status = fetch_json(
            YOUTUBE_CHANNELS_URL,
            params={"part": "snippet,statistics", "id": ",".join(batch)},
            headers=auth,
            limiter=limiter,
        )
        if data is None:
            log.warning("youtube: channels.list failed (status=%s)", status)
            continue
        for item in data.get("items", []):
            cid = item.get("id", "")
            snippet = item.get("snippet", {})
            title = snippet.get("title", "") or cid
            desc = snippet.get("description", "") or ""
            country = snippet.get("country", "") or ""
            stats = item.get("statistics", {}) or {}
            try:
                subs = int(stats.get("subscriberCount", 0) or 0)
            except (TypeError, ValueError):
                subs = 0
            lang = "fr" if country == "FR" else detect_language(f"{title} {desc}")
            if not desc:
                desc = f"Chaîne YouTube détectée via la recherche vigie24 (requête « {kw_of.get(cid, '')} »)."
            out.append(
                normalize(
                    "youtube",
                    f"youtube:{cid}",
                    title,
                    f"https://www.youtube.com/channel/{cid}",
                    cat_of.get(cid, default_cat),
                    lang,
                    desc,
                    subs,
                    discovered_at,
                )
            )
    return out


# ---------------------------------------------------------------------------
# Reddit connector (JSON API with curated offline fallback)
# ---------------------------------------------------------------------------

def _reddit_terms(categories: List[dict]) -> Dict[str, List[str]]:
    subs_by_cat: Dict[str, List[str]] = {}
    for cat in categories:
        for term in cat.get("platforms", {}).get("reddit", []):
            name = term.strip().lstrip("r/")
            if re.fullmatch(r"[A-Za-z0-9_\-]{2,}", name):
                subs_by_cat.setdefault(cat["slug"], []).append(name)
    return subs_by_cat


def _reddit_live(
    subs_by_cat: Dict[str, List[str]],
    *,
    limiter: Optional[RateLimiter],
    discovered_at: Optional[str],
    log: logging.Logger,
    max_terms: int = 30,
    max_pages: int = 2,
) -> List[dict]:
    """Live Reddit JSON API mode: subreddit search per term, with pagination."""
    out: List[dict] = []
    seen: set = set()
    for cat_slug, names in subs_by_cat.items():
        for name in names[:1]:
            if len(out) >= max_terms * 10:
                break
            cursor = None
            for page in range(max_pages):
                params = {"q": name, "limit": 25}
                if cursor:
                    params["after"] = cursor
                data, status = fetch_json(REDDIT_SUBREDDIT_SEARCH, params=params, limiter=limiter)
                if data is None:
                    break
                children = (data.get("data") or {}).get("children", [])
                for child in children:
                    d = child.get("data", {})
                    display = d.get("display_name", "")
                    if not display or display.lower() in seen:
                        continue
                    seen.add(display.lower())
                    url = d.get("url") or f"https://www.reddit.com/r/{display}/"
                    lang = "fr" if display.lower() in FR_SUBREDDITS else detect_language(f"{d.get('title','')} {d.get('public_description','')}")
                    out.append(
                        normalize(
                            "reddit",
                            f"reddit:{display}",
                            f"r/{display}",
                            url,
                            cat_slug,
                            lang,
                            (d.get("public_description") or d.get("title") or "").strip(),
                            int(d.get("subscribers", 0) or 0),
                            discovered_at,
                        )
                    )
                cursor = (data.get("data") or {}).get("after")
                if not cursor:
                    break
    return out


def _reddit_offline(
    subs_by_cat: Dict[str, List[str]],
    *,
    discovered_at: Optional[str],
) -> List[dict]:
    """Curated offline fallback when the Reddit API is unreachable: emits a
    candidate per distinct subreddit name found in the taxonomy, with honest
    zeroed metadata."""
    out: List[dict] = []
    seen: set = set()
    for cat_slug, names in subs_by_cat.items():
        for name in names:
            key = name.lower()
            if key in seen:
                continue
            seen.add(key)
            lang = "fr" if key in FR_SUBREDDITS else detect_language(name)
            if lang == "unknown":
                lang = "en"
            out.append(
                normalize(
                    "reddit",
                    f"reddit:{name}",
                    f"r/{name}",
                    f"https://www.reddit.com/r/{name}/",
                    cat_slug,
                    lang,
                    f"Subreddit r/{name} — découvert via la taxonomie vigie24 (API Reddit inaccessible : métadonnées hors ligne).",
                    0,
                    discovered_at,
                )
            )
    return out


def discover_reddit(
    categories: List[dict],
    *,
    limiter: Optional[RateLimiter] = None,
    logger: Optional[logging.Logger] = None,
    discovered_at: Optional[str] = None,
    max_terms: int = 30,
) -> List[dict]:
    """Discover subreddits. Tries the live JSON API; degrades to a curated
    offline list (built from the taxonomy's reddit terms) when Reddit serves a
    bot-wall or is unreachable."""
    log = _log(logger)
    subs_by_cat = _reddit_terms(categories)
    total = sum(len(v) for v in subs_by_cat.values())
    data, status = fetch_json(
        REDDIT_SUBREDDIT_SEARCH, params={"q": "programmation", "limit": 3}, limiter=limiter
    )
    live_ok = (
        isinstance(data, dict)
        and bool((data.get("data") or {}).get("children"))
    )
    if live_ok:
        log.info("reddit: JSON API reachable — live mode")
        return _reddit_live(subs_by_cat, limiter=limiter, discovered_at=discovered_at, log=log, max_terms=max_terms)
    log.warning(
        "reddit: JSON API unavailable (status=%s) — using curated offline fallback (%d subreddits)",
        status, total,
    )
    return _reddit_offline(subs_by_cat, discovered_at=discovered_at)


# ---------------------------------------------------------------------------
# Bluesky connector (AT Protocol public API)
# ---------------------------------------------------------------------------

def discover_bluesky(
    categories: List[dict],
    *,
    limiter: Optional[RateLimiter] = None,
    logger: Optional[logging.Logger] = None,
    discovered_at: Optional[str] = None,
    max_terms: int = 10,
    max_pages: int = 2,
) -> List[dict]:
    """Discover Bluesky accounts via the public searchActors endpoint. No auth
    required. Best-effort: failures are logged and skipped."""
    log = _log(logger)
    terms: List[Tuple[str, str]] = []
    for cat in categories:
        for term in cat.get("platforms", {}).get("bluesky", []):
            q = term.lstrip("#").strip()
            if q:
                terms.append((cat["slug"], q))
    out: List[dict] = []
    seen_handles: set = set()
    for cat_slug, q in terms[:max_terms]:
        cursor = None
        for page in range(max_pages):
            params = {"q": q, "limit": 25}
            if cursor:
                params["cursor"] = cursor
            data, status = fetch_json(BSKY_SEARCH_ACTORS, params=params, limiter=limiter)
            if data is None:
                log.debug("bluesky search failed (status=%s): %s", status, q)
                break
            actors = data.get("actors", [])
            if not actors:
                break
            for actor in actors:
                handle = actor.get("handle", "")
                if not handle or handle.lower() in seen_handles:
                    continue
                seen_handles.add(handle.lower())
                did = actor.get("did", handle)
                display = actor.get("displayName", "") or handle
                desc = actor.get("description", "") or ""
                lang = detect_language(f"{display} {desc}")
                if not desc:
                    desc = f"Compte Bluesky détecté via la recherche vigie24 (requête « {q} »)."
                out.append(
                    normalize(
                        "bluesky",
                        f"bluesky:{did}",
                        display,
                        f"https://bsky.app/profile/{handle}",
                        cat_slug,
                        lang,
                        desc,
                        0,
                        discovered_at,
                    )
                )
            cursor = data.get("cursor")
            if not cursor:
                break
    log.info("bluesky: %d candidates (%d terms searched)", len(out), min(len(terms), max_terms))
    return out


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

CONNECTORS = {
    "rss": discover_rss,
    "youtube": discover_youtube,
    "reddit": discover_reddit,
    "bluesky": discover_bluesky,
}


def write_jsonl(path: str, rows: List[dict]) -> None:
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        for row in rows:
            fh.write(json.dumps(row, ensure_ascii=False) + "\n")


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        prog="src.discovery",
        description="Multi-platform source discovery for vigie24 (RSS, YouTube, Reddit, Bluesky).",
    )
    parser.add_argument("--config", default="config/categories.json", help="category taxonomy JSON")
    parser.add_argument("--output", default="candidates.jsonl", help="output JSONL path")
    parser.add_argument("--platform", action="append", choices=list(PLATFORMS), help="restrict to a platform (repeatable)")
    parser.add_argument("--yt-tokens", default=os.environ.get("YT_TOKENS_PATH"), help="YouTube OAuth tokens JSON (refresh token)")
    parser.add_argument("--min-interval", type=float, default=DEFAULT_MIN_INTERVAL, help="min seconds between HTTP requests")
    parser.add_argument("--max-queries", type=int, default=36, help="Google News query budget")
    parser.add_argument("--verbose", action="store_true", help="debug logging")
    args = parser.parse_args(argv)

    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
    )
    log = logging.getLogger("discovery")

    if not os.path.exists(args.config):
        log.error("config file not found: %s", args.config)
        return 2
    cfg = load_config(args.config)
    categories = flatten_categories(cfg)
    log.info("Loaded %d categories from %s", len(categories), args.config)

    discovered_at = datetime.now(timezone.utc).isoformat(timespec="seconds")
    limiter = RateLimiter(args.min_interval)
    dedup = Deduper()
    platforms = args.platform or list(PLATFORMS)

    results: List[dict] = []
    for platform in platforms:
        connector = CONNECTORS[platform]
        try:
            raw = connector(categories, limiter=limiter, logger=log, discovered_at=discovered_at, tokens_path=args.yt_tokens)
        except TypeError:
            # Connectors that do not accept tokens_path (defensive; all do today).
            raw = connector(categories, limiter=limiter, logger=log, discovered_at=discovered_at)
        except Exception as exc:  # best-effort: a failing connector never kills the run
            log.error("%s connector failed: %s", platform, exc)
            raw = []
        before = len(raw)
        kept = [c for c in raw if dedup.add(c["url"])]
        log.info("%s: %d raw candidates, %d kept after URL dedup", platform, before, len(kept))
        results.extend(kept)

    results.sort(key=lambda c: (c["platform"], c["category"], c["url"]))
    write_jsonl(args.output, results)
    log.info("Wrote %d candidates to %s", len(results), args.output)
    return 0 if results else 1


if __name__ == "__main__":
    sys.exit(main())
