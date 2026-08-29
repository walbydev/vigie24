#!/usr/bin/env python3
"""
Vigie24 — Normalise les URLs d'articles contenant des caractères non-ASCII.

Contexte : les flux RSS Reddit fournissent des slugs avec accents bruts
(…/progrès_à_la_salle/…). Kestrel rejette ces caractères dans l'en-tête
Location → le redirect /track renvoyait HTTP 500 (liens cassés). Le worker
stocke désormais des URLs encodées (Uri.AbsoluteUri) ; ce script corrige
les lignes existantes (articles.CanonicalUrl + article_sources.SourceArticleUrl).

Usage:  python3 scripts/normalize_urls.py [--host staging|prod]
Seulement les lignes avec caractères non-ASCII sont mises à jour (idempotent).
"""
import argparse
import subprocess
import urllib.parse

HOSTS = {
    "prod": ("vigie24-prod", "vigie24-db-prod", "postgres_prod", "vigie24_prod"),
    "staging": ("vigie24-staging", "vigie24-db-staging", "postgres_staging", "vigie24_staging"),
}


def norm(url: str) -> str:
    """Percent-encode ONLY non-ASCII chars (matches C# Uri.AbsoluteUri for these URLs)."""
    return "".join(c if ord(c) < 128 else urllib.parse.quote(c) for c in url)


def db_query(ssh_host: str, db_container: str, db_user: str, db_name: str, sql: str) -> str:
    cmd = ["ssh", ssh_host,
           f"docker exec -i {db_container} psql -U {db_user} -d {db_name} -A -F '|' -t"]
    out = subprocess.run(cmd, input=sql, capture_output=True, text=True, timeout=300)
    if out.returncode != 0:
        raise SystemExit(f"psql failed: {out.stderr.strip()[:300]}")
    return out.stdout


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", choices=HOSTS, default="prod")
    args = ap.parse_args()
    ssh_host, db_container, db_user, db_name = HOSTS[args.host]

    # 1) Articles : CanonicalUrl non-ASCII
    rows = db_query(ssh_host, db_container, db_user, db_name,
                    'SELECT "Id", "CanonicalUrl" FROM articles '
                    'WHERE octet_length("CanonicalUrl") <> length("CanonicalUrl");').splitlines()
    article_updates = []
    for line in rows:
        parts = line.split("|")
        if len(parts) != 2:
            continue
        aid, url = parts[0].strip(), parts[1]
        fixed = norm(url)
        if fixed != url:
            article_updates.append((aid, fixed))

    # 2) article_sources : SourceArticleUrl non-ASCII
    rows = db_query(ssh_host, db_container, db_user, db_name,
                    'SELECT "ArticleId", "SourceArticleUrl" FROM article_sources '
                    'WHERE octet_length("SourceArticleUrl") <> length("SourceArticleUrl");').splitlines()
    source_updates = []
    for line in rows:
        parts = line.split("|")
        if len(parts) != 2:
            continue
        aid, url = parts[0].strip(), parts[1]
        fixed = norm(url)
        if fixed != url:
            source_updates.append((aid, fixed))

    print(f"[{args.host}] {len(article_updates)} article(s) + {len(source_updates)} source(s) à normaliser")

    if article_updates:
        sql = "UPDATE articles AS a SET \"CanonicalUrl\" = v.url FROM (VALUES " + \
            ",".join(f"('{a}'::uuid, '{u}'::text)" for a, u in article_updates) + \
            ") AS v(id, url) WHERE a.\"Id\" = v.id;"
        db_query(ssh_host, db_container, db_user, db_name, sql)
        print(f"  ✅ articles.CanonicalUrl mis à jour ({len(article_updates)})")

    if source_updates:
        sql = "UPDATE article_sources AS s SET \"SourceArticleUrl\" = v.url FROM (VALUES " + \
            ",".join(f"('{a}'::uuid, '{u}'::text)" for a, u in source_updates) + \
            ") AS v(id, url) WHERE s.\"ArticleId\" = v.id;"
        db_query(ssh_host, db_container, db_user, db_name, sql)
        print(f"  ✅ article_sources.SourceArticleUrl mis à jour ({len(source_updates)})")

    if not article_updates and not source_updates:
        print("  Rien à faire — aucune URL non-ASCII.")


if __name__ == "__main__":
    main()
