# RPG.GameServer

## Health i metryki
- Liveness: `GET /health/live`
- Readiness: `GET /health/ready`
- Prometheus: `GET /metrics`

### K8s probes (Deployment przykład)
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 15
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 15
```

