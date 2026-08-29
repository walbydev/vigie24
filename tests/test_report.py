#!/usr/bin/env python3
"""Unit tests for src/report.py — scoring, dedup, seen-state, selection,
report rendering and the CLI pipeline. No network."""
import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

from src import report as r  # noqa: E402

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONFIG = os.path.join(REPO_ROOT, "config", "categories.json")

IA_CAT = {
    "slug": "ia",
    "name": "IA",
    "name_en": "AI",
    "keywords": {"fr": ["intelligence artificielle", "machine learning", "IA"], "en": ["artificial intelligence", "AI"]},
    "platforms": {"rss": ["intelligence artificielle"], "youtube": ["IA"], "reddit": ["programmation"], "bluesky": ["#IA"]},
}


def make_row(**over):
    base = {
        "source_id": "rss:1",
        "title": "Exemple IA",
        "url": "https://example.com/feed",
        "platform": "rss",
        "category": "ia",
        "language": "fr",
        "description": "Actualités intelligence artificielle et machine learning.",
        "subscriber_count": 0,
        "discovered_at": "2026-08-29T12:00:00+00:00",
    }
    base.update(over)
    return base


class TestScoring(unittest.TestCase):
    def test_fr_keyword_hit_scores(self):
        score, matched = r.score_candidate(make_row(), r.keyword_set(IA_CAT))
        self.assertGreaterEqual(score, 4)  # 2 hits + fr bonus
        self.assertIn("intelligence artificielle", matched)

    def test_fr_bonus_over_en(self):
        fr = r.score_candidate(make_row(description="intelligence artificielle"), r.keyword_set(IA_CAT))
        en = r.score_candidate(make_row(language="en", description="artificial intelligence"), r.keyword_set(IA_CAT))
        self.assertGreater(fr[0], en[0])

    def test_no_keyword_hit_scores_low(self):
        score, matched = r.score_candidate(
            make_row(title="Armando Iannucci", description="Words and smiles."), r.keyword_set(IA_CAT)
        )
        self.assertEqual(matched, [])
        self.assertEqual(score, 2)  # fr language only

    def test_short_keyword_word_boundary(self):
        # "IA" must not match "via" or "matériaux"
        score, matched = r.score_candidate(
            make_row(title="Via les matériaux", description="rien d'autre"), r.keyword_set(IA_CAT)
        )
        self.assertNotIn("IA", matched)

    def test_accent_insensitive_match(self):
        cat = {"slug": "x", "name": "X", "name_en": "X", "keywords": {"fr": ["cybersécurité"], "en": []}, "platforms": {}}
        _, matched = r.score_candidate(
            {"title": "La cybersecurite des entreprises", "description": "", "language": "fr"},
            r.keyword_set(cat),
        )
        self.assertIn("cybersécurité", matched)


class TestCanonicalKey(unittest.TestCase):
    def test_rss_normalized_url(self):
        self.assertEqual(
            r.canonical_key(make_row(url="https://www.Example.com/feed/?utm_source=x")),
            "rss:https://example.com/feed",
        )

    def test_youtube_channel_id(self):
        row = make_row(platform="youtube", url="https://www.youtube.com/channel/UCabcXYZ/")
        self.assertEqual(r.canonical_key(row), "youtube:ucabcxyz")

    def test_youtube_handle(self):
        row = make_row(platform="youtube", url="https://youtube.com/@Korben")
        self.assertEqual(r.canonical_key(row), "youtube:@korben")

    def test_reddit_subreddit_case_insensitive(self):
        row = make_row(platform="reddit", url="https://www.reddit.com/r/Programmation/")
        self.assertEqual(r.canonical_key(row), "reddit:programmation")

    def test_bluesky_handle(self):
        row = make_row(platform="bluesky", url="https://bsky.app/profile/Korben.bsky.social")
        self.assertEqual(r.canonical_key(row), "bluesky:korben.bsky.social")

    def test_domain_key_rss_only(self):
        self.assertEqual(r.domain_key(make_row(url="https://www.korben.info/feed")), "domain:korben.info")
        self.assertIsNone(r.domain_key(make_row(platform="reddit", url="https://www.reddit.com/r/x/")))
        self.assertIsNone(r.domain_key(make_row(url="https://news.google.com/rss/articles/x")))


