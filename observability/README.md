# Observability Stack - Grafana + Prometheus + Tempo + Loki

Pełny stack monitoringu i obserwacji aplikacji RPG w Dockerze.

## Co jest zawarte?

### 1. **Prometheus** (Metryki)
- Port: `9090`
- URL: http://localhost:9090
- **Zbiera**: Metryki z aplikacji .NET, MongoDB, Redis, RabbitMQ
- **Używane do**: Monitoring wydajności, liczników, CPU, pamięci

### 2. **Tempo** (Distributed Tracing)
- Port: `3200` (HTTP), `4317` (OTLP gRPC), `4318` (OTLP HTTP)
- **Zbiera**: OpenTelemetry traces z aplikacji
- **Używane do**: Śledzenie requestów przez mikroservisy, analiza latencji

### 3. **Loki** (Logi)
- Port: `3100`
- **Zbiera**: Logi z aplikacji
- **Używane do**: Centralne przechowywanie i przeszukiwanie logów

### 4. **Grafana** (Wizualizacja)
- Port: `3000`
- URL: http://localhost:3000
- Login: `admin` / `admin`
- **Funkcje**: Dashboardy, alerty, wizualizacja wszystkich danych

## Uruchomienie

```bash
# Uruchom cały stack
docker-compose up -d

# Sprawdź status
docker-compose ps

# Logi z Grafany
docker-compose logs -f grafana
```

## ✅ Already Integrated

**RPG.GameServer** is already configured with full OpenTelemetry support and now emits spans for MongoDB, Redis, and RabbitMQ operations through the shared `IActivityScope` abstraction:

- ✅ Traces exported to Tempo via OTLP (http://tempo:4317)
- ✅ Metrics exported to Prometheus via `/metrics` endpoint
- ✅ Custom ActivitySource `RPG.GameServer` automatically captured
- ✅ ASP.NET Core, gRPC, and HTTP client instrumentation enabled
- ✅ MongoDB/Redis repositories and RabbitMQ publisher/consumer create spans with database & messaging tags

**Configuration:**
- Production: `appsettings.json` → `"OtlpEndpoint": "http://tempo:4317"`
- Development: `appsettings.Development.json` → `"OtlpEndpoint": "http://localhost:4317"`

The `OpenTelemetryActivityScope` class automatically creates spans that are exported to Tempo.

---

## Integration with Other Services

### 1. Dodaj pakiety NuGet do projektów:

```bash
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Extensions.Hosting
```

### 2. Zaktualizuj `Program.cs`:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry Configuration
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("RPG.GameServer")  // Twój ActivitySource
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("RPG.GameServer"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://tempo:4317"); // OTLP gRPC
            });
    })
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("RPG.GameServer"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();  // Dodaje /metrics endpoint
    });

var app = builder.Build();

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();  // /metrics

app.Run();
```

### 3. Używanie OpenTelemetry w kodzie:

Twoja istniejąca klasa `OpenTelemetryActivityScope` już działa poprawnie! Teraz traces będą wysyłane do Tempo:

```csharp
public class MyService
{
    private readonly IActivityScope _activityScope;

    public MyService(IActivityScope activityScope)
    {
        _activityScope = activityScope;
    }

    public async Task DoWork()
    {
        using var activity = _activityScope.Start("MyService.DoWork", new Dictionary<string, object>
        {
            ["user.id"] = "123",
            ["operation"] = "create_character"
        });

        // Twój kod...
        await Task.Delay(100);
        
        // Trace będzie widoczny w Grafana -> Tempo
    }
}
```

## Dostęp do narzędzi

| Narzędzie | URL | Login/Hasło |
|-----------|-----|-------------|
| **Grafana** | http://localhost:3000 | admin / admin |
| **Prometheus** | http://localhost:9090 | - |
| **RabbitMQ Management** | http://localhost:15672 | rabbit_user / rabbit_pass |

## Grafana - Pierwsze kroki

1. **Otwórz Grafana**: http://localhost:3000
2. **Zaloguj się**: admin / admin
3. **Explore**:
   - Wybierz **Prometheus** → Query metryki (np. `http_requests_total`)
   - Wybierz **Tempo** → Wyszukaj traces po TraceID lub service
   - Wybierz **Loki** → Przeglądaj logi (np. `{service="RPG.GameServer"}`)

## Przykładowe zapytania

### Prometheus (Metryki)
```promql
# Request rate
rate(http_requests_total[5m])

# 95th percentile latency
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Memory usage
process_resident_memory_bytes / 1024 / 1024
```

### Loki (Logi)
```logql
# Wszystkie logi z GameServer
{service="RPG.GameServer"}

# Błędy z ostatniej godziny
{service="RPG.GameServer"} |= "error" | json

# Logi dla konkretnego użytkownika
{service="RPG.GameServer"} | json | user_id="123"
```

### Tempo (Traces)
- Wyszukaj po service name: `service.name="RPG.GameServer"`
- Wyszukaj wolne requesty: `duration > 1s`
- Wyszukaj błędy: `status=error`

## Monitoring infrastruktury

### MongoDB
`compose.yaml` uruchamia teraz `percona/mongodb_exporter` (port `9216`). Prometheus zbiera metryki z endpointu `mongodb_exporter:9216`.

### Redis
`compose.yaml` startuje `oliver006/redis_exporter` (port `9121`). Prometheus zbiera metryki z endpointu `redis_exporter:9121`.

## Alerty

Grafana wspiera alerty na podstawie metryk z Prometheusa:

1. Przejdź do **Alerting** → **Alert rules**
2. Stwórz nowy alert (np. "High Error Rate")
3. Skonfiguruj warunki (np. `error_rate > 5%`)
4. Dodaj notification channel (email, Slack, Discord)

## Dashboardy

Grafana ma setki gotowych dashboardów:

1. **Dashboards** → **Import**
2. Wpisz ID gotowego dashboardu:
   - **ASP.NET Core**: 10915
   - **MongoDB**: 2583
   - **Redis**: 763
   - **RabbitMQ**: 10991
3. Wybierz datasource (Prometheus) i importuj

## Troubleshooting

### Tempo nie odbiera traces
```bash
# Sprawdź logi
docker-compose logs tempo

# Sprawdź czy aplikacja wysyła na właściwy port
# W Program.cs: options.Endpoint = new Uri("http://tempo:4317");
```

### Prometheus nie zbiera metryk
```bash
# Sprawdź czy /metrics endpoint działa
curl http://localhost:5000/metrics

# Sprawdź konfigurację w prometheus.yml
docker-compose exec prometheus cat /etc/prometheus/prometheus.yml
```

### Grafana nie pokazuje danych
```bash
# Sprawdź datasources
http://localhost:3000/datasources

# Test connection dla każdego datasource
```

## Volumes (dane)

Wszystkie dane są przechowywane w Docker volumes:
```bash
# Lista volumes
docker volume ls | grep devhobby

# Usuń wszystkie dane (UWAGA: traci się historię!)
docker-compose down -v
```

## Production Recommendations

Dla produkcji rozważ:

1. **Persistent volumes** - montuj zewnętrzne dyski
2. **Retention policies** - skonfiguruj ile dni trzymać dane
3. **Authentication** - włącz auth dla wszystkich serwisów
4. **Limits** - ustaw limity CPU/Memory dla kontenerów
5. **Backup** - regularnie backupuj volumes
6. **Distributed setup** - dla większej skali użyj:
   - **Grafana Cloud** (managed)
   - **Thanos** (distributed Prometheus)
   - **Grafana Enterprise Traces** (scalable Tempo)

---

*Utworzono: 7 listopada 2025*
