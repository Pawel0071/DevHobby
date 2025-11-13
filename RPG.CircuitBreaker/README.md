# RPG.CircuitBreaker

Rola: Processor outboxu. Ten serwis:
- Czyta wiadomości z kolejki Redis (`outbox:pending` oraz `outbox:retry`)
- Publikuje je do RabbitMQ (topic exchange)
- Stosuje Circuit Breaker:
  - Closed – normalna praca
  - Open – po serii błędów publikacja wstrzymana na czas
  - Half-open – pojedyncze próby; sukces -> Closed, błąd -> Open
- Ekspozycja health endpoints i metryk:
  - Liveness: `GET /health/live`
  - Readiness: `GET /health/ready`
  - Prometheus: `GET /metrics`

## K8s probes (przykład Deployment)

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 80
  initialDelaySeconds: 10
  periodSeconds: 15
readinessProbe:
  httpGet:
    path: /health/ready
    port: 80
  initialDelaySeconds: 10
  periodSeconds: 15
```

## Zmienne środowiskowe (ważne)
- `Outbox__Enabled=true`
- `Outbox__Role=Processor`
- `ConnectionStrings__Redis`, `ConnectionStrings__Mongo`
- Sekcja `RabbitMQ` (Host/Port/User/Pass/VHost/ExchangeName/ExchangeType)