class TestSelection(unittest.TestCase):
    def _pool(self, n_fr, n_en):
        rows = []
        for i in range(n_fr):
            rows.append(make_row(source_id=f"rss:fr{i}", title=f"Source FR {i}", category="ia", language="fr", _score=6))
        for i in range(n_en):
            rows.append(make_row(source_id=f"rss:en{i}", title=f"Source EN {i}", category="ia", language="en", _score=4))
        return rows

    def test_caps_at_max_sources(self):
        selected, warnings = r.select_sources(self._pool(40, 40), max_sources=20)
        self.assertEqual(len(selected), 20)
        self.assertEqual(warnings, [])

    def test_at_least_60_percent_french(self):
        selected, _ = r.select_sources(self._pool(40, 40), max_sources=20, fr_ratio=0.6)
        fr = sum(1 for c in selected if c["language"] == "fr")
        self.assertGreaterEqual(fr, 12)

    def test_under_min_sources_warns(self):
        selected, warnings = r.select_sources(self._pool(4, 1), min_sources=10)
        self.assertEqual(len(selected), 5)
        self.assertTrue(any("rapport partiel" in w for w in warnings))

    def test_no_candidates_warns(self):
        selected, warnings = r.select_sources([])
        self.assertEqual(selected, [])
        self.assertTrue(any("Aucune" in w for w in warnings))

    def test_category_round_robin_diversity(self):
        rows = []
        for i in range(8):
            rows.append(make_row(source_id=f"rss:a{i}", title=f"A{i}", category="ia", language="fr", _score=6))
            rows.append(make_row(source_id=f"rss:b{i}", title=f"B{i}", category="science", language="fr", _score=6))
        selected, _ = r.select_sources(rows, max_sources=10)
        cats = {c["category"] for c in selected}
        self.assertEqual(cats, {"ia", "science"})
        counts = {"ia": 0, "science": 0}
        for c in selected:
            counts[c["category"]] += 1
        self.assertLessEqual(abs(counts["ia"] - counts["science"]), 1)


class TestSeenState(unittest.TestCase):
    def _write_state(self, path, entries):
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as fh:
            json.dump({"schema_version": "1.0", "seen": entries}, fh)

    def test_load_seen_keys_and_source_ids(self):
        with tempfile.TemporaryDirectory() as td:
            path = os.path.join(td, "seen.json")
            self._write_state(path, [{"key": "rss:https://example.com/feed", "source_id": "rss:1"}])
            keys, entries = r.load_seen(path)
            self.assertIn("rss:https://example.com/feed", keys)
            self.assertIn("source_id:rss:1", keys)
            self.assertEqual(len(entries), 1)

    def test_missing_state_is_empty(self):
        keys, entries = r.load_seen("/nonexistent/seen.json")
        self.assertEqual((keys, entries), (set(), []))

    def test_update_seen_merges_and_is_atomic(self):
        with tempfile.TemporaryDirectory() as td:
            path = os.path.join(td, "state", "seen.json")
            self._write_state(path, [{"key": "rss:https://old.example", "seen_at": "2026-01-01T00:00:00+00:00"}])
            entries = [{"key": "rss:https://old.example", "seen_at": "2026-01-01T00:00:00+00:00"}]
            added = r.update_seen(path, entries, [make_row()], "2026-08-29")
            self.assertEqual(added, 1)
            keys, entries2 = r.load_seen(path)
            self.assertIn("rss:https://example.com/feed", keys)
            self.assertEqual(len(entries2), 2)
            # Re-running must not duplicate
            added2 = r.update_seen(path, entries2, [make_row()], "2026-08-30")
            self.assertEqual(added2, 0)
            self.assertEqual(len(r.load_seen(path)[1]), 2)


class TestRender(unittest.TestCase):
    def test_report_structure(self):
        rows = [
            make_row(title="Monde Numérique", url="https://monde.example/feed", description="Le meilleur de l'IA en français."),
            make_row(source_id="rss:2", title="Tech Weekly", url="https://tech.example/feed", language="en", category="science"),
        ]
        md = r.render_report(rows, {"ia": {"name": "IA"}, "science": {"name": "Science"}},
                             stats={"total": 10, "kept": 5, "seen_removed": 2})
        self.assertIn("# 📡 Nouvelles sources vigie24", md)
        self.assertIn("## IA", md)
        self.assertIn("## Science", md)
        self.assertIn("[Monde Numérique](https://monde.example/feed)", md)
        self.assertIn("**Plateforme :** Flux RSS", md)
        self.assertIn("**Langue :** Français", md)
        self.assertIn("**Description :**", md)
        self.assertIn("**Pourquoi :**", md)
        self.assertIn("2 déjà signalés retirés", md)

    def test_report_warning_when_few_sources(self):
        md = r.render_report([make_row()], {"ia": {"name": "IA"}}, warnings=["Seulement 1 source(s) valide(s) — rapport partiel (minimum souhaité : 10)."])
        self.assertIn("⚠️ **Avertissement :**", md)
        self.assertIn("rapport partiel", md)

    def test_description_truncated(self):
        long_desc = "x" * 500
        md = r.render_report([make_row(description=long_desc)], {"ia": {"name": "IA"}})
        self.assertIn("…", md)
        self.assertNotIn("x" * 300, md)


