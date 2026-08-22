#!/bin/bash
# Test rapide de l'API Infomaniak AI (alias de test-infomaniak.sh)
# La cle est lue depuis .env et JAMAIS affichee
cd "$(dirname "$0")/.." || exit 1

INFOMANIAK_API_KEY=$(grep "^INFOMANIAK_API_KEY=" .env | cut -d= -f2)
BASE_URL=$(grep "^INFOMANIAK_API_URL=" .env | cut -d= -f2)
MODEL=$(grep "^AI_MODEL=" .env | cut -d= -f2)

[ -n "$INFOMANIAK_API_KEY" ] || { echo "ERREUR: INFOMANIAK_API_KEY manquante dans .env"; exit 1; }
[ -n "$BASE_URL" ] || { echo "ERREUR: INFOMANIAK_API_URL manquante dans .env"; exit 1; }

echo "=== Test API Infomaniak ($BASE_URL/chat/completions) ==="
echo "Longueur cle: ${#INFOMANIAK_API_KEY} (jamais affichee)"
echo "Modele: $MODEL"
echo

curl -s -X POST "$BASE_URL/chat/completions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $INFOMANIAK_API_KEY" \
  -d "{\"model\": \"$MODEL\", \"messages\": [{\"role\": \"user\", \"content\": \"Hello\"}], \"max_tokens\": 10}" \
  | head -c 500
echo
