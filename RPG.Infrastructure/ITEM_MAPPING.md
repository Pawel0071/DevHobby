# Item Mapping Architecture

## Odpowiedzialności

### ItemDocumentMapper
**Plik**: `Mappers/ItemDocumentMapper.cs`  
**Odpowiedzialność**: Główna logika konwersji między `Item` (domena) a `ItemDocument` (persistence)

**Metody**:
- `ToDocument(Item entity)` - konwertuje Item → ItemDocument
- `ToDomain(ItemDocument document)` - konwertuje ItemDocument → Item
- `CreateComponent(Type type, ItemDocument doc)` - **public static** - tworzy komponenty z dokumentu

**Używane przez**:
- `ItemDocumentExtensions` - deleguje do tego mappera
- Kod produkcyjny gdy potrzebuje stworzyć komponenty dynamicznie
- Unit testy

---

### ItemDocumentExtensions
**Plik**: `Extensions/ItemDocumentExtensions.cs`  
**Odpowiedzialność**: Extension methods dla wygodnego użycia

**Metody**:
- `ToDomain(this ItemDocument doc, ItemTypeDefinition? def)` - extension method
- `ToDocument(this Item item)` - extension method

**Używane przez**:
- Kod produkcyjny konwertujący ItemDocument ↔ Item
- Testy

**Uzasadnienie**: Extension methods są wygodne i czytelne: `doc.ToDomain()` zamiast `new ItemDocumentMapper().ToDomain(doc)`

---

## Przykłady użycia

### Podstawowa konwersja
```csharp
// Document → Domain
var item = itemDocument.ToDomain(itemTypeDefinition);

// Domain → Document
var document = item.ToDocument();
```

### Dynamiczne dodawanie komponentów (np. na podstawie tagów)
```csharp
var item = doc.ToDomain(def);

// Dodaj komponenty wymagane przez tagi
// UWAGA: CreateComponent zwraca null jeśli dokument nie ma wymaganych danych
// Nie każdy tag wymaga komponentu - niektóre tagi są tylko dla logiki biznesowej
foreach (var componentType in tagRegistry.GetRequiredComponents(item.Tags))
{
    var component = ItemDocumentMapper.CreateComponent(componentType, doc);
    if (component != null)  // Sprawdź czy dane były dostępne
        item.Components.Add(component);
}
```

---

## Usunięte duplikacje

### ~~ItemComponentFactory~~ → USUNIĘTE ✅
**Było**: Statyczna fabryka komponentów  
**Problem**: Duplikowała logikę z `ItemDocumentMapper.CreateComponent`  
**Rozwiązanie**: Fizycznie usunięta z projektu  
**Data usunięcia**: 7 listopada 2025

### ~~ItemFactory~~ → USUNIĘTE ✅
**Było**: Fabryka dodająca komponenty na podstawie tagów  
**Problem**: 
- Tylko używana w testach
- Zakładała że każdy tag wymaga komponentu (błędne założenie)
- Prosta logika łatwa do zreplikowania inline  
**Rozwiązanie**: Fizycznie usunięta z projektu  
**Data usunięcia**: 7 listopada 2025

---

## Flow diagramy

### Podstawowa konwersja
```
ItemDocument → ItemDocumentExtensions.ToDomain() → ItemDocumentMapper.ToDomain() → Item
Item → ItemDocumentExtensions.ToDocument() → ItemDocumentMapper.ToDocument() → ItemDocument
```

### Tworzenie z dynamicznymi komponentami
```
ItemDocument + ItemTypeDefinition
    ↓
doc.ToDomain(def) → Item z komponentami z definicji typu
    ↓
foreach(componentType in GetRequiredComponents(...))
    ↓
ItemDocumentMapper.CreateComponent(type, doc) → dodaj komponent
    ↓
Item z wszystkimi komponentami
```

---

## Testowanie

- **ItemDocumentMapperTests** - testuje mapper (ToDocument, ToDomain, CreateComponent)
- **ItemDocumentExtensionsTests** - testuje extension methods

---

*Utworzono: 7 listopada 2025*  
*Ostatnia aktualizacja: 7 listopada 2025*
