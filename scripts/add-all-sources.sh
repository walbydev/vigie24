#!/bin/bash
# ============================================
# Script d'ajout de 200+ sources RSS
# ============================================
# Usage: ./scripts/add-all-sources.sh
#
# Ce script ajoute plus de 200 sources RSS technologiques
# françaises et internationales à la base de données.
#
# Catégories incluses :
# - Actualités Générales (France)
# - Sécurité Informatique
# - Open Source
# - DevOps & Cloud
# - IA & Data
# - Programmation
# - Hardware & Tech
# - Startups & Business
# - Mobile & Apps
# - Blockchain & Crypto
# - Podcasts Tech
# - Newsletters Tech
# - Recherche & Académique
# - YouTube Tech Channels
#
# Total: 200+ sources
# ============================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "========================================"
echo "  Vigie24 - Ajout de 200+ sources RSS"
echo "========================================"
echo ""

# Vérifier que le fichier SQL existe
if [ ! -f "$SCRIPT_DIR/add-all-sources.sql" ]; then
    echo "ERREUR: fichier SQL introuvable."
    echo "   Attendu: $SCRIPT_DIR/add-all-sources.sql"
    exit 1
fi

# Vérifier que Docker Compose est disponible
if ! command -v docker compose &> /dev/null; then
    echo "❌ Erreur: docker compose non trouvé"
    exit 1
fi

# Vérifier que les conteneurs sont en ligne
echo "🔍 Vérification des conteneurs..."
cd "$PROJECT_ROOT"
if ! docker compose ps | grep -q "Up"; then
    echo "❌ Erreur: Les conteneurs ne sont pas en ligne"
    echo "   Lancez: docker compose up -d"
    exit 1
fi

echo "✅ Conteneurs en ligne"
echo ""

# Exécuter le script SQL
echo "📝 Exécution du script SQL..."
cat "$SCRIPT_DIR/add-all-sources.sql" | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24

echo ""
echo "========================================"
echo "  ✅ Ajout terminé avec succès !"
echo "========================================"
echo ""

# Afficher le résumé
echo "📊 Résumé des sources ajoutées :"
echo ""
docker compose exec postgres psql -U postgres -d vigie24 -c "
  SELECT 'Total sources actives' as metric, COUNT(*)::text as value 
  FROM sources WHERE \"IsActive\" = true
  UNION ALL 
  SELECT 'Total sources inactives', COUNT(*)::text 
  FROM sources WHERE \"IsActive\" = false
  UNION ALL
  SELECT 'Total toutes sources', COUNT(*)::text 
  FROM sources;
"

echo ""
echo "📈 Répartition par catégorie :"
docker compose exec postgres psql -U postgres -d vigie24 -c "
  SELECT \"SourceKind\", COUNT(*) as count 
  FROM sources WHERE \"IsActive\" = true 
  GROUP BY \"SourceKind\" 
  ORDER BY count DESC;
"

echo ""
echo "🚀 Pour voir les nouvelles sources :"
echo "   docker compose exec postgres psql -U postgres -d vigie24 -c \"SELECT Name, WebsiteUrl FROM sources WHERE IsActive = true ORDER BY Name;\""
echo ""
