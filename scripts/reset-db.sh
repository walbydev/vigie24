#!/bin/bash
# ============================================
# Complete reset script for Vigie24
# ============================================
# Usage: ./scripts/reset-db.sh
#
# This script deletes all database data
# and re-inserts the base categories, tags and sources.
# ============================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SQL_FILE="$SCRIPT_DIR/reset-database.sql"

echo "🔄 Réinitialisation de la base de données Vigie24..."
echo ""

# Check that the SQL file exists
if [ ! -f "$SQL_FILE" ]; then
    echo "❌ Erreur: Fichier SQL non trouvé: $SQL_FILE"
    exit 1
fi

# Check that Docker Compose is available
if ! command -v docker compose &> /dev/null; then
    echo "❌ Erreur: docker compose n'est pas disponible"
    exit 1
fi

# Run the SQL script
cd "$PROJECT_ROOT"
echo "📝 Exécution du script SQL..."
cat "$SQL_FILE" | docker compose exec -e PGPASSWORD=postgres postgres psql -U postgres -d vigie24

echo ""
echo "✅ Réinitialisation terminée avec succès !"
echo ""
echo "📊 Résumé :"
echo "   - Catégories: 8"
echo "   - Tags: 15"
echo "   - Sources RSS: 16"
echo ""
echo "🔗 Pour voir les sources: docker compose exec postgres psql -U postgres -d vigie24 -c \"SELECT Name, FeedUrl, SourceKind FROM sources;\""
