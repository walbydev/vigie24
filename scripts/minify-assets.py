#!/usr/bin/env python3
"""Minify Vigie24 static assets. Source readable files -> *.min.* committed.

Run:  make assets
      # or: uv run --with rcssmin --with jsmin python3 scripts/minify-assets.py

Rule of thumb: after ANY edit to a source CSS/JS file, run `make assets`
before committing — the site serves the .min.* outputs (cache-busted by
asp-append-version), so a stale .min.* silently ships old styles.
"""
import pathlib

from rcssmin import cssmin
from jsmin import jsmin

ROOT = pathlib.Path(__file__).resolve().parent.parent / "src" / "Vigie24.Web" / "wwwroot"
PAIRS = [
    ("css/site.css", "css/site.min.css"),
    ("css/radar.css", "css/radar.min.css"),
    ("js/vigie24.js", "js/vigie24.min.js"),
]

for src, dst in PAIRS:
    raw = (ROOT / src).read_text(encoding="utf-8")
    out = cssmin(raw) if src.endswith(".css") else jsmin(raw)
    (ROOT / dst).write_text(out, encoding="utf-8")
    print(f"{dst}: {len(raw):>7} -> {len(out):>7} octets")
