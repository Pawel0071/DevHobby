# DevHobby

Wieloserwisowa aplikacja RPG w .NET 8.0 (microservices). Repozytorium zawiera serwer gry (gRPC/ASP.NET), biblioteki domenowe, warstwy aplikacyjne i infrastrukturalne, oraz usługi pomocnicze (workers).

## Spis treści
- Architektura i struktura repozytorium
- Wymagania i środowisko
- Budowanie i uruchamianie (lokalnie i Docker)
- Testy (konwencje i uruchamianie)
- gRPC / Protobuf
- Konfiguracja, DI i logowanie
- Integracje (MongoDB, Redis, RabbitMQ)
- CI (GitHub Actions)
- Dodawanie nowego serwisu i modyfikacja encji współdzielonych
- Troubleshooting (najczęstsze problemy)

## Architektura i struktura repozytorium

- `RPG.Core/` — wspólne encje domenowe, interfejsy i usługi (kontrakty i logika współdzielona)
- `RPG.Domain/` — logika domenowa (modele i reguły)
- `RPG.Application/` — przypadki użycia i serwisy aplikacyjne
- `RPG.Infrastructure/` — infrastruktura (MongoDB, Redis, RabbitMQ, rejestracje DI, outbox itp.)
- `RPG.GameServer/` — serwer gry (ASP.NET/gRPC), kontrolery, Protos
`RPG.PersistenceService/` — serwis do zadań trwałości (worker)
- `RedisWormUp/`, `CricuitBraker/` — usługi pomocnicze typu worker
- `RPG.UI/` — UI (klient)
- `RPG.CLI/` — narzędzia CLI
- `RPG.UnitTest/` — testy jednostkowe; podkatalogi: `Core/*`, `InfrastructureTests/*` itd.

Pliki na poziomie repozytorium:
- `DevHobby.sln` — plik rozwiązania .NET
- `compose.yaml` — środowisko Docker Compose (uwaga: patrz sekcja Docker)
- `.github/workflows/ci.yml` — pipeline CI (GitHub Actions)

## Wymagania i środowisko
- .NET SDK 8.0+
- Opcjonalnie: Docker oraz Docker Compose

## Budowanie i uruchamianie

Pełne build rozwiązania:
```bash
dotnet build DevHobby.sln
```

Uruchomienie pojedynczego serwisu (np. GameServer):
```bash
cd RPG.GameServer
dotnet run
```

### Docker / Compose
W repo znajduje się aktualny `compose.yaml` uruchamiający infrastrukturę oraz wybrane serwisy workers:

- Usługi w compose:
	- `rpg.persistenceservice` (Dockerfile: `RPG.PersistenceService/Dockerfile`)
	- `redis.wormup` (Dockerfile: `RedisWormUp/Dockerfile`)
	- `circuitbreaker` (Dockerfile: `CricuitBraker/Dockerfile`)
	- `mongodb` (image: `mongo:latest`)
	- `rabbitmq` (image: `rabbitmq:management`)
	- `redis` (image: `redis:latest`)

Per‑service zmienne środowiskowe (zgodnie z `compose.yaml`):

- rpg.persistenceservice
	- `MONGO_URI` (np. `mongodb://mongo_user:mongo_pass@mongodb:27017/rpgdb`)
	- `REDIS_HOST`, `REDIS_PORT`
	- `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_USER`, `RABBITMQ_PASS`

- redis.wormup
	- `MONGO_URI`
	- `REDIS_HOST`, `REDIS_PORT`

- circuitbreaker
	- `MONGO_URI`
	- `REDIS_HOST`, `REDIS_PORT`
	- `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_USER`, `RABBITMQ_PASS`

Uwaga: kod serwisów powinien czytać powyższe zmienne poprzez konfigurację .NET (IConfiguration/Options). Jeśli dany serwis nadal używa wartości "localhost" zakodowanych na stałe, zaktualizuj `Program.cs`, aby korzystał z konfiguracji środowiskowej.

Start usług w kontenerach:
```bash
docker compose up --build
```

## Testy

Konwencje:
- Pliki testów kończą się na `*Tests.cs`
- Preferowane katalogi testów z sufiksem `Tests` (np. `InfrastructureTests`)

