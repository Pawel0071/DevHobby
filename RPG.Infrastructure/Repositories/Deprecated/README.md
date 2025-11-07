# Deprecated - Do Usunięcia

Ten katalog zawiera klasy, które będą usunięte w przyszłości.

## Pliki

### CharacterStatsLoader.cs
- **Namespace**: `RPG.Infrastructure.Repositories.Deprecated`
- **Powód deprecation**: Statyczne ładowanie statystyk z JSON - do zastąpienia bardziej elastycznym rozwiązaniem
- **Status**: Do usunięcia

### ExperienceProvider.cs
- **Namespace**: `RPG.Infrastructure.Repositories.Deprecated`
- **Powód deprecation**: Ładowanie tabeli doświadczenia z MongoDB - do zastąpienia konfiguracją
- **Interfejs**: `IExperienceProvider` (RPG.Domain)
- **Używane przez**: `LevelingService` (RPG.Core)
- **Status**: Do usunięcia po refaktoryzacji LevelingService

## Usunięte pliki (historia)

### ~~ItemComponentFactory.cs~~ - USUNIĘTE ✅
- **Usunięte**: 7 listopada 2025
- **Powód**: Duplikowało logikę z `ItemDocumentMapper.CreateComponent`
- **Zastąpione przez**: `ItemDocumentMapper.CreateComponent(Type, ItemDocument)` (public static)

### ~~ItemFactory.cs~~ - USUNIĘTE ✅
- **Usunięte**: 7 listopada 2025
- **Powód**: Używane tylko w testach, zakładało że każdy tag wymaga komponentu
- **Zastąpione przez**: Bezpośrednie użycie `ItemDocumentMapper.CreateComponent`

### ~~CachedItemRepository.cs~~ - USUNIĘTE ✅
- **Usunięte**: 7 listopada 2025
- **Powód**: Używało nieistniejącego interfejsu `IItemRepository`, nigdy nie było używane w produkcji
- **Zastąpione przez**: Bezpośrednie użycie Redis + MongoDB w repozytorium
- **Testy**: CachedItemRepositoryTests.cs również usunięte

## Akcje przed usunięciem

1. **ExperienceProvider**:
   - Przenieś tabelę doświadczenia do konfiguracji (appsettings.json)
   - Zaktualizuj `LevelingService` aby nie używał `IExperienceProvider`
   - Usuń interface `IExperienceProvider` z RPG.Domain

2. **CharacterStatsLoader**:
   - Sprawdź czy jest gdzieś używany
   - Przenieś dane do MongoDB lub konfiguracji
   - Usuń plik

---
*Utworzono: 7 listopada 2025*  
*Ostatnia aktualizacja: 7 listopada 2025*
