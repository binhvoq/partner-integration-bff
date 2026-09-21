# Partner Integration BFF

## Architectural choices

sử dụng clean architecture và BFF, bài toán đang có 1 hệ thống legacy system. Mình viết 1 app BFF để thu thập request từ các partner/third party. App có 3 nhiêm vu chính, 

- đầu tiên là validation các field và kiểm tra verify các Partner có hợp lệ hay không?

- sau đó làm giàu request (Enrich) để hợp với legacy system

cuối cùng đẩy request vào meesage broker để legacy system xử lý từ từ

---

## Demonstrate how you would secure this endpoint

Hiện tại `POST /api/v1/partner/transactions` đã `[Authorize]` bằng `X-Api-Key`. Dummy verify để `[AllowAnonymous]` vì nó giả hệ thống ngoài; sau này đổi `[Authorize]` hoặc không public route `/internal`.

Production:

- HTTPS, key không để trong `appsettings.json`
- Mỗi partner một credential, và credential đó phải đúng với `partnerId` trong body
- Nếu số partner tăng: OAuth2 Client Credentials (xin token, không phải login user). Partner cố định thì thêm IP allow-list / mTLS
- Rate limit để một bên không spam queue

---

## How to run the project

Requires .NET 8 SDK and Docker Desktop.

**Option 1: run the API locally, queue in Docker**

```bash
docker compose up rabbitmq -d
dotnet run --project src/PartnerIntegration.Api
```

- Swagger: http://localhost:5263/swagger
- Health: http://localhost:5263/health
- RabbitMQ UI: http://localhost:15672 (`guest` / `guest`)

**Option 2: run everything in Docker**

```bash
docker compose up --build
```

- API: http://localhost:8080/swagger
- RabbitMQ UI: http://localhost:15672

Example request:

```bash
curl -X POST http://localhost:5263/api/v1/partner/transactions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-partner-api-key" \
  -d "{\"partnerId\":\"P-1001\",\"transactionReference\":\"TXN-99823\",\"amount\":250.00,\"currency\":\"USD\",\"timestamp\":\"2024-05-10T14:30:00Z\"}"
```

Valid partners: `P-1001`, `P-1002`, `P-2001`.

A successful call returns `202 Accepted` and publishes to `partner.transactions`.

To skip RabbitMQ, set `MessageBroker:Provider` to `InMemory`.

---

## How to run the tests

```bash
dotnet test
```

With coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage files are under `tests/PartnerIntegration.UnitTests/TestResults/`.
