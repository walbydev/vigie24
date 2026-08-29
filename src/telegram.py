#!/usr/bin/env python3
"""Envoi Telegram du rapport vigie24 (API Bot, stdlib only).

Fournit ``send_report`` (le rapport Markdown est converti en HTML Telegram,
découpé en messages <= 4096 caractères) et ``send_error`` (alerte d'échec du
pipeline). Le jeton du bot et le chat_id sont lus depuis les variables
d'environnement ``VIGIE24_TELEGRAM_BOT_TOKEN`` et ``VIGIE24_TELEGRAM_CHAT_ID``
— aucun secret n'est versionné (voir ``.env.example``).

Usage CLI (dry-run manuel de l'envoi) ::

    VIGIE24_TELEGRAM_BOT_TOKEN=... VIGIE24_TELEGRAM_CHAT_ID=... \\
        python3 -m src.telegram --send-report report.md

    python3 -m src.telegram --send-error "le pipeline a échoué"

Exit codes : 0 = envoi réussi, 2 = usage/config (variables manquantes),
1 = échec de l'API Telegram.
"""
from __future__ import annotations

import argparse
import html
import json
import logging
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from typing import Dict, List, Optional

__version__ = "1.0.0"

API_BASE = "https://api.telegram.org/bot{token}/{method}"
MAX_MESSAGE_CHARS = 4096
MAX_CHUNK_CHARS = 3900  # marge de sécurité sous la limite Telegram
HTTP_TIMEOUT = 25

log = logging.getLogger("telegram")

# Tokeniseur Markdown: [texte](url) -> (texte, url). Les URLs sont capturées
# AVANT l'échappement HTML pour préserver les '&' des query strings.
_LINK_RE = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")
_BOLD_RE = re.compile(r"\*\*(.+?)\*\*")
_ITALIC_RE = re.compile(r"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)")


class TelegramError(RuntimeError):
    """Échec d'un appel à l'API Telegram."""


def env_token() -> str:
    return os.environ.get("VIGIE24_TELEGRAM_BOT_TOKEN", "").strip()


def env_chat_id() -> str:
    return os.environ.get("VIGIE24_TELEGRAM_CHAT_ID", "").strip()


def _require_config() -> None:
    """Lève TelegramError si la config d'environnement est incomplète."""
    missing = [name for name, val in (
        ("VIGIE24_TELEGRAM_BOT_TOKEN", env_token()),
        ("VIGIE24_TELEGRAM_CHAT_ID", env_chat_id()),
    ) if not val]
    if missing:
        raise TelegramError(
            "Variables d'environnement manquantes : "
            + ", ".join(missing)
            + " (voir .env.example — secrets hors git)"
        )


def _api(method: str, payload: Dict[str, str]) -> Dict:
    """POST form-encoded vers l'API Bot ; lève TelegramError si !ok."""
    _require_config()
    token = env_token()
    url = API_BASE.format(token=token, method=method)
    data = urllib.parse.urlencode(payload).encode("utf-8")
    req = urllib.request.Request(url, data=data, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=HTTP_TIMEOUT) as resp:
            body = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", "replace")[:300]
        raise TelegramError(f"HTTP {exc.code} de l'API Telegram ({method}) : {detail}") from exc
    except urllib.error.URLError as exc:
        raise TelegramError(f"Réseau injoignable pour l'API Telegram ({method}) : {exc.reason}") from exc
    if not body.get("ok"):
        desc = (body.get("description") or "réponse inattendue")[:300]
        raise TelegramError(f"API Telegram ({method}) refusée : {desc}")
    return body


def send_message(text: str, chat_id: Optional[str] = None,
                 parse_mode: Optional[str] = "HTML") -> Dict:
    """Envoie un message texte (HTML par défaut) et retourne la réponse API."""
    chat_id = chat_id or env_chat_id()
    payload = {"chat_id": chat_id, "text": text}
    if parse_mode:
        payload["parse_mode"] = parse_mode
    body = _api("sendMessage", payload)
    res = body.get("result", {})
    log.info("sendMessage OK: message_id=%s chat_id=%s", res.get("message_id"), res.get("chat", {}).get("id"))
    return body


def send_error(message: str, chat_id: Optional[str] = None) -> Dict:
    """Alerte d'échec du pipeline : texte brut préfixé ⚠️, tronqué à 4000 car."""
    text = ("⚠️ Vigie24 — " + message).strip()
    if len(text) > MAX_MESSAGE_CHARS - 96:
        text = text[: MAX_MESSAGE_CHARS - 96] + "…"
    return send_message(text, chat_id=chat_id, parse_mode=None)


def _inline_markdown(line: str) -> str:
    """Convertit les liens [t](u) et la graisse **b** d'une ligne en HTML,
    en échappant les entités HTML (hors URLs des liens)."""
    out: List[str] = []
    pos = 0
    for m in _LINK_RE.finditer(line):
        out.append(html.escape(line[pos:m.start()]))
        label = html.escape(m.group(1))
        url = html.escape(m.group(2), quote=True)
        out.append(f'<a href="{url}">{label}</a>')
        pos = m.end()
    out.append(html.escape(line[pos:]))
    escaped = "".join(out)
    escaped = _BOLD_RE.sub(r"<b>\1</b>", escaped)
    escaped = _ITALIC_RE.sub(r"<i>\1</i>", escaped)
    return escaped


