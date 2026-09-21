# Partner Integration BFF

.NET 8 Backend-for-Frontend that accepts partner transactions, verifies the partner against an unreliable upstream API, and queues accepted work for legacy systems.

## Architecture

```
Partner  --API key-->  Partner Integration BFF
                           |  1. FluentValidation
                           |  2. HTTP call to dummy Partner Verification API
                           |     (30% TimeoutException, Polly retry)
                           v
                      RabbitMQ queue  -->  legacy processors
```

The solution follows a pragmatic Clean Architecture split:

| Project | Responsibility |
| --- | --- |
| `PartnerIntegration.Domain` | Transaction entity and currency value object |
| `PartnerIntegration.Application` | Use case, contracts, FluentValidation, ports |
| `PartnerIntegration.Infrastructure` | HttpClient, Polly retries, RabbitMQ / in-memory publisher, dummy failure injector |
| `PartnerIntegration.Api` | Controllers, API key auth, global exception handler |
| `PartnerIntegration.UnitTests` | Validation, resilience, service, and API tests |

The dummy Partner Verification API lives in the same host at `GET /internal/v1/partners/{partnerId}/verify`. The transaction use case still calls it over HTTP, the same way it would call a real external service.

Known partners used by the dummy catalog: `P-1001`, `P-1002`, `P-2001`.

## Run locally

Prerequisites: .NET 8 SDK, Docker Desktop.

```bash
docker compose up rabbitmq -d
dotnet run --project src/PartnerIntegration.Api
```

Swagger: http://localhost:5263/swagger  
Health: http://localhost:5263/health

Example request:

```bash
curl -X POST http://localhost:5263/api/v1/partner/transactions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-partner-api-key" \
  -d "{\"partnerId\":\"P-1001\",\"transactionReference\":\"TXN-99823\",\"amount\":250.00,\"currency\":\"USD\",\"timestamp\":\"2024-05-10T14:30:00Z\"}"
```

A successful request returns `202 Accepted` and publishes a durable JSON message to the `partner.transactions` queue. Inspect messages in the RabbitMQ UI at http://localhost:15672 (`guest` / `guest`).

To run without RabbitMQ, set `MessageBroker:Provider` to `InMemory`.

## Run with Docker

```bash
docker compose up --build
```

API: http://localhost:8080/swagger  
RabbitMQ UI: http://localhost:15672

Use the same API key (`dev-partner-api-key`) and change the host to port `8080`.

## Tests

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage collector output is written under `tests/PartnerIntegration.UnitTests/TestResults/`.

The tests cover:

- Payload validation (required fields, amount `> 0`, ISO 4217 currency)
- Dummy `TimeoutException` injection at the 30% threshold
- Polly retries on `TimeoutException` / HTTP 5xx, plus retry exhaustion
- Application service mapping of verification and messaging failures
- Endpoint behaviour: 401, 400, 422, 503, 202, and dummy 504

## Design notes

**Resilience.** The dummy endpoint throws `TimeoutException` with probability `0.3`. The global handler maps that to HTTP 504. The typed `HttpClient` converts 504 / 5xx / client timeouts into retryable exceptions. Polly retries with exponential backoff and jitter (`MaxRetryAttempts = 3`). If the budget is exhausted, the BFF returns `503` instead of crashing the incoming request.

**Messaging.** `ITransactionQueuePublisher` is the port. `RabbitMqTransactionPublisher` is the production adapter (durable queue, persistent messages, automatic recovery, lazy connect). `InMemoryTransactionQueuePublisher` is used by tests and local fallback.

**Errors.** `IExceptionHandler` returns RFC 7807 problem details with a stable shape (`title`, `detail`, `status`, `traceId`, and validation `errors`).

**Security.** The transaction endpoint is authenticated with an API key (`X-Api-Key`) using a custom authentication handler and a constant-time comparison. Swagger is configured so reviewers can send the header. In production this would sit behind TLS, secret rotation, partner-scoped keys, and IP allow-lists / mTLS. The dummy verification route is anonymous because it stands in for an external system.

**Validation.** All fields are required. Amount must be greater than zero. Currency must be a supported ISO 4217 code. Timestamp cannot be far in the future.

## Configuration

| Key | Default | Meaning |
| --- | --- | --- |
| `Security:ApiKey` | `dev-partner-api-key` | Required header value |
| `PartnerVerification:TimeoutProbability` | `0.3` | Dummy timeout rate |
| `PartnerVerification:MaxRetryAttempts` | `3` | Polly retries after the first attempt |
| `MessageBroker:Provider` | `RabbitMQ` | `RabbitMQ` or `InMemory` |
| `MessageBroker:QueueName` | `partner.transactions` | Target queue |
