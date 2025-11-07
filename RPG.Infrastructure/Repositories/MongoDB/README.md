# RPG.Infrastructure - Generic MongoDB Integration

## Architecture Overview

The Infrastructure layer provides **generic, reusable components** for MongoDB integration, following clean architecture principles and separation of concerns.

## Key Components

### 1. Generic MongoDB Consumer
**File:** `Mango/GenericMongoConsumer.cs`

A fully generic consumer that can save any domain entity to MongoDB using a mapper.

```csharp
public class GenericMongoConsumer<TEntity, TDocument> : IMangoConsumer<TEntity>
```

**Features:**
- Type-safe entity to document conversion
- Automatic upsert operations
- Logging support
- Flexible ID selector

**Usage:**
```csharp
var consumer = new GenericMongoConsumer<Item, ItemDocument>(
    collection: mongoCollection,
    mapper: new ItemDocumentMapper(),
    logger: logger,
    idSelector: doc => doc.Id
);

await consumer.Consume(item);
```

### 2. Document Mapper Interface
**File:** `Interfaces/IDocumentMapper.cs`

Defines the contract for converting between domain entities and MongoDB documents.

```csharp
public interface IDocumentMapper<TEntity, TDocument>
{
    TDocument ToDocument(TEntity entity);
    TEntity ToDomain(TDocument document);
}
```

### 3. Concrete Mappers
**File:** `Mappers/ItemDocumentMapper.cs`

Specific implementation for mapping `Item` domain entities to `ItemDocument` MongoDB documents.

**Responsibilities:**
- Convert domain entities to documents (ToDocument)
- Convert documents to domain entities (ToDomain)
- Handle component mapping (Stats, Sockets, Skills, Quest items)
- Support optional ItemTypeDefinition for domain reconstruction

## Benefits of This Architecture

### 🎯 **Separation of Concerns**
- **Infrastructure Layer**: Generic data access logic (consumers, repositories)
- **Mappers**: Conversion logic between domain and persistence models
- **Domain Layer**: Pure business logic, no infrastructure dependencies

### 🔄 **Reusability**
- `GenericMongoConsumer` can be used for ANY entity type
- Just create a new mapper for each document type
- No code duplication

### 🧪 **Testability**
- Easy to mock `IDocumentMapper` in tests
- Generic consumer can be tested once for all entity types
- Mappers can be unit tested independently

### 📦 **Extensibility**
- Add new entity types by creating new mappers
- Consumer logic remains unchanged
- Easy to add validation, caching, or other cross-cutting concerns

## Migration from Old Architecture

### Before (Specific Consumer):
```csharp
public class ItemSaveMangoConsumer : IMangoConsumer<Item>
{
    // Hardcoded Item-specific logic
    // Mixed concerns (data access + mapping)
}
```

### After (Generic Consumer + Mapper):
```csharp
// Reusable consumer
var consumer = new GenericMongoConsumer<Item, ItemDocument>(
    collection, 
    new ItemDocumentMapper(), 
    logger, 
    doc => doc.Id
);

// Dedicated mapper
public class ItemDocumentMapper : IDocumentMapper<Item, ItemDocument>
{
    // Pure mapping logic
}
```

## Adding New Entity Types

To add support for a new entity (e.g., `Character`):

1. **Create Document Class:**
```csharp
public class CharacterDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // ... other properties
}
```

2. **Create Mapper:**
```csharp
public class CharacterDocumentMapper : IDocumentMapper<Character, CharacterDocument>
{
    public CharacterDocument ToDocument(Character entity) { /* ... */ }
    public Character ToDomain(CharacterDocument document) { /* ... */ }
}
```

3. **Register Consumer:**
```csharp
services.AddSingleton<IMangoConsumer<Character>>(sp => 
    new GenericMongoConsumer<Character, CharacterDocument>(
        sp.GetRequiredService<IMongoCollection<CharacterDocument>>(),
        new CharacterDocumentMapper(),
        sp.GetRequiredService<ILogger<GenericMongoConsumer<Character, CharacterDocument>>>(),
        doc => doc.Id
    )
);
```

## Extension Methods

The `ItemDocumentExtensions` class provides convenient extension methods that delegate to the mapper:

```csharp
// Convert to domain
Item item = document.ToDomain(itemTypeDefinition);

// Convert to document
ItemDocument doc = item.ToDocument();
```

These are thin wrappers around `ItemDocumentMapper` for backward compatibility and convenience.

## Design Patterns Used

- **Repository Pattern**: `GenericMongoConsumer` acts as a write-only repository
- **Mapper Pattern**: `IDocumentMapper` separates mapping concerns
- **Generic Programming**: Type-safe operations across different entity types
- **Dependency Injection**: All components are interface-based and injectable

## Future Enhancements

Potential improvements to consider:

1. **Validation**: Add validation pipeline in the generic consumer
2. **Caching**: Integrate caching layer for frequently accessed documents
3. **Batch Operations**: Support bulk insert/update operations
4. **Change Tracking**: Track entity changes for audit logging
5. **Schema Migration**: Automatic document schema versioning and migration
