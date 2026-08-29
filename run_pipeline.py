#!/usr/bin/env python3
"""Orchestrateur du pipeline vigie24 : discovery -> report -> Telegram.

Exécute ``src.discovery`` (collecte des candidats RSS/YouTube/Reddit/Bluesky),
puis ``src.report`` (filtrage pertinence + dédup + état vu + rapport 10-20
sources majoritairement francophone), puis envoie le rapport sur le Telegram
de Philippe via ``src.telegram``. En cas d'échec d'une étape, une alerte est
envoyée sur Telegram (``send_error``) et le script se termine avec un code
non nul — le planificateur n'est jamais bloqué.

Usage ::

    python3 run_pipeline.py                    # pipeline complet + envoi
    python3 run_pipeline.py --dry-run          # pipeline sans envoi Telegram,
                                               # état seen non modifié
    python3 run_pipeline.py --send-report report.md   # envoi manuel (test)
    python3 run_pipeline.py --send-error "message"    # alerte manuelle (test)

Exit codes : 0 = OK, 1 = échec discovery/report, 2 = usage/config,
3 = échec de l'envoi Telegram. Journal : logs/pipeline.log (gitignoré).
"""
from __future__ import annotations

import argparse
import logging
import os
import pathlib
import subprocess
import sys
from datetime import datetime, timezone
from typing import List, Optional

REPO_ROOT = pathlib.Path(__file__).resolve().parent
LOG_DIR = REPO_ROOT / "logs"
LOG_FILE = LOG_DIR / "pipeline.log"

DISCOVERY_CMD = [
    sys.executable, "-m", "src.discovery",
    "--config", "config/categories.json",
    "--output", "candidates.jsonl",
]
REPORT_BASE_CMD = [
    sys.executable, "-m", "src.report",
    "--input", "candidates.jsonl",
    "--state", "state/seen_sources.json",
]

log = logging.getLogger("pipeline")


def timestamped_report_path(now: Optional[datetime] = None) -> str:
    """Nom de rapport horodaté, ex. report_2026-08-29_1601.md (gitignoré)."""
    now = now or datetime.now(timezone.utc)
    return now.strftime("report_%Y-%m-%d_%H%M.md")


def run_step(cmd: List[str], cwd: pathlib.Path = REPO_ROOT) -> int:
    """Exécute une étape du pipeline, stdout/stderr vers le journal ; renvoie
    le code de sortie (ne lève pas)."""
    log.info("Étape : %s", " ".join(cmd))
    proc = subprocess.run(cmd, cwd=str(cwd))
    log.info("Étape terminée : exit=%d", proc.returncode)
    return proc.returncode


def notify_error(message: str) -> None:
    """Envoie une alerte Telegram ; l'échec de la notification est journalisé
    localement mais ne masque jamais l'erreur d'origine."""
    try:
        from src import telegram as tg
        tg.send_error(message)
        log.info("Alerte Telegram envoyée")
    except Exception as exc:  # noqa: BLE001 — la notification ne doit pas casser le pipeline
        log.error("Échec de la notification Telegram : %s", exc)


def _yt_tokens_arg(arg: Optional[str]) -> List[str]:
    """--yt-tokens si fourni ou présent dans l'environnement (YT_TOKENS_PATH)
    et existant sur disque, sinon aucun (discovery reste best-effort)."""
    path = arg or os.environ.get("YT_TOKENS_PATH", "")
    if path and pathlib.Path(path).is_file():
        return ["--yt-tokens", path]
    if arg:
        log.warning("Fichier de tokens YouTube introuvable : %s", path)
    return []


def _send_report(report_path: str, chat_id: Optional[str] = None) -> None:
    from src import telegram as tg
    results = tg.send_report(report_path, chat_id=chat_id)
    ids = [r.get("result", {}).get("message_id") for r in results]
    log.info("Rapport envoyé sur Telegram : %d message(s) %s", len(results), ids)


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        prog="run_pipeline.py",
        description="Pipeline vigie24 : discovery + report + envoi Telegram (toutes les 72 h).",
    )
    parser.add_argument("--dry-run", action="store_true",
                        help="exécute discovery+report sans envoyer sur Telegram "
                             "(état seen non modifié)")
    parser.add_argument("--send-report", metavar="PATH",
                        help="envoie un rapport Markdown existant puis s'arrête (test manuel)")
    parser.add_argument("--send-error", metavar="MESSAGE",
                        help="envoie une alerte Telegram puis s'arrête (test manuel)")
    parser.add_argument("--yt-tokens", default=None,
                        help="fichier JSON de tokens YouTube (défaut: env YT_TOKENS_PATH)")
    parser.add_argument("--chat-id", default=None, help="override du chat_id Telegram")
    parser.add_argument("--verbose", action="store_true", help="debug logging")
    args = parser.parse_args(argv)

    LOG_DIR.mkdir(parents=True, exist_ok=True)
    handlers: list[logging.Handler] = [logging.FileHandler(LOG_FILE, encoding="utf-8")]
    if sys.stderr.isatty():
        handlers.append(logging.StreamHandler())  # console en interactif seulement
    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s %(levelname)s %(name)s %(message)s",
        handlers=handlers,
    )
    log = logging.getLogger("pipeline")

    if args.send_report or args.send_error:
        try:
            if args.send_report:
                _send_report(args.send_report, chat_id=args.chat_id)
            else:
                from src import telegram as tg
                tg.send_error(args.send_error, chat_id=args.chat_id)
        except Exception as exc:  # noqa: BLE001
            log.error("Envoi manuel échoué : %s", exc)
            return 3
        log.info("Envoi manuel réussi")
        return 0

    # -- pipeline complet --------------------------------------------------
    rc = run_step(DISCOVERY_CMD + _yt_tokens_arg(args.yt_tokens))
    if rc != 0:
        notify_error(f"Étape discovery échouée (exit {rc}) — pas de rapport aujourd'hui.")
        return 1

    report_path = timestamped_report_path()
    report_cmd = REPORT_BASE_CMD + ["--output", report_path]
    if args.dry_run:
        report_cmd.append("--dry-run")
    rc = run_step(report_cmd)
    if rc != 0:
        notify_error(f"Étape report échouée (exit {rc}) — pas de rapport aujourd'hui.")
        return 1

    if args.dry_run:
        log.info("DRY-RUN : rapport généré sans envoi Telegram -> %s", report_path)
        log.info("Pour tester l'envoi : python3 run_pipeline.py --send-report %s", report_path)
        print(f"DRY-RUN OK — rapport : {report_path} (non envoyé, état non modifié)")
        return 0

    try:
        _send_report(report_path, chat_id=args.chat_id)
    except Exception as exc:  # noqa: BLE001
        notify_error(f"Échec de l'envoi du rapport sur Telegram : {exc}")
        return 3

    log.info("Pipeline terminé avec succès : %s", report_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
