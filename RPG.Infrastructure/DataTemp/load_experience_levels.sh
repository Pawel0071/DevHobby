#!/bin/bash

# Parametry połączenia
MONGO_HOST="localhost"
MONGO_PORT="27017"
DATABASE="game_config"
COLLECTION="tables"
TEMP_FILE="/init/experience_levels.json"

# Generowanie danych JSON
echo '{ "_id": "experience_levels", "levels": {' > "$TEMP_FILE"

XP=200
for LEVEL in $(seq 1 50); do
    echo "  \"$LEVEL\": $XP," >> "$TEMP_FILE"
    XP=$((XP * 2))
done

# Usunięcie przecinka z ostatniej linii
sed -i '' '$ s/,$//' "$TEMP_FILE"

# Zakończenie dokumentu
echo '} }' >> "$TEMP_FILE"

# Import do MongoDB
mongoimport --host "$MONGO_HOST" --port "$MONGO_PORT" \
  --db "$DATABASE" --collection "$COLLECTION" \
  --drop --file "$TEMP_FILE" --jsonArray

# Czyszczenie
#rm "$TEMP_FILE"

echo "✅ Dane doświadczenia zostały załadowane do MongoDB."