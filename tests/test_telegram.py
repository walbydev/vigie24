#!/usr/bin/env python3
"""Unit tests for src/telegram.py — conversion Markdown->HTML, découpage en
messages Telegram, envoi via API mockée (aucun réseau)."""
import io
import json
import os
import sys
import tempfile
import unittest
import urllib.error
from unittest import mock

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

from src import telegram as t  # noqa: E402


def fake_response(body: dict, status: int = 200):
    resp = mock.MagicMock()
    resp.__enter__ = mock.MagicMock(return_value=resp)
    resp.__exit__ = mock.MagicMock(return_value=False)
    resp.read.return_value = json.dumps(body).encode()
    resp.status = status
    return resp


class TestMarkdownToHtml(unittest.TestCase):
    def test_title(self):
        self.assertEqual(t.markdown_to_html("# 📡 Titre"), "<b>📡 Titre</b>")

    def test_category_header(self):
        self.assertEqual(t.markdown_to_html("## IA"), "<b>🗂 IA</b>")

    def test_source_link(self):
        md = "### [100% Vosges](https://centpourcent-vosges.fr/)"
        expected = '<a href="https://centpourcent-vosges.fr/"><b>100% Vosges</b></a>'
        self.assertEqual(t.markdown_to_html(md), expected)

    def test_bullet_with_bold_labels(self):
        md = "- **Plateforme :** Flux RSS · **Langue :** Français"
        expected = "• <b>Plateforme :</b> Flux RSS · <b>Langue :</b> Français"
        self.assertEqual(t.markdown_to_html(md), expected)

    def test_plain_bullet(self):
        md = "- Simple ligne"
        self.assertEqual(t.markdown_to_html(md), "• Simple ligne")

    def test_italic_footer(self):
        md = "*Pipeline vigie24 — 1307 candidats analysés.*"
        self.assertEqual(t.markdown_to_html(md), "<i>Pipeline vigie24 — 1307 candidats analysés.</i>")

    def test_horizontal_rule(self):
        self.assertEqual(t.markdown_to_html("---"), "┄" * 24)

    def test_html_entities_escaped(self):
        md = "A & B < C > D"
        self.assertEqual(t.markdown_to_html(md), "A &amp; B &lt; C &gt; D")

    def test_url_with_query_string_ampersand(self):
        md = "[X](https://example.com/?a=1&b=2)"
        expected = '<a href="https://example.com/?a=1&amp;b=2">X</a>'
        self.assertEqual(t.markdown_to_html(md), expected)

    def test_no_markdown_plain_line(self):
        self.assertEqual(t.markdown_to_html("Description simple."), "Description simple.")


class TestChunkHtml(unittest.TestCase):
    def test_single_chunk_when_small(self):
        chunks = t.chunk_html("<b>a</b>\n\n<b>b</b>")
        self.assertEqual(len(chunks), 1)

    def test_multiple_chunks_respect_limit(self):
        block = "<b>ligne</b>\n" * 400  # ~5200 chars
        chunks = t.chunk_html(block, limit=1200)
        self.assertGreater(len(chunks), 1)
        for c in chunks:
            self.assertLessEqual(len(c), 1200)

    def test_blocks_kept_whole_when_possible(self):
        html_text = "<b>A</b>\n\n<b>B</b>\n\n<b>C</b>"
        chunks = t.chunk_html(html_text, limit=1000)
        self.assertEqual(len(chunks), 1)
        self.assertIn("<b>B</b>", chunks[0])

    def test_blocks_split_across_chunks(self):
        html_text = "\n\n".join(f"<b>bloc-{i}</b> " + "x" * 60 for i in range(50))
        chunks = t.chunk_html(html_text, limit=400)
        self.assertGreater(len(chunks), 1)
        # chaque bloc (80 car) tient dans la limite : aucun découpage interne
        for c in chunks:
            self.assertLessEqual(len(c), 400)

    def test_oversized_single_block_split_on_newline(self):
        block = ("ligne " * 300) + "\n" + ("queue " * 50)
        chunks = t.chunk_html(block, limit=300)
        self.assertGreater(len(chunks), 1)
        self.assertTrue(all(len(c) <= 300 for c in chunks))


