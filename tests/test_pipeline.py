#!/usr/bin/env python3
"""Unit tests for run_pipeline.py — orchestrateur discovery -> report ->
Telegram (étapes et envoi mockés, aucun réseau)."""
import os
import pathlib
import re
import sys
import tempfile
import unittest
from unittest import mock

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

import run_pipeline as rp  # noqa: E402

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent


class TestHelpers(unittest.TestCase):
    def test_timestamped_report_path(self):
        from datetime import datetime, timezone
        path = rp.timestamped_report_path(datetime(2026, 8, 29, 16, 5, tzinfo=timezone.utc))
        self.assertRegex(path, r"^report_2026-08-29_1605\.md$")

    def test_yt_tokens_arg_existing(self):
        with tempfile.NamedTemporaryFile(delete=False) as fh:
            name = fh.name
        try:
            self.assertEqual(rp._yt_tokens_arg(name), ["--yt-tokens", name])
        finally:
            os.unlink(name)

    def test_yt_tokens_arg_missing(self):
        self.assertEqual(rp._yt_tokens_arg("/nonexistent/tokens.json"), [])

    def test_yt_tokens_arg_env_fallback(self):
        with tempfile.NamedTemporaryFile(delete=False) as fh:
            name = fh.name
        try:
            with mock.patch.dict(os.environ, {"YT_TOKENS_PATH": name}, clear=False):
                self.assertEqual(rp._yt_tokens_arg(None), ["--yt-tokens", name])
        finally:
            os.unlink(name)


class TestMain(unittest.TestCase):
    def test_dry_run_does_not_send(self):
        with mock.patch.object(rp, "run_step", return_value=0) as rs, \
             mock.patch.object(rp, "_send_report") as send, \
             mock.patch.object(rp, "notify_error") as ne, \
             mock.patch.object(rp.logging, "basicConfig"):
            code = rp.main(["--dry-run"])
        self.assertEqual(code, 0)
        self.assertEqual(rs.call_count, 2)  # discovery + report
        send.assert_not_called()
        ne.assert_not_called()

    def test_discovery_failure_notifies_and_returns_1(self):
        with mock.patch.object(rp, "run_step", return_value=1) as rs, \
             mock.patch.object(rp, "_send_report") as send, \
             mock.patch.object(rp, "notify_error") as ne, \
             mock.patch.object(rp.logging, "basicConfig"):
            code = rp.main([])
        self.assertEqual(code, 1)
        self.assertEqual(rs.call_count, 1)
        send.assert_not_called()
        ne.assert_called_once()
        self.assertIn("discovery", ne.call_args.args[0])

    def test_report_failure_notifies_and_returns_1(self):
        side = [0, 1]
        with mock.patch.object(rp, "run_step", side_effect=side), \
             mock.patch.object(rp, "notify_error") as ne, \
             mock.patch.object(rp.logging, "basicConfig"):
            code = rp.main([])
        self.assertEqual(code, 1)
        ne.assert_called_once()
        self.assertIn("report", ne.call_args.args[0])

    def test_full_run_sends_report(self):
        with mock.patch.object(rp, "run_step", return_value=0), \
             mock.patch.object(rp, "_send_report") as send, \
             mock.patch.object(rp, "notify_error") as ne, \
             mock.patch.object(rp.logging, "basicConfig"):
            code = rp.main([])
        self.assertEqual(code, 0)
        send.assert_called_once()
        self.assertRegex(send.call_args.args[0], r"^report_\d{4}-\d{2}-\d{2}_\d{4}\.md$")
        ne.assert_not_called()

    def test_full_run_send_failure_returns_3(self):
        with mock.patch.object(rp, "run_step", return_value=0), \
             mock.patch.object(rp, "_send_report", side_effect=RuntimeError("API down")), \
             mock.patch.object(rp, "notify_error") as ne, \
             mock.patch.object(rp.logging, "basicConfig"):
            code = rp.main([])
        self.assertEqual(code, 3)
        ne.assert_called_once()
        self.assertIn("API down", ne.call_args.args[0])

    def test_send_report_manual_mode(self):
        with mock.patch.object(rp, "_send_report") as send, \
             mock.patch.object(rp.logging, "basicConfig"):
            code = rp.main(["--send-report", "report.md"])
        self.assertEqual(code, 0)
        send.assert_called_once_with("report.md", chat_id=None)

    def test_send_error_manual_mode(self):
        with mock.patch("src.telegram.send_error", return_value={"ok": True}) as se, \
             mock.patch.object(rp.logging, "basicConfig"):
            code = rp.main(["--send-error", "test"])
        self.assertEqual(code, 0)
        self.assertEqual(se.call_args.args[0], "test")


if __name__ == "__main__":
    unittest.main()
