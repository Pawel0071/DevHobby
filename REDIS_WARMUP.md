# RedisWarmUp Service

## Cel
RedisWarmUp to jednorazowy serwis startowy, który ładuje WSZYSTKIE dokumenty z MongoDB do Redis cache przed uruchomieniem GameServer.

## Architektura

### Przepływ
```
[START] RedisWarmUp
    ↓
1. Ładowanie CharacterDocument z MongoDB
   → Serialize to JSON
   → Write to Redis: "Characters:{id}"
    ↓
2. Ładowanie ItemDocument z MongoDB
   → Serialize to JSON
   → Write to Redis: "Items:{id}"
    ↓
3. Ładowanie SkillDocument...
4. Ładowanie QuestDocument...
5. Ładowanie NpcDocument...
6. Ładowanie PlayerDocument...
7. Ładowanie MapObjectDocument...
8. Ładowanie WorldStateDocument...
    ↓
[COMPLETE] → Exit (code 0)
    ↓
[GameServer może startować]
```

## Implementacja

### RedisWarmUpService.cs
```csharp
public class RedisWarmUpService
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IRedisDocumentRepository _redisRepository;
    private readonly ILogger _logger;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Load all document types
        await WarmUpCollection<CharacterDocument>(cancellationToken);
        await WarmUpCollection<ItemDocument>(cancellationToken);
        await WarmUpCollection<SkillDocument>(cancellationToken);
        // ... inne typy
    }

    private async Task<int> WarmUpCollection<TDocument>()
        where TDocument : class, IMongoDocument
    {
        // 1. Get collection name from Document type
        var collectionName = TDocument.CollectionName;
        
        // 2. Load ALL documents from MongoDB
        var collection = _mongoDatabase.GetCollection<TDocument>(collectionName);
        var documents = await collection.Find(_ => true).ToListAsync();
        
        // 3. Write each to Redis
        foreach (var document in documents)
        {
            var redisKey = $"{collectionName}:{document.Id}";
            var json = JsonSerializer.Serialize(document);
            await _redisRepository.WriteDocumentAsync(redisKey, json, TimeSpan.FromHours(24));
        }
        
        return documents.Count;
    }
}
```

### Program.cs
```csharp
var builder = Host.CreateApplicationBuilder(args);

// Register Infrastructure (MongoDB, Redis, Logging)
builder.Services.AddInfrastructure(builder.Configuration);

// Register RedisWarmUpService
builder.Services.AddSingleton<RedisWarmUpService>();

var host = builder.Build();

// Execute warm-up ONCE and exit
var warmUpService = host.Services.GetRequiredService<RedisWarmUpService>();
await warmUpService.ExecuteAsync();

return 0; // Success - GameServer can start
```

## Obsługiwane typy dokumentów

| Document Type | MongoDB Collection | Redis Key Pattern |
|--------------|-------------------|-------------------|
| CharacterDocument | Characters | `Characters:{guid}` |
| ItemDocument | Items | `Items:{guid}` |
| SkillDocument | Skills | `Skills:{guid}` |
| QuestDocument | Quests | `Quests:{guid}` |
| NpcDocument | Npcs | `Npcs:{guid}` |
| PlayerDocument | Players | `Players:{guid}` |
| MapObjectDocument | MapObjects | `MapObjects:{guid}` |
| WorldStateDocument | Worlds | `Worlds:{guid}` |

## Konfiguracja

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/redis-warmup-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### MongoDB Connection (appsettings.infrastructure.json)
```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://localhost:27017/rpg"
  }
}
```

### Redis Connection (appsettings.infrastructure.json)
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

## Użycie

### Standalone (przed GameServer)
```bash
# 1. Uruchom RedisWarmUp
dotnet run --project RedisWarmUp/RedisWarmUp.csproj

# Output:
# 🚀 Starting RedisWarmUp service
# Loading collection: Characters
#   📊 Characters: 1250 documents to load
#   ✅ Characters: 1250 documents loaded to Redis
# Loading collection: Items
#   📊 Items: 5432 documents to load
#   ✅ Items: 5432 documents loaded to Redis
# ...
# ✅ Redis WarmUp COMPLETED: 8764 documents loaded in 12.34s

# 2. Teraz uruchom GameServer
dotnet run --project RPG.GameServer/RPG.GameServer.csproj
```

### Docker Compose
```yaml
version: '3.8'

services:
  mongodb:
    image: mongo:7
    ports:
      - "27017:27017"
    volumes:
      - mongo-data:/data/db

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  redis-warmup:
    build:
      context: .
      dockerfile: RedisWarmUp/Dockerfile
    depends_on:
      - mongodb
      - redis
    environment:
      - ConnectionStrings__Mongo=mongodb://mongodb:27017/rpg
      - ConnectionStrings__Redis=redis:6379
    restart: "no"  # Run once and exit

  gameserver:
    build:
      context: .
      dockerfile: RPG.GameServer/Dockerfile
    depends_on:
      redis-warmup:
        condition: service_completed_successfully  # Wait for warmup
    ports:
      - "5000:5000"
    environment:
      - ConnectionStrings__Mongo=mongodb://mongodb:27017/rpg
      - ConnectionStrings__Redis=redis:6379

volumes:
  mongo-data:
```