def markdown_to_html(md: str) -> str:
    """Convertit le rapport Markdown vigie24 en HTML Telegram.

    Règles : ``# `` -> titre gras ; ``## `` -> en-tête de catégorie (🗂) ;
    ``### [t](u)`` -> lien gras ; ``- **Label :** v`` -> puce avec label gras ;
    ``*...*`` -> italique ; ``---`` -> filet. Tout le reste est échappé.
    """
    lines: List[str] = []
    for raw in md.splitlines():
        line = raw.rstrip()
        if line.startswith("### "):
            body = line[4:].strip()
            m = _LINK_RE.fullmatch(body)
            if m:
                lines.append(f'<a href="{html.escape(m.group(2), quote=True)}">'
                             f'<b>{html.escape(m.group(1))}</b></a>')
            else:
                lines.append("<b>" + _inline_markdown(body) + "</b>")
        elif line.startswith("## "):
            lines.append("<b>🗂 " + _inline_markdown(line[3:].strip()) + "</b>")
        elif line.startswith("# "):
            lines.append("<b>" + _inline_markdown(line[2:].strip()) + "</b>")
        elif line.strip() == "---":
            lines.append("┄" * 24)
        elif line.startswith("- "):
            body = _inline_markdown(line[2:].strip())
            # "- **Label :** valeur" -> "• <b>Label :</b> valeur"
            if body.startswith("<b>"):
                lines.append("• " + body)
            else:
                lines.append("• " + body)
        elif line.strip():
            lines.append(_inline_markdown(line.strip()))
        else:
            lines.append("")
    return "\n".join(lines)


def _split_block(block: str, limit: int) -> List[str]:
    """Découpe un bloc trop grand sur les fins de ligne, jamais au milieu d'une
    balise HTML (repli sur le dernier espace, puis coupe dure avec '…')."""
    if len(block) <= limit:
        return [block]
    chunks: List[str] = []
    rest = block
    while len(rest) > limit:
        cut = rest.rfind("\n", 0, limit)
        if cut <= 0:
            cut = rest.rfind(" ", 0, limit)
        if cut <= 0:
            chunks.append(rest[: limit - 1] + "…")
            rest = rest[limit - 1:]
        else:
            chunks.append(rest[:cut].rstrip())
            rest = rest[cut:].lstrip("\n")
    if rest:
        chunks.append(rest)
    return chunks


def chunk_html(html_text: str, limit: int = MAX_CHUNK_CHARS) -> List[str]:
    """Découpe le HTML en messages Telegram : les blocs (séparés par des lignes
    vides) restent entiers tant que la limite le permet."""
    blocks = html_text.split("\n\n")
    chunks: List[str] = []
    current = ""
    for block in blocks:
        if len(block) > limit:
            if current:
                chunks.append(current)
                current = ""
            chunks.extend(_split_block(block, limit))
            continue
        if current and len(current) + len(block) + 2 > limit:
            chunks.append(current)
            current = block
        else:
            current = (current + "\n\n" + block) if current else block
    if current:
        chunks.append(current)
    return chunks


def send_report(report_path: str, chat_id: Optional[str] = None) -> List[Dict]:
    """Envoie le rapport Markdown sur Telegram (1 message par morceau HTML).

    Retourne la liste des réponses API ; lève TelegramError si l'envoi échoue.
    """
    try:
        with open(report_path, encoding="utf-8") as fh:
            md = fh.read()
    except OSError as exc:
        raise TelegramError(f"Rapport illisible ({report_path}) : {exc}") from exc

    html_text = markdown_to_html(md)
    chunks = chunk_html(html_text)
    log.info("Rapport %s : %d caractères -> %d message(s) Telegram",
             report_path, len(html_text), len(chunks))
    results: List[Dict] = []
    for i, chunk in enumerate(chunks, start=1):
        if len(chunks) > 1:
            chunk = f"<i>({i}/{len(chunks)})</i>\n{chunk}"
        results.append(send_message(chunk, chat_id=chat_id, parse_mode="HTML"))
    return results


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        prog="python3 -m src.telegram",
        description="Envoi manuel (dry-run) du rapport ou d'une alerte sur Telegram.",
    )
    parser.add_argument("--send-report", metavar="PATH", help="rapport Markdown à envoyer")
    parser.add_argument("--send-error", metavar="MESSAGE", help="alerte d'erreur à envoyer")
    parser.add_argument("--chat-id", default=None, help="override du chat_id (défaut: env)")
    parser.add_argument("--verbose", action="store_true", help="debug logging")
    args = parser.parse_args(argv)

    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
    )
    if not args.send_report and not args.send_error:
        parser.error("fournir --send-report PATH ou --send-error MESSAGE")
    try:
        if args.send_report:
            results = send_report(args.send_report, chat_id=args.chat_id)
            log.info("Envoi réussi : %d message(s)", len(results))
        else:
            res = send_error(args.send_error, chat_id=args.chat_id)
            log.info("Alerte envoyée : message_id=%s", res.get("result", {}).get("message_id"))
    except TelegramError as exc:
        log.error("%s", exc)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
