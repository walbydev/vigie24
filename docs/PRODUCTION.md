# 🖥️ Vigie24 — Mise en place de la production (VPS Debian)

Ce document vous guide pour **préparer votre VPS** (Debian 12 ou 13) afin que l'assistant puisse y déployer Vigie24 en production.

La stack de production est conteneurisée : **Caddy** (reverse proxy HTTPS auto) → **Vigie24.Web** (Blazor) → **PostgreSQL**, avec **Vigie24.Worker** (polling RSS + IA).

---

## 0. Prérequis

| Élément | Recommandation |
|---|---|
| OS | Debian 12 « Bookworm » ou 13 « Trixie » |
| CPU / RAM | 2 vCPU / 4 Go minimum (le worker + l'IA consomment) |
| Stockage | 20 Go SSD minimum (images Docker + base) |
| Ports | 22 (SSH), 80 et 443 (web) |
| Domaine (recommandé) | Un nom de domaine pointant vers l'IP du VPS (nécessaire pour le HTTPS automatique). Sans domaine, le site fonctionnera en HTTP sur l'IP. |

> ⏱️ Comptez **15-20 minutes** pour l'ensemble des étapes.

---

## 1. Mise à jour du système

Connectez-vous en **root** (ou avec votre utilisateur + `sudo`) :

```bash
apt update && apt upgrade -y
apt install -y curl ca-certificates sudo git
```

## 2. Créer l'utilisateur de déploiement

```bash
adduser vigie24deploy
usermod -aG sudo vigie24deploy
```

> L'utilisateur **vigie24deploy** servira uniquement au déploiement (SSH + Docker). Il n'a pas besoin de mot de passe SSH : on utilisera une clé.

## 3. Installer Docker et le plugin Compose

```bash
# Dépendances
apt install -y apt-transport-https ca-certificates curl gnupg lsb-release

# Dépôt officiel Docker
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(. /etc/os-release && echo "$VERSION_CODENAME") stable" > /etc/apt/keyrings/docker.list

apt update
apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# L'utilisateur de déploiement peut piloter Docker sans sudo
usermod -aG docker vigie24deploy

# Activer Docker au démarrage
systemctl enable --now docker
```

Vérification :

```bash
docker --version        # ex: Docker version 26.x
docker compose version  # ex: Docker Compose version v2.x
```

> Si l'OS est Debian 13 et que le dépôt `trixie` n'est pas encore disponible chez Docker, utilisez le dépôt `bookworm` (les paquets sont compatibles) :
> remplacez `$(. /etc/os-release && echo "$VERSION_CODENAME")` par `bookworm`.

## 4. Ajouter la clé SSH de déploiement

L'assistant se connectera au VPS avec la clé publique suivante :

```
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIFnnVk4Ju+gqpuuUvMdd0nBEBFtbSCTUShwJ5nS6ADp6 vigie24-prod-deploy
```

Installez-la pour l'utilisateur **vigie24deploy** (toujours en root) :

```bash
mkdir -p /home/vigie24deploy/.ssh
echo "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIFnnVk4Ju+gqpuuUvMdd0nBEBFtbSCTUShwJ5nS6ADp6 vigie24-prod-deploy" >> /home/vigie24deploy/.ssh/authorized_keys
chmod 700 /home/vigie24deploy/.ssh
chmod 600 /home/vigie24deploy/.ssh/authorized_keys
chown -R vigie24deploy:vigie24deploy /home/vigie24deploy/.ssh
```

> 🔐 La clé privée reste chez l'assistant ; elle n'est jamais partagée.

## 5. Ouvrir les ports

Si un pare-feu est actif (UFW ou celui du fournisseur) :

```bash
apt install -y ufw
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

> Vérifiez aussi la **console du fournisseur** (OVH, Hetzner, Scaleway, Infomaniak…) : le groupe de sécurité doit autoriser 22, 80 et 443 depuis Internet.

## 6. Nom de domaine (recommandé pour le HTTPS)

Créez un enregistrement **A** pointant vers l'IP publique du VPS :

```
vigie24.example.com  A  203.0.113.10
```

Caddy obtiendra automatiquement un certificat **Let's Encrypt** (HTTPS) au premier déploiement. Sans domaine, le site sera servi en HTTP sur l'IP (pas de certificat possible pour une IP).

---

## 7. Ce que vous devez me communiquer

Une fois les étapes 1 à 6 faites, envoyez-moi :

1. **L'IP publique** du VPS (ex: `203.0.113.10`)
2. **Le nom de domaine** (si vous en avez configuré un)
3. **La confirmation** que la clé publique ci-dessus est installée

À réception, je :
- configure l'alias SSH `vigie24-prod` en local,
- crée le `.env.production` (clés IA, mot de passe base, domaine),
- lance `make prod-deploy` (transfert du code + build des images + démarrage),
- vérifie le site (HTTP/HTTPS, WebSockets, worker) et vous donne les accès.

---

## 8. Après le déploiement — les commandes utiles

Depuis votre PC (dossier du projet) :

```bash
make prod-deploy      # déployer une nouvelle version (code + images)
make prod-status      # état des conteneurs
make prod-logs        # suivre les logs
make prod-backup      # sauvegarde PostgreSQL (dump daté dans backups/)
make prod-down        # arrêter la stack
```

Sur le VPS (dépanneur) :

```bash
cd ~/vigie24
docker compose --env-file .env.production -f docker-compose.prod.yml ps
docker compose --env-file .env.production -f docker-compose.prod.yml logs -f vigie24-worker
```

## 9. Sauvegardes (à mettre en place)

- `make prod-backup` génère un dump SQL dans `backups/` — à planifier en cron (ex: quotidien).
- Les données sont aussi persistées dans le volume Docker `vigie24_prod_data` (survivre à un `docker compose down`, pas à la suppression du VPS).
- Si le volume est perdu, la base est vide : il faudra re-ingérer (le worker repollera les flux).

---

## 10. Architecture de la stack

```
Internet ──► Caddy (80/443, HTTPS Let's Encrypt)
                └──► vigie24-web (Blazor Server, :8080, réseau interne)
                          └──► PostgreSQL 16 (volume vigie24_prod_data)
                └──► vigie24-worker (polling RSS → IA Infomaniak)
```

Fichiers clés du projet :
- `docker-compose.prod.yml` — orchestration de production
- `Caddyfile` — reverse proxy (domaine/IP via `VIGIE24_DOMAIN`)
- `.env.production.example` — modèle des secrets (à copier en `.env.production` **sur le VPS**, jamais commité)
- `appsettings.Production.json` — configuration non-sensible de production
- `Makefile` — cibles `prod-*`
