#!/usr/bin/env python3
"""Planificateur APScheduler du pipeline vigie24 — toutes les 72 heures.

Méthode portable du dépôt : ``IntervalTrigger(days=3)``, comme demandé par la
carte kanban. À utiliser sur un hôte qui a un superviseur de processus
(systemd, s6, docker restart policy…) ; sur le conteneur Hermes, la
planification équivalente est assurée par le scheduler Hermes (cron toutes
les 72 h, supervisé par le service ``main-hermes``) — voir README.

Usage (dépendance apscheduler requise) ::

    uv run --with apscheduler python3 scheduler.py            # puis toutes les 72 h
    uv run --with apscheduler python3 scheduler.py --run-now  # 1 exécution immédiate

Chaque déclenchement exécute ``run_pipeline.py`` (discovery -> report ->
Telegram). Les erreurs sont notifiées sur Telegram par le pipeline lui-même
et le planificateur continue (les exceptions de job sont journalisées).
"""
from __future__ import annotations

import argparse
import logging
import pathlib
import subprocess
import sys

REPO_ROOT = pathlib.Path(__file__).resolve().parent

log = logging.getLogger("vigie24-scheduler")


def run_pipeline_once() -> None:
    log.info("Déclenchement du pipeline vigie24")
    try:
        proc = subprocess.run(
            [sys.executable, str(REPO_ROOT / "run_pipeline.py")],
            cwd=str(REPO_ROOT),
        )
        log.info("Pipeline terminé : exit=%d", proc.returncode)
    except Exception as exc:  # noqa: BLE001 — ne jamais bloquer le scheduler
        log.exception("Échec du pipeline : %s", exc)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(
        prog="scheduler.py",
        description="Planificateur APScheduler toutes les 72 h du pipeline vigie24.",
    )
    parser.add_argument("--run-now", action="store_true",
                        help="exécute une fois le pipeline immédiatement au démarrage")
    parser.add_argument("--verbose", action="store_true", help="debug logging")
    args = parser.parse_args(argv)

    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s %(levelname)s %(name)s %(message)s",
    )

    try:
        from apscheduler.schedulers.blocking import BlockingScheduler
        from apscheduler.triggers.interval import IntervalTrigger
    except ImportError as exc:
        log.error("apscheduler manquant : installez-le avec "
                  "`uv run --with apscheduler python3 scheduler.py` (%s)", exc)
        return 2

    if args.run_now:
        run_pipeline_once()

    scheduler = BlockingScheduler(timezone="UTC")
    scheduler.add_job(
        run_pipeline_once,
        IntervalTrigger(days=3),
        id="vigie24-report",
        name="Rapport vigie24 toutes les 72 h",
        replace_existing=True,
        misfire_grace_time=3600,
        coalesce=True,
        max_instances=1,
    )
    log.info("Scheduler démarré : prochaine exécution dans 3 jours (IntervalTrigger(days=3))")
    try:
        scheduler.start()
    except (KeyboardInterrupt, SystemExit):
        log.info("Scheduler arrêté")
    return 0


if __name__ == "__main__":
    sys.exit(main())
