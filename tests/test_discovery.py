#!/usr/bin/env python3
"""Unit tests for src/discovery.py — normalization, dedup, rate limiting,
language detection, feed extraction and Google News parsing. No network."""
import json
import os
import sys
import tempfile
import time
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

from src import discovery as d  # noqa: E402

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONFIG = os.path.join(REPO_ROOT, "config", "categories.json")


class TestNormalize(unittest.TestCase):
    def test_all_required_fields_filled(self):
        row = d.normalize("rss", "rss:example", "Example", "https://example.com/feed", "ia", "fr")
        self.assertEqual(sorted(row.keys()), sorted(d.REQUIRED_FIELDS))
        for field in d.REQUIRED_FIELDS:
            if field in ("description", "subscriber_count"):
                continue
            self.assertTrue(str(row[field]) != "", field)
        self.assertIsInstance(row["subscriber_count"], int)
        self.assertIn("T", row["discovered_at"])  # ISO 8601 datetime

    def test_url_required_absolute(self):
        with self.assertRaises(ValueError):
            d.normalize("rss", "x", "T", "not-a-url", "ia", "fr")
        with self.assertRaises(ValueError):
            d.normalize("rss", "x", "T", "", "ia", "fr")

    def test_title_required(self):
        with self.assertRaises(ValueError):
            d.normalize("rss", "x", "  ", "https://example.com", "ia", "fr")

    def test_unknown_platform_rejected(self):
        with self.assertRaises(ValueError):
            d.normalize("tiktok", "x", "T", "https://example.com", "ia", "fr")

    def test_subscriber_count_coercion(self):
        self.assertEqual(d.normalize("rss", "x", "T", "https://a.example", "ia", "fr", subscriber_count="1234")["subscriber_count"], 1234)  # type: ignore[arg-type]
        self.assertEqual(d.normalize("rss", "x", "T", "https://a.example", "ia", "fr", subscriber_count=None)["subscriber_count"], 0)
        self.assertEqual(d.normalize("rss", "x", "T", "https://a.example", "ia", "fr", subscriber_count="abc")["subscriber_count"], 0)  # type: ignore[arg-type]
        self.assertEqual(d.normalize("rss", "x", "T", "https://a.example", "ia", "fr", subscriber_count=-5)["subscriber_count"], 0)

    def test_url_normalization(self):
        row = d.normalize("rss", "x", "T", "https://www.Example.com/path/?utm_source=tw#frag", "ia", "fr")
        self.assertEqual(row["url"], "https://example.com/path")
        row2 = d.normalize("rss", "x2", "T", "http://example.com:80/path/", "ia", "fr")
        self.assertEqual(row2["url"], "http://example.com/path")

    def test_language_fallback_detection(self):
        row = d.normalize("rss", "x", "Intelligence artificielle pour les entreprises", "https://a.example", "ia", "")
        self.assertEqual(row["language"], "fr")
        row = d.normalize("rss", "x", "Artificial intelligence for enterprises", "https://a.example", "ia", "")
        self.assertEqual(row["language"], "en")

    def test_source_id_and_discovered_at_passthrough(self):
        row = d.normalize("youtube", "youtube:UCabc", "Chan", "https://youtube.com/channel/UCabc", "ia", "fr", discovered_at="2026-08-29T12:00:00+00:00")
        self.assertEqual(row["source_id"], "youtube:UCabc")
        self.assertEqual(row["discovered_at"], "2026-08-29T12:00:00+00:00")


class TestDeduper(unittest.TestCase):
    def test_dedup_by_normalized_url(self):
        dup = d.Deduper()
        self.assertTrue(dup.add("https://Example.com/path/"))
        self.assertFalse(dup.add("https://example.com/path"))
        self.assertFalse(dup.add("https://example.com/path/?utm_source=x"))
        self.assertTrue(dup.add("https://example.com/other"))
        self.assertEqual(len(dup), 2)

    def test_empty_url_rejected(self):
        dup = d.Deduper()
        self.assertFalse(dup.add(""))


class TestRateLimiter(unittest.TestCase):
    def test_min_interval_enforced(self):
        rl = d.RateLimiter(0.25)
        t0 = time.monotonic()
        rl.wait()
        rl.wait()
        self.assertGreaterEqual(time.monotonic() - t0, 0.24)

    def test_first_call_immediate(self):
        rl = d.RateLimiter(10)
        t0 = time.monotonic()
        rl.wait()
        self.assertLess(time.monotonic() - t0, 0.5)