## Kubernetes Init Container
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: rpg-gameserver
spec:
  replicas: 1
  template:
    spec:
      # Init container - runs before main container
      initContainers:
      - name: redis-warmup
        image: rpg-redis-warmup:latest
        env:
        - name: ConnectionStrings__Mongo
          value: "mongodb://mongodb-service:27017/rpg"
        - name: ConnectionStrings__Redis
          value: "redis-service:6379"
      
      # Main container - starts AFTER warmup completes
      containers:
      - name: gameserver
        image: rpg-gameserver:latest
        ports:
        - containerPort: 5000
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
```

## Metryki i Monitoring

### Log Output
```
[11:23:45 INF] 🚀 Starting RedisWarmUp service
[11:23:45 INF] Loading collection: Characters
[11:23:45 INF]   📊 Characters: 1250 documents to load
[11:23:47 INF]   ✅ Characters: 1250 documents loaded to Redis
[11:23:47 INF] Loading collection: Items
[11:23:47 INF]   📊 Items: 5432 documents to load
[11:23:52 INF]   ✅ Items: 5432 documents loaded to Redis
[11:23:52 INF] Loading collection: Skills
[11:23:52 INF]   ℹ️  Skills: empty, skipping
[11:23:52 INF] Loading collection: Quests
[11:23:52 INF]   📊 Quests: 123 documents to load
[11:23:53 INF]   ✅ Quests: 123 documents loaded to Redis
[11:23:53 INF] Loading collection: Npcs
[11:23:53 INF]   📊 Npcs: 456 documents to load
[11:23:55 INF]   ✅ Npcs: 456 documents loaded to Redis
[11:23:55 INF] Loading collection: Players
[11:23:55 INF]   📊 Players: 789 documents to load
[11:23:57 INF]   ✅ Players: 789 documents loaded to Redis
[11:23:57 INF] Loading collection: MapObjects
[11:23:57 INF]   📊 MapObjects: 234 documents to load
[11:23:58 INF]   ✅ MapObjects: 234 documents loaded to Redis
[11:23:58 INF] Loading collection: Worlds
[11:23:58 INF]   📊 Worlds: 1 documents to load
[11:23:58 INF]   ✅ Worlds: 1 documents loaded to Redis
[11:23:58 INF] ✅ Redis WarmUp COMPLETED: 8285 documents loaded in 13.24s
```

### Performance Metrics
- **Throughput**: ~625 documents/second (example)
- **Total Time**: ~13 seconds for 8000 documents
- **Redis Memory**: Depends on document size (estimate ~100KB per character)

## Error Handling

### MongoDB Connection Error
```
[11:23:45 ERR] ❌ Redis WarmUp FAILED
MongoDB.Driver.MongoConnectionException: Unable to connect to MongoDB
Exit Code: 1
```

### Redis Connection Error
```
[11:23:45 ERR] Error warming up collection CharacterDocument
StackExchange.Redis.RedisConnectionException: Unable to connect to Redis
```

### Empty Collection
```
[11:23:52 INF] Loading collection: Skills
[11:23:52 INF]   ℹ️  Skills: empty, skipping
```

## Troubleshooting

### Q: WarmUp trwa bardzo długo
**A:** Zmniejsz batch size lub dodaj indeksy w MongoDB

### Q: Redis wypełnia się za szybko
**A:** Ustaw krótszy TTL (np. 1h zamiast 24h) lub ogranicz typy dokumentów

### Q: GameServer nie widzi danych
**A:** Sprawdź czy WarmUp zakończył się sukcesem (exit code 0)

### Q: Duplikaty w Redis
**A:** Normalne - używamy UPSERT, więc duplikaty są nadpisywane

## Zalety

✅ **Fast Startup**: GameServer ma wszystkie dane w Redis od razu  
✅ **Simple**: Uruchamia się raz i kończy  
✅ **No State**: Bezstanowy - można uruchomić ponownie w każdej chwili  
✅ **Observable**: Logi pokazują dokładny postęp  
✅ **Idempotent**: Można uruchomić wielokrotnie bez skutków ubocznych  

## Wady i Ograniczenia

⚠️ **Cold Start**: Pierwsze uruchomienie może trwać długo  
⚠️ **Memory**: Redis musi pomieścić wszystkie dokumenty  
⚠️ **Freshness**: Dane mogą być nieświeże (używaj PersistenceService do aktualizacji)  
⚠️ **Single Point**: Jeśli fail, GameServer nie może startować  

## Future Enhancements

- [ ] Parallel loading (load multiple collections at once)
- [ ] Incremental warmup (only load new/updated documents)
- [ ] Health check endpoint podczas warmup
- [ ] Prometheus metrics export
- [ ] Retry logic for transient errors
- [ ] Delta warmup (only changed since last run)
