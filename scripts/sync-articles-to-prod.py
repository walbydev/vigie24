#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Synchronise les articles du staging vers la production (Vigie24).

Idempotent et SANS DOUBLON : la déduplication repose sur la contrainte unique
CanonicalUrl (IX_articles_CanonicalUrl). Les articles déjà présents en
production sont ignorés ; les catégories sont mappées par slug, les tags et
les AUTEURS manquants sont créés, les liens article_tags / article_sources /
article_authors reproduits (y compris pour les articles déjà présents dont
les liens manqueraient — répare les imports incomplets).

Usage :
    python3 scripts/sync-articles-to-prod.py            # synchronisation réelle
    python3 scripts/sync-articles-to-prod.py --dry-run  # aperçu sans rien écrire

Prérequis : alias SSH 'vigie24-staging' et 'vigie24-prod' configurés
(~/.ssh/config). Aucun secret en clair : tout passe par SSH (clés).
"""

import csv
import io
import subprocess
import sys

SSH_STAGING = (
    "ssh -o ConnectTimeout=10 vigie24-staging "
    '"cd ~/vigie24 && docker compose --env-file .env.staging -f docker-compose.staging.yml '
    'exec -T postgres psql -U postgres_staging -d vigie24_staging -tA -v ON_ERROR_STOP=1"'
)
SSH_PROD = (
    "ssh -o ConnectTimeout=10 vigie24-prod "
    '"cd ~/vigie24 && docker compose --env-file .env.production -f docker-compose.prod.yml '
    'exec -T postgres psql -U postgres_prod -d vigie24_prod -tA -v ON_ERROR_STOP=1"'
)

ARTICLE_COLS = [
    "Id", "CategoryId", "CanonicalUrl", "Title", "Summary", "ImageUrl", "ImageAltText",
    "LanguageCode", "PublishedAt", "FirstSeenAt", "EditorialScore", "AlgorithmicScore",
    "RankingScore", "VoteCount", "VoteAverage", "CrossSourceCount", "IsPublished",
    "CreatedAt", "UpdatedAt", "ClickCount",
]
SOURCE_LINK_COLS = [
    "ArticleId", "SourceId", "SourceArticleUrl", "SourceTitle", "SourcePublishedAt",
    "SourceAuthorText", "IsPrimary", "CreatedAt",
]
AUTHOR_COLS = ["Id", "Name", "ProfileUrl", "CreatedAt", "UpdatedAt"]
ARTICLE_AUTHOR_COLS = ["ArticleId", "AuthorId", "AuthorOrder", "CreatedAt"]


def psql(host_cmd, sql, timeout=180):
    """Exécute du SQL sur l'hôte via SSH + docker compose exec psql (stdin)."""
    p = subprocess.run(host_cmd, shell=True, input=sql, capture_output=True,
                       text=True, timeout=timeout)
    if p.returncode != 0:
        raise RuntimeError(f"psql a échoué (exit {p.returncode}) :\n{p.stderr[-800:]}")
    return p.stdout


def export(host_cmd, select_sql):
    """Exporte un SELECT en lignes (vrai format CSV, NULL représenté par __NULL__)."""
    copy_sql = "COPY ({}) TO STDOUT WITH (FORMAT csv, NULL '__NULL__');".format(select_sql)
    out = psql(host_cmd, copy_sql)
    return [r for r in csv.reader(io.StringIO(out)) if r]


def q(v):
    """Quote une valeur SQL : NULL si marqueur __NULL__, sinon littéral string échappé."""
    if v is None or v == "__NULL__":
        return "NULL"
    return "'" + v.replace("'", "''") + "'"


def b(v):
    """Convertit t/f en true/false SQL."""
    return "true" if v == "t" else "false"


