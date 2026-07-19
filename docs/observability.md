# Observability foundation

**Status:** Baseline shipped (pre–Payment)  
**Endpoints:** `/health`, `/health/live`, `/health/ready`, `/metrics`  
**Header:** `X-Correlation-Id` (echoed on response; pushed to Serilog + Activity tags)

## Stack

| Concern | Implementation |
|---------|----------------|
| Tracing | OpenTelemetry ASP.NET Core + HttpClient + EF Core |
| Metrics | OTel meters + Prometheus scrape (`/metrics`) |
| Logs | Serilog structured + `CorrelationId` enricher |
| Health | self (live) · postgres · redis (ready) |
| Export | Prometheus always; OTLP when `OpenTelemetry:OtlpEndpoint` set; console traces in Development |

## Booking metrics

| Name | Type | When |
|------|------|------|
| `booking_created_total` | counter | Create booking success |
| `booking_conflict_total` | counter | Availability / EXCLUDE conflict |
| `booking_expired_total` | counter | Expire worker batch |
| `idempotency_conflict_total` | counter | Payload mismatch / in-progress |
| `expire_worker_duration` | histogram (ms) | Per tick |
| `expire_worker_failed_total` | counter | Tick exception |

Config: `OpenTelemetry:ServiceName`, `OpenTelemetry:OtlpEndpoint`.