class TestBuildReport(unittest.TestCase):
    def _write_candidates(self, path, rows):
        with open(path, "w", encoding="utf-8") as fh:
            for row in rows:
                fh.write(json.dumps(row, ensure_ascii=False) + "\n")

    def _cli(self, candidates_path, output_path, state_path, **kwargs):
        args = [
            "--input", candidates_path,
            "--output", output_path,
            "--state", state_path,
            "--config", CONFIG,
        ]
        for k, v in kwargs.items():
            if v is True:
                args.append(f"--{k.replace('_', '-')}")
            else:
                args.extend([f"--{k.replace('_', '-')}", str(v)])
        return r.main(args)

    def test_end_to_end_report_and_state_update(self):
        with tempfile.TemporaryDirectory() as td:
            cand = os.path.join(td, "candidates.jsonl")
            out = os.path.join(td, "report.md")
            state = os.path.join(td, "state", "seen_sources.json")
            rows = [
                make_row(source_id=f"rss:fr{i}", title=f"Source FR {i}", category="ia", language="fr",
                         url=f"https://fr{i}.example/feed",
                         description="intelligence artificielle et machine learning en français") for i in range(8)
            ] + [
                make_row(source_id=f"rss:en{i}", title=f"Source EN {i}", category="science", language="en",
                         url=f"https://en{i}.example/feed",
                         description="science technologie actualites news weekly") for i in range(2)
            ]
            self._write_candidates(cand, rows)

            code = self._cli(cand, out, state)
            self.assertEqual(code, 0)
            with open(out, encoding="utf-8") as fh:
                md = fh.read()
            self.assertIn("# 📡 Nouvelles sources vigie24", md)
            self.assertEqual(md.count("### ["), 10)
            # State now contains the 10 reported sources
            keys, entries = r.load_seen(state)
            self.assertEqual(len(entries), 10)

            # Second run on identical input must report nothing new
            out2 = os.path.join(td, "report2.md")
            code2 = self._cli(cand, out2, state)
            self.assertEqual(code2, 1)  # no valid new source
            self.assertFalse(os.path.exists(out2))

    def test_seen_sources_never_reported(self):
        with tempfile.TemporaryDirectory() as td:
            cand = os.path.join(td, "candidates.jsonl")
            out = os.path.join(td, "report.md")
            state = os.path.join(td, "seen.json")
            row = make_row(title="Déjà Vu", description="intelligence artificielle")
            self._write_candidates(cand, [row])
            with open(state, "w", encoding="utf-8") as fh:
                json.dump({"seen": [{"key": "rss:https://example.com/feed"}]}, fh)
            code = self._cli(cand, out, state)
            self.assertEqual(code, 1)  # nothing new -> no report
            self.assertFalse(os.path.exists(out))

    def test_dry_run_does_not_update_state(self):
        with tempfile.TemporaryDirectory() as td:
            cand = os.path.join(td, "candidates.jsonl")
            out = os.path.join(td, "report.md")
            state = os.path.join(td, "seen.json")
            self._write_candidates(cand, [make_row(description="intelligence artificielle")])
            code = self._cli(cand, out, state, dry_run=True)
            self.assertEqual(code, 0)
            self.assertTrue(os.path.exists(out))
            self.assertFalse(os.path.exists(state))

    def test_bad_fr_ratio_rejected(self):
        code = r.main(["--input", "x", "--output", "y", "--state", "z", "--config", CONFIG, "--fr-ratio", "1.5"])
        self.assertEqual(code, 2)

    def test_missing_config_is_usage_error(self):
        code = r.main(["--input", "x", "--output", "y", "--state", "z", "--config", "/nonexistent.json"])
        self.assertEqual(code, 2)


if __name__ == "__main__":
    unittest.main()
