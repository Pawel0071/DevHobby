# Parametry połączenia
$mongoHost = "localhost"
$mongoPort = 27017
$database = "game_config"
$collection = "tables"

# Generowanie danych
$levels = @{}
$xp = 200
for ($level = 1; $level -le 50; $level++) {
    $levels["$level"] = $xp
    $xp *= 2
}

# Tworzenie dokumentu JSON
$document = @{
    _id = "experience_levels"
    levels = $levels
} | ConvertTo-Json -Depth 3

# Zapis tymczasowy
$tempFile = "$env:TEMP\experience_levels.json"
$document | Out-File -Encoding UTF8 -FilePath $tempFile

# Wstrzyknięcie do MongoDB
$mongoImport = "mongoimport --host $mongoHost --port $mongoPort --db $database --collection $collection --drop --file `"$tempFile`" --jsonArray"
Invoke-Expression $mongoImport

# Czyszczenie
Remove-Item $tempFile

Write-Host "✅ Dane doświadczenia zostały załadowane do MongoDB."