#!/bin/bash
cd /home/philippe/RiderProjects/OpenSource/vigie24

# Extraire la clé API
INFOMANIAK_API_KEY=$(grep "^INFOMANIAK_API_KEY=" .env | cut -d= -f2)

echo "=== Test connexion API Infomaniak ==="
echo "Longueur clé: ${#INFOMANIAK_API_KEY}"
echo "Début clé: ${INFOMANIAK_API_KEY:0:15}..."
echo ""

# Tester l'API
curl -s -X POST https://api.infomaniak.com/1.0/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $INFOMANIAK_API_KEY" \
  -d '{"model":"Qwen/Qwen3.5-122B-A10B-FP8","messages":[{"role":"user","content":"Hello"}],"max_tokens":10}' \
  | jq '.' 2>/dev/null || echo "(Pas de jq, affichage brut)"