Uruchamianie testów:
```bash
dotnet test RPG.UnitTest/RPG.UnitTest.csproj
```

W CI testy uruchamiane są w konfiguracji Release.

## gRPC / Protobuf
- Definicje Protobuf znajdują się w `RPG.GameServer/Protos/`
- Zmiana `.proto` wymaga przebudowania, aby odświeżyć klasy generowane do gRPC

## Konfiguracja, DI i logowanie
- Każdy serwis ma własne `appsettings.json` i `appsettings.Development.json`
- Rejestracje usług przez wbudowany DI w `Program.cs`
- Logowanie jest konfigurowane w `appsettings.json` i używa `ILogger`

## Integracje zewnętrzne
- MongoDB — repozytoria i kolekcje rejestrowane w `RPG.Infrastructure`
- Redis — `IRedisCache` i implementacja `RedisCache`
  - **CacheKeyBuilder** — centralna klasa do budowania kluczy z prefiksami (np. `char:guid`, `item:id`)
  - **CacheTtl** — strategie TTL (Short/Medium/Long/Permanent)
- RabbitMQ — publisher i kanały konfiguracji
  - **NullRabbitPublisher** — Null Object Pattern gdy RabbitMQ nie jest skonfigurowany
  - **OutboxDispatcher** — reliable messaging z retry mechanism (max 3 próby)
- W testach jednostkowych integracje są mockowane (bez zależności od zewnętrznych serwisów)

### Health Checks
Projekt `RPG.Infrastructure` dostarcza health checks dla:
- MongoDB (`MongoHealthCheck`)
- Redis (`RedisHealthCheck`)
- RabbitMQ (`RabbitMqHealthCheck`)

Aby włączyć endpoint health checks w aplikacji:
```csharp
// Program.cs
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();
app.MapHealthChecks("/health");
```

Sprawdzenie stanu:
```bash
curl http://localhost:5000/health
```

📝 **Szczegółowa dokumentacja zmian w Infrastructure:** zobacz `INFRASTRUCTURE_CHANGES.md`

## CI (GitHub Actions)
Pipeline znajduje się w `.github/workflows/ci.yml` i wykonuje:
- checkout kodu
- setup .NET 8
- `dotnet restore`
- `dotnet build` w konfiguracji Release
- `dotnet test` w konfiguracji Release (bez ponownego builda)

Jeśli chcesz przyspieszyć CI, możesz dodać cache NuGet (przykład):
```yaml
- name: Cache NuGet
	uses: actions/cache@v4
	with:
		path: ~/.nuget/packages
		key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
		restore-keys: |
			${{ runner.os }}-nuget-
```

## Dodawanie nowego serwisu
1. Utwórz katalog serwisu i plik `.csproj`
2. Dodaj projekt do rozwiązania: `dotnet sln add <ścieżka-do-csproj>`
3. Skopiuj strukturę (np. `Program.cs`, `appsettings.json`)
4. Zarejestruj zależności w `Program.cs`
5. Dodaj testy do katalogu `Tests`

## Modyfikacja encji współdzielonej (`RPG.Core`)
1. Zaktualizuj encję/interfejs w `RPG.Core`
2. Zbuduj całość i uruchom testy, aby upewnić się, że zmiana jest kompatybilna wstecznie
3. Zaktualizuj serwisy zależne (jeśli to konieczne)

## Troubleshooting
- Błędy `dotnet build` dotyczące brakujących projektów — sprawdź wpisy w `DevHobby.sln`
- Błędy przestrzeni nazw/typów — sprawdź `ProjectReference` w `.csproj`
- Compose nie startuje — zaktualizuj ścieżki `dockerfile:` w `compose.yaml` do faktycznych katalogów
- Compose nie startuje — sprawdź, czy porty 27017/6379/5672/15672 nie są zajęte, oraz czy ścieżki Dockerfile wskazują na istniejące pliki (w tym repo są już poprawne)
- Testy w CI — upewnij się, że testy uruchamiane są w tej samej konfiguracji co build (`Release`)

---

Jeśli chcesz, mogę dodać sekcję z dokładnymi zmiennymi środowiskowymi dla każdego serwisu oraz przykładowe seedy bazy (Mongo). Napisz, które części rozwinąć.