def main():
    dry = "--dry-run" in sys.argv
    if dry:
        print("== MODE DRY-RUN : aucune écriture en production ==\n")

    # ---- 1. Export staging ----
    print("1) Lecture du staging…")
    cat_rows = export(SSH_STAGING,
                      'SELECT "Id","Slug","ParentCategoryId","DisplayOrder","IsActive" FROM categories')
    tag_rows = export(SSH_STAGING,
                      'SELECT "Id","Slug","CreatedAt","UpdatedAt" FROM tags')
    art_rows = export(SSH_STAGING,
                      "SELECT {} FROM articles ORDER BY \"Id\"".format(
                          ",".join('"{}"'.format(c) for c in ARTICLE_COLS)))
    at_rows = export(SSH_STAGING,
                     'SELECT at."ArticleId", t."Slug" FROM article_tags at '
                     'JOIN tags t ON t."Id" = at."TagId"')
    src_rows = export(SSH_STAGING,
                      "SELECT {} FROM article_sources".format(
                          ",".join('"{}"'.format(c) for c in SOURCE_LINK_COLS)))
    staging_src_feed = {r[0]: r[1] for r in export(SSH_STAGING,
                                                   'SELECT "Id","FeedUrl" FROM sources')}
    auth_rows = export(SSH_STAGING,
                       "SELECT {} FROM authors".format(
                           ",".join('"{}"'.format(c) for c in AUTHOR_COLS)))
    aa_rows = export(SSH_STAGING,
                     "SELECT {} FROM article_authors".format(
                         ",".join('"{}"'.format(c) for c in ARTICLE_AUTHOR_COLS)))

    # ---- 2. État prod ----
    print("2) Lecture de la production…")
    prod_cats = {r[1]: r[0] for r in export(SSH_PROD, 'SELECT "Id","Slug" FROM categories')}
    prod_tags = {r[1]: r[0] for r in export(SSH_PROD, 'SELECT "Id","Slug" FROM tags')}
    prod_urls = {r[0] for r in export(SSH_PROD, 'SELECT "CanonicalUrl" FROM articles')}
    prod_article_ids = {r[0] for r in export(SSH_PROD, 'SELECT "Id" FROM articles')}
    # Sources: mapping by FeedUrl (natural identifier) — Ids may differ
    # between staging and prod if the prod worker created sources itself.
    prod_sources_feed = {r[1]: r[0] for r in export(SSH_PROD, 'SELECT "Id","FeedUrl" FROM sources')}
    # article→source links already present in prod (dedupe by article + FeedUrl)
    prod_link_feeds = {(r[0], r[1]) for r in export(
        SSH_PROD, 'SELECT a_s."ArticleId", s."FeedUrl" FROM article_sources a_s '
                  'JOIN sources s ON s."Id" = a_s."SourceId"')}
    prod_author_names = {r[1]: r[0] for r in export(SSH_PROD, 'SELECT "Id","Name" FROM authors')}

    # ---- 3. Category mapping (by slug) ----
    cat_map = {}            # id cat staging -> id cat prod
    cats_to_insert = []     # (id, slug, parent_mapped, display_order, is_active)
    for cid, slug, parent, disp, active in cat_rows:
        if slug in prod_cats:
            cat_map[cid] = prod_cats[slug]
        else:
            cat_map[cid] = cid
            parent_mapped = cat_map.get(parent) if parent and parent != "__NULL__" else None
            cats_to_insert.append((cid, slug, parent_mapped, disp, active))

    # ---- 4. Mapping tags (par slug) ----
    tag_map = {}            # id tag staging -> id tag prod
    tags_to_insert = []     # (id, slug, created, updated)
    for tid, slug, created, updated in tag_rows:
        if slug in prod_tags:
            tag_map[tid] = prod_tags[slug]
        else:
            tag_map[tid] = tid
            tags_to_insert.append((tid, slug, created, updated))

    # ---- 5. Articles absents de la prod ----
    existing = 0
    to_insert = []
    for r in art_rows:
        row = dict(zip(ARTICLE_COLS, r))
        if row["CanonicalUrl"] in prod_urls:
            existing += 1
        else:
            to_insert.append(row)

    # Apply the category mapping (staging Id -> production Id)
    for row in to_insert:
        cid = row["CategoryId"]
        if cid != "__NULL__":
            row["CategoryId"] = cat_map.get(cid, cid)

    # ---- 6. article_tags links of imported articles ----
    art_ids_new = {r["Id"] for r in to_insert}
    slug_to_tagid = {slug: tid for tid, slug, _, _ in tag_rows}
    tagid_prod_by_slug = {slug: tag_map[slug_to_tagid[slug]] for slug in slug_to_tagid}
    links = [(aid, tagid_prod_by_slug[slug]) for aid, slug in at_rows if aid in art_ids_new]

    # ---- 7. article → source links (for ALL articles present in prod) ----
    # Mapping du SourceId staging → prod via le FeedUrl de la source.
    src_links = []
    for r in src_rows:
        row = dict(zip(SOURCE_LINK_COLS, r))
        if row["ArticleId"] not in prod_article_ids:
            continue
        feed = staging_src_feed.get(row["SourceId"])
        if feed is None:
            continue
        prod_sid = prod_sources_feed.get(feed)
        if prod_sid is None:
            continue
        # Already linked in prod (same article + same source)? → skip, whatever the Id.
        if (row["ArticleId"], feed) in prod_link_feeds:
            continue
        row["SourceId"] = prod_sid
        src_links.append(row)

    # ---- 8. Auteurs (mapping par nom) + liens article → auteur ----
    author_map = {}         # id auteur staging -> id auteur prod
    authors_to_insert = []  # (id, name, profile_url, created, updated)
    for r in auth_rows:
        row = dict(zip(AUTHOR_COLS, r))
        if row["Name"] in prod_author_names:
            author_map[row["Id"]] = prod_author_names[row["Name"]]
        else:
            author_map[row["Id"]] = row["Id"]
            authors_to_insert.append(row)

    aa_links = [
        (r[0], author_map.get(r[1], r[1]), r[2], r[3]) for r in aa_rows
        if r[0] in prod_article_ids
    ]

    # ---- 9. Rapport ----
    print("\nRésumé :")
    print(f"  articles staging                : {len(art_rows)}")
    print(f"  déjà présents en production     : {existing}")
    print(f"  à importer                      : {len(to_insert)}")
    print(f"  catégories à créer              : {len(cats_to_insert)}")
    print(f"  tags à créer                    : {len(tags_to_insert)}")
    print(f"  liens article→tag à créer       : {len(links)}")
    print(f"  liens article→source à créer    : {len(src_links)}")
    print(f"  auteurs à créer                 : {len(authors_to_insert)}")
    print(f"  liens article→auteur à créer    : {len(aa_links)}")

    if dry or not (to_insert or cats_to_insert or tags_to_insert
                   or links or src_links or authors_to_insert or aa_links):
        if not dry:
            print("\nRien à importer : tout est déjà synchronisé.")
        return

    # ---- 10. Generate the import SQL (transactional) ----
    sql = ["BEGIN;"]

    if cats_to_insert:
        vals = ",\n".join(
            "({},{},{},{},{})".format(q(cid), q(slug),
                                      "NULL" if parent is None else q(parent),
                                      q(disp), b(active))
            for cid, slug, parent, disp, active in cats_to_insert)
        sql.append('INSERT INTO categories ("Id","Slug","ParentCategoryId","DisplayOrder","IsActive") VALUES\n'
                   + vals + "\nON CONFLICT DO NOTHING;")

    if tags_to_insert:
        vals = ",\n".join(
            "({},{},{},{})".format(q(tid), q(slug), q(created), q(updated))
            for tid, slug, created, updated in tags_to_insert)
        sql.append('INSERT INTO tags ("Id","Slug","CreatedAt","UpdatedAt") VALUES\n'
                   + vals + "\nON CONFLICT DO NOTHING;")

    if authors_to_insert:
        vals = ",\n".join(
            "({},{},{},{},{})".format(q(a["Id"]), q(a["Name"]), q(a["ProfileUrl"]),
                                      q(a["CreatedAt"]), q(a["UpdatedAt"]))
            for a in authors_to_insert)
        sql.append('INSERT INTO authors ("Id","Name","ProfileUrl","CreatedAt","UpdatedAt") VALUES\n'
                   + vals + "\nON CONFLICT DO NOTHING;")

    if to_insert:
        cols = ",".join('"{}"'.format(c) for c in ARTICLE_COLS)
        vals = ",\n".join(
            "({})".format(
                ",".join(
                    q(row[c]) if c != "IsPublished" else b(row[c])
                    for c in ARTICLE_COLS))
            for row in to_insert)
        sql.append('INSERT INTO articles ({}) VALUES\n'.format(cols)
                   + vals + "\nON CONFLICT (\"CanonicalUrl\") DO NOTHING;")

    if src_links:
        vals = ",\n".join(
            "({},{},{},{},{},{},{},{})".format(
                q(l["ArticleId"]), q(l["SourceId"]), q(l["SourceArticleUrl"]),
                q(l["SourceTitle"]), q(l["SourcePublishedAt"]), q(l["SourceAuthorText"]),
                b(l["IsPrimary"]), q(l["CreatedAt"]))
            for l in src_links)
        sql.append('INSERT INTO article_sources ("ArticleId","SourceId","SourceArticleUrl",'
                   '"SourceTitle","SourcePublishedAt","SourceAuthorText","IsPrimary","CreatedAt") VALUES\n'
                   + vals + "\nON CONFLICT DO NOTHING;")

    if links:
        vals = ",\n".join(
            "({},{},now())".format(q(aid), q(tid)) for aid, tid in links)
        sql.append('INSERT INTO article_tags ("ArticleId","TagId","CreatedAt") VALUES\n'
                   + vals + "\nON CONFLICT DO NOTHING;")

    if aa_links:
        vals = ",\n".join(
            "({},{},{},{})".format(q(aid), q(auth_id), q(order), q(created))
            for aid, auth_id, order, created in aa_links)
        sql.append('INSERT INTO article_authors ("ArticleId","AuthorId","AuthorOrder","CreatedAt") VALUES\n'
                   + vals + "\nON CONFLICT DO NOTHING;")

    sql.append("COMMIT;")

    # ---- 11. Execution ----
    print("\n3) Import en production…")
    psql(SSH_PROD, "\n".join(sql))

    # Final control counts
    n = psql(SSH_PROD, 'SELECT count(*) FROM articles;').strip()
    dup = psql(SSH_PROD,
               'SELECT count(*) FROM (SELECT "CanonicalUrl" FROM articles '
               'GROUP BY "CanonicalUrl" HAVING count(*) > 1) d;').strip()
    n_src = psql(SSH_PROD, 'SELECT count(*) FROM article_sources;').strip()
    n_auth = psql(SSH_PROD, 'SELECT count(*) FROM article_authors;').strip()
    print("\n✅ Synchronisation terminée.")
    print(f"   articles en production après sync       : {n}")
    print(f"   doublons CanonicalUrl (doit être 0)      : {dup}")
    print(f"   liens article→source en production       : {n_src}")
    print(f"   liens article→auteur en production       : {n_auth}")


if __name__ == "__main__":
    main()
