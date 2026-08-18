#!/bin/bash
cd /home/philippe/RiderProjects/OpenSource/vigie24

# Extraire la clé API sans les commentaires
INFOMANIAK_API_KEY=$(grep "^INFOMANIAK_API_KEY=" .env | cut -d= -f2)

echo "=== Test connexion Infomaniak ==="
echo "Longueur clé: ${#INFOMANIAK_API_KEY}"
echo "Début clé: ${INFOMANIAK_API_KEY:0:15}..."

# Tester l'API
curl -s -X POST https://api.infomaniak.com/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $INFOMANIAK_API_KEY" \
  -d '{"model": "Qwen/Qwen3.5-122B-A10B-FP8", "messages": [{"role": "user", "content": "Test: dis-moi juste oui"}], "max_tokens": 5, "temperature": 0.3}' \
  | head -c 500