class TestLanguageDetection(unittest.TestCase):
    def test_french(self):
        self.assertEqual(d.detect_language("Intelligence artificielle et machine learning pour les entreprises françaises"), "fr")

    def test_english(self):
        self.assertEqual(d.detect_language("Artificial intelligence and machine learning for enterprises"), "en")

    def test_unknown(self):
        self.assertEqual(d.detect_language("xyz qwerty 123"), "unknown")
        self.assertEqual(d.detect_language(""), "unknown")


class TestCategories(unittest.TestCase):
    def test_flatten_categories_count(self):
        cfg = d.load_config(CONFIG)
        flat = d.flatten_categories(cfg)
        self.assertEqual(len(flat), 42)  # 8 parents + 34 subcategories
        parents = [c for c in flat if c.get("parent_slug") is None]
        self.assertEqual(len(parents), 8)
        for cat in flat:
            self.assertIn("slug", cat)
            self.assertIn("keywords", cat)
            for plat in d.PLATFORMS:
                self.assertIn(plat, cat["platforms"])
                self.assertTrue(cat["platforms"][plat], f"{cat['slug']} has no {plat} terms")

    def test_reddit_terms_parse(self):
        cfg = d.load_config(CONFIG)
        subs = d._reddit_terms(d.flatten_categories(cfg))
        all_names = [n for names in subs.values() for n in names]
        self.assertGreaterEqual(len(set(all_names)), 20)


class TestRssParsing(unittest.TestCase):
    GN_XML = b"""<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0"><channel><title>Test</title>
<item>
  <title>Grand titre tech</title>
  <link>https://news.google.com/rss/articles/CBI123</link>
  <source url="https://www.numerama.com/">Numerama</source>
</item>
<item>
  <title>Pas de source</title>
  <link>https://example.org/article-1</link>
</item>
<item>
  <title>Sans url</title>
</item>
</channel></rss>"""

    def test_google_news_parse_prefers_source(self):
        rows = d.parse_google_news_xml(self.GN_XML, category="ia", query="IA")
        self.assertEqual(len(rows), 2)
        self.assertEqual(rows[0]["title"], "Numerama")
        self.assertEqual(rows[0]["url"], "https://numerama.com/")
        self.assertEqual(rows[0]["platform"], "rss")
        self.assertEqual(rows[0]["category"], "ia")
        self.assertEqual(rows[0]["language"], "fr")
        self.assertIn("IA", rows[0]["description"])
        self.assertEqual(rows[1]["url"], "https://example.org/article-1")

    def test_google_news_parse_invalid_xml(self):
        self.assertEqual(d.parse_google_news_xml(b"not xml", category="ia", query="x"), [])

    def test_extract_feed_links(self):
        html = """<html><head>
          <link rel="alternate" type="application/rss+xml" title="RSS" href="/feed/">
          <link rel="alternate" type="application/atom+xml" href="https://cdn.example.com/atom.xml">
        </head></html>"""
        links = d.extract_feed_links(html, "https://www.example.com/blog/")
        self.assertEqual(len(links), 2)
        self.assertIn("https://www.example.com/feed/", links)
        self.assertIn("https://cdn.example.com/atom.xml", links)

    def test_extract_feed_links_none(self):
        self.assertEqual(d.extract_feed_links("<html><body>rien</body></html>", "https://a.example"), [])


class TestJsonl(unittest.TestCase):
    def test_roundtrip(self):
        rows = [
            d.normalize("rss", "a", "Titre un", "https://a.example/feed", "ia", "fr"),
            d.normalize("bluesky", "bluesky:did:plc:x", "Compte", "https://bsky.app/profile/x.bsky.social", "dev", "en", "desc"),
        ]
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "candidates.jsonl")
            d.write_jsonl(path, rows)
            with open(path, encoding="utf-8") as fh:
                loaded = [json.loads(line) for line in fh]
        self.assertEqual(loaded, rows)


class TestConnectorSignatures(unittest.TestCase):
    def test_each_connector_individually_callable(self):
        cfg = d.load_config(CONFIG)
        cats = d.flatten_categories(cfg)
        for name in d.PLATFORMS:
            connector = d.CONNECTORS[name]
            self.assertTrue(callable(connector), name)
        # Reddit offline path must produce candidates from the taxonomy without network.
        subs = d._reddit_terms(cats)
        offline = d._reddit_offline(subs, discovered_at="2026-08-29T12:00:00+00:00")
        self.assertGreaterEqual(len(offline), 20)
        for row in offline:
            self.assertEqual(sorted(row.keys()), sorted(d.REQUIRED_FIELDS))
            self.assertEqual(row["platform"], "reddit")
            self.assertTrue(row["url"].startswith("https://reddit.com/r/"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
