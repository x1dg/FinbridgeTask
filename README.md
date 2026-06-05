# Finbridge Task

A web service for managing users and their balances built with ASP.NET Core 10.0.

## Features

- Create users with Full Name, Date of Birth, Place of Birth
- Update user balance (with validation: non-negative and not exceeding max balance)
- Batch update balances for multiple users
- Retrieve user balance history (last 20 changes)
- Send user balance update events to Apache Kafka topic `users.events`
- REST API with JSON exchange format
- PostgreSQL database with **optimistic locking** (concurrency token) and **retry on conflict**
- MVC web interface (Bootstrap) for browsing users and history
- Global exception handling middleware (maps domain errors to HTTP codes)
- Rate limiting (fixed window: 10 req / 10s, queue 5)
- Swagger/OpenAPI documentation
- xUnit tests for core services
- Dockerized: `docker compose up` brings up Postgres + Kafka + API + Web
- CI on GitHub Actions (build + test + Docker build)

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/) (or use Docker)
- [Apache Kafka](https://kafka.apache.org/) (or use Docker)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — recommended for the one-liner startup

## Quick Start (Docker)

```bash
docker compose up --build
```

This starts:
- **Postgres** on `localhost:5432` (DB `finbridge`, user/password `postgres/postgres`)
- **Zookeeper** on `localhost:2181`
- **Kafka** on `localhost:29092` (internal `kafka:9092`)
- **Finbridge.Api** on `http://localhost:8080` (Swagger UI: `http://localhost:8080/swagger`)
- **Finbridge.Web** on `http://localhost:5000`

The API auto-creates the database schema on first start (`EnsureCreated()`).

Stop everything:

```bash
docker compose down
```

Drop the Postgres volume too:

```bash
docker compose down -v
```

## Manual Run (without Docker)

### 1. Start Postgres and Kafka

Use any way you like — local install, WSL, or partial compose:

```bash
docker compose up postgres zookeeper kafka -d
```

### 2. Configure

Default `Finbridge.Api/appsettings.json` points to `localhost:5432` and `localhost:9092`.
Override anything via env vars (double-underscore syntax):

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=finbridge;Username=postgres;Password=postgres"
export KafkaSettings__BootstrapServers="localhost:9092"
```

### 3. Run the API

```bash
dotnet run --project Finbridge.Api/Finbridge.Api.csproj
```

Swagger UI: `https://localhost:5001/swagger`

### 4. Run the Web UI

```bash
dotnet run --project Finbridge.Web/Finbridge.Web.csproj
```

Web UI: `https://localhost:5002` — points to the API at `https://localhost:5001` by default
(override with `ApiSettings__BaseUrl`).

## Configuration

`Finbridge.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=finbridge;Username=postgres;Password=postgres"
  },
  "BalanceSettings": {
    "MaxBalance": 1000000
  },
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "Topic": "users.events"
  }
}
```

`Finbridge.Web/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

## API Endpoints

### Users
- `POST /api/users` — Create a new user
- `GET  /api/users/{id}` — Get user by ID
- `GET  /api/users` — Get all users

### Balances
- `POST /api/balances` — Update balance for a single user
- `POST /api/balances/batch` — Update balances for multiple users
- `GET  /api/balances/history/{userId}` — Get balance history (last 20 changes)

All responses are JSON.

## Architecture

```
+----------------+        HTTP/JSON        +----------------+
|  Finbridge.Web |  ---------------------> |  Finbridge.Api |
|  (MVC, BS5)    |  <--------------------- |  (REST API)    |
+----------------+                         +--------+-------+
                                                     |
                                          EF Core   |   Confluent.Kafka
                                                     v   v
                                            +--------+---+-----------+
                                            |  Postgres |  Kafka    |
                                            +-----------------------+
```

### Projects

- **`Finbridge.Core`** — domain models: `User`, `BalanceHistory` (POCO, no deps).
- **`Finbridge.Data`** — `FinbridgeDbContext` (EF Core), `User.Version` as concurrency token.
- **`Finbridge.Api`** — REST controllers, `UserService` / `BalanceService`, `KafkaProducer`,
  `ExceptionHandlingMiddleware`, rate limiting, Swagger.
- **`Finbridge.Web`** — ASP.NET Core MVC client with `ApiService` (typed `HttpClient`).
- **`Finbridge.Tests`** — xUnit tests over `UserService` (InMemory EF Core).

### Concurrency model

`User.Version` is a `uint` row version marked as EF Core concurrency token.
`BalanceService.UpdateBalance` retries the transaction up to 3 times on
`DbUpdateConcurrencyException`, so two concurrent updates to the same user will
not silently overwrite each other — the loser re-reads and retries.

### Kafka events

On every successful balance change `KafkaProducer` publishes a JSON message
to topic `users.events`:

```json
{
  "eventType": "BalanceUpdated",
  "userId": 1,
  "fullName": "Ivan Ivanov",
  "oldBalance": 100.0,
  "newBalance": 250.0,
  "delta": 150.0,
  "timestamp": "2026-06-05T12:34:56Z"
}
```

In Docker, the API talks to the broker at `kafka:9092`; outside it uses
`localhost:9092`.

### Error handling

`ExceptionHandlingMiddleware` maps:

| Exception                  | HTTP |
|----------------------------|------|
| `KeyNotFoundException`     | 404  |
| `InvalidOperationException`| 400  |
| `ArgumentException`        | 400  |
| anything else              | 500  |

### Rate limiting

`AddRateLimiter` — fixed window: 10 requests per 10 seconds, queue up to 5
extra, oldest-first processing. Applied globally via `app.UseRateLimiter()`.

### Modern C# features in use

The project uses a number of .NET 7–10 language features (samples in
`Finbridge.Api/Features/`):

- Raw string literals, UTF-8 string literals
- Pattern matching (`is { }` / `is not null`)
- Init-only setters and `required` members
- Records and `with` expressions
- File-scoped namespaces
- Target-typed `new()`
- `IAsyncEnumerable<T>` (async streams)
- Lambda improvements (natural type, attributes)
- Inline arrays (struct buffers)
- Covariant return types

## Running Tests

```bash
dotnet test Finbridge.slnx
```

## CI

GitHub Actions workflow at `.github/workflows/ci.yml`:

1. `build-and-test` — restore, build Release, run xUnit on `ubuntu-latest`
2. `docker-build` — build both Docker images (after tests pass)
3. `docker-compose-validate` — `docker compose config` to catch syntax errors

## Implementation Notes

- Balance updates are wrapped in a transaction for batch operations to ensure consistency.
- After each balance update, an event is published to Kafka with the current user state.
- The service validates that balance cannot be negative and cannot exceed the configured maximum.
- Balance history is stored in the database and can be retrieved via the API.
- `EnsureCreated()` is used in the demo for first-run convenience; production should
  switch to EF Core migrations.

## License

This project is for educational purposes.