class TestSendError(unittest.TestCase):
    def test_truncation(self):
        with mock.patch.object(t, "send_message", return_value={"ok": True}) as sm:
            t.send_error("x" * 5000)
        sent = sm.call_args.args[0]
        self.assertTrue(sent.startswith("⚠️ Vigie24 — "))
        self.assertLessEqual(len(sent), t.MAX_MESSAGE_CHARS)
        self.assertTrue(sent.endswith("…"))

    def test_plain_text_no_parse_mode(self):
        with mock.patch.object(t, "send_message", return_value={"ok": True}) as sm:
            t.send_error("boom")
        self.assertEqual(sm.call_args.kwargs["parse_mode"], None)


class TestConfig(unittest.TestCase):
    def test_missing_env_raises(self):
        with mock.patch.dict(os.environ, {}, clear=True):
            with self.assertRaises(t.TelegramError) as ctx:
                t._require_config()
        self.assertIn("VIGIE24_TELEGRAM_BOT_TOKEN", str(ctx.exception))
        self.assertIn("VIGIE24_TELEGRAM_CHAT_ID", str(ctx.exception))

    def test_api_requires_config(self):
        with mock.patch.dict(os.environ, {}, clear=True):
            with self.assertRaises(t.TelegramError):
                t._api("sendMessage", {"chat_id": "1", "text": "x"})


class TestApi(unittest.TestCase):
    def setUp(self):
        self.env = {
            "VIGIE24_TELEGRAM_BOT_TOKEN": "tok",
            "VIGIE24_TELEGRAM_CHAT_ID": "42",
        }

    def test_api_ok(self):
        with mock.patch.dict(os.environ, self.env, clear=True):
            with mock.patch("urllib.request.urlopen", return_value=fake_response({"ok": True, "result": {"message_id": 7}})) as uo:
                body = t._api("sendMessage", {"chat_id": "42", "text": "hi"})
        self.assertTrue(body["ok"])
        url = uo.call_args.args[0].full_url
        self.assertEqual(url, "https://api.telegram.org/bottok/sendMessage")

    def test_api_not_ok_raises(self):
        with mock.patch.dict(os.environ, self.env, clear=True):
            with mock.patch("urllib.request.urlopen", return_value=fake_response({"ok": False, "description": "chat not found"})):
                with self.assertRaises(t.TelegramError) as ctx:
                    t._api("sendMessage", {"chat_id": "0", "text": "x"})
        self.assertIn("chat not found", str(ctx.exception))

    def test_api_http_error_raises(self):
        http_err = urllib.error.HTTPError(
            "https://api.telegram.org/bottok/sendMessage", 400, "Bad Request", {},
            io.BytesIO(b'{"ok":false,"description":"bad"}'),
        )
        with mock.patch.dict(os.environ, self.env, clear=True):
            with mock.patch("urllib.request.urlopen", side_effect=http_err):
                with self.assertRaises(t.TelegramError) as ctx:
                    t._api("sendMessage", {"chat_id": "0", "text": "x"})
        self.assertIn("HTTP 400", str(ctx.exception))


class TestSendReport(unittest.TestCase):
    def test_send_report_single_message(self):
        md = "# 📡 Rapport\n\n## IA\n\n### [Src](https://x.fr/)\n- **Langue :** Français\n"
        with tempfile.NamedTemporaryFile("w", suffix=".md", delete=False, encoding="utf-8") as fh:
            fh.write(md)
            path = fh.name
        try:
            with mock.patch.dict(os.environ, {"VIGIE24_TELEGRAM_BOT_TOKEN": "tok", "VIGIE24_TELEGRAM_CHAT_ID": "42"}, clear=True):
                with mock.patch.object(t, "_api", return_value={"ok": True, "result": {"message_id": 1, "chat": {"id": 42}}}) as api:
                    results = t.send_report(path, chat_id="42")
        finally:
            os.unlink(path)
        self.assertEqual(len(results), 1)
        payload = api.call_args.args[1]
        self.assertEqual(payload["chat_id"], "42")
        self.assertEqual(payload["parse_mode"], "HTML")
        self.assertIn("<b>", payload["text"])

    def test_send_report_missing_file(self):
        with self.assertRaises(t.TelegramError):
            t.send_report("/nonexistent/report.md")


class TestMain(unittest.TestCase):
    def test_main_send_error_cli(self):
        with mock.patch.object(t, "send_error", return_value={"ok": True, "result": {"message_id": 1}}) as se:
            code = t.main(["--send-error", "boom"])
        self.assertEqual(code, 0)
        self.assertEqual(se.call_args.args[0], "boom")

    def test_main_requires_action(self):
        with self.assertRaises(SystemExit) as ctx:
            t.main([])
        self.assertEqual(ctx.exception.code, 2)


if __name__ == "__main__":
    unittest.main()
