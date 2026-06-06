# Finbridge Task

Веб-сервис для управления пользователями и их балансами на ASP.NET Core 10.0.

## Запуск

```bash
docker compose up --build
```

Поднимает Postgres, Kafka, API (`http://localhost:8080`) и Web UI (`http://localhost:5000`).

Только инфраструктура (без API/Web):

```bash
docker compose up postgres zookeeper kafka -d
dotnet run --project Finbridge.Api/Finbridge.Api.csproj
dotnet run --project Finbridge.Web/Finbridge.Web.csproj
```

## Тесты

```bash
dotnet test Finbridge.slnx
```

## API

| Метод | Путь | Описание | Авторизация |
|-------|------|----------|-------------|
| `POST` | `/api/auth/token` | Получить JWT (dev) | — |
| `POST` | `/api/users` | Создать пользователя | Bearer |
| `GET`  | `/api/users/{id}` | Получить пользователя | Bearer |
| `GET`  | `/api/users` | Список пользователей | Bearer |
| `POST` | `/api/balances` | Обновить баланс | Bearer |
| `POST` | `/api/balances/batch` | Пакетное обновление | Bearer |
| `GET`  | `/api/balances/history/{userId}` | История баланса (последние 20) | Bearer |
| `GET`  | `/health/live` | Liveness (всегда 200 если процесс жив) | — |
| `GET`  | `/health/ready` | Readiness (Postgres + Kafka + outbox) | — |

Swagger: `http://localhost:8080/swagger` (кнопка "Authorize" принимает токен из `/api/auth/token`).

## Архитектура

```
Finbridge.Domain      агрегаты, VO, доменные события, исключения
Finbridge.Application CQRS-обработчики (Commands/Queries), контракты, dispatcher
Finbridge.Data        EF Core (Npgsql), репозитории, outbox-таблица, optimistic locking
Finbridge.Api         REST-контроллеры, Kafka-продюсер, outbox-relay, resilience, OTel
Finbridge.Web         MVC-клиент
Finbridge.Tests       xUnit
```

Изменения баланса валидируются агрегатом (`неотрицательный`, `не выше MaxBalance`),
сохраняются с optimistic concurrency (`User.Version` + retry до 3 раз),
и атомарно пишутся в outbox-таблицу в той же транзакции.
`OutboxRelayService` (BackgroundService) раз в 5с достаёт необработанные сообщения
и публикует в Kafka-топик `users.events` через pipeline
`retry → circuit breaker → rate limiter` (до 10 ретраев).

CQRS: контроллеры вызывают `IRequestDispatcher` → конкретный `IRequestHandler<,>`.

## Телеметрия

OpenTelemetry: распределённые трейсы (HTTP, EF, ASP.NET Core, outbox-poll, outbox-publish)
и метрики (ASP.NET Core, HttpClient, runtime).

Кастомные `ActivitySource`/`Meter` `Finbridge.Outbox`:
- `outbox.published` / `outbox.failed` — счётчики публикаций
- `outbox.publish.duration` — гистограмма длительности publish
- `outbox.pending` — gauge, количество необработанных сообщений

Экспорт OTLP опционален. Задайте `OpenTelemetry__OtlpEndpoint=http://otel-collector:4317` —
иначе инструментация работает локально, без отправки.

## Конфигурация

`Finbridge.Api/appsettings.json`:

```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=postgres;Port=5432;Database=finbridge;Username=postgres;Password=postgres" },
  "BalanceSettings":   { "MaxBalance": 1000000 },
  "KafkaSettings":     { "BootstrapServers": "kafka:9092", "Topic": "users.events" },
  "KafkaResilience":   {
    "Retry":          { "MaxAttempts": 3, "BaseDelayMs": 200 },
    "CircuitBreaker": { "FailureRatio": 0.5, "MinimumThroughput": 5, "SamplingDurationSec": 30, "BreakDurationSec": 15 },
    "RateLimiter":    { "PermitLimit": 100, "WindowSec": 1, "SegmentsPerWindow": 10, "QueueLimit": 50 }
  },
  "JwtSettings":       { "Issuer": "finbridge", "Audience": "finbridge", "SigningKey": "<set-via-env>", "ExpirationHours": 24 },
  "OpenTelemetry":     { "OtlpEndpoint": "" }
}
```

`Finbridge.Web/appsettings.json`:

```json
{ "ApiSettings": { "BaseUrl": "http://api:8080" } }
```

Переопределение через ENV (приоритет над `appsettings.json`):

| ENV | Назначение |
|-----|------------|
| `ConnectionStrings__DefaultConnection` | строка подключения к Postgres |
| `BalanceSettings__MaxBalance` | лимит баланса |
| `KafkaSettings__BootstrapServers` / `KafkaSettings__Topic` | адрес и топик Kafka |
| `KafkaResilience__*` | параметры resilience pipeline (см. `KafkaResilienceOptions`) |
| `JwtSettings__SigningKey` | **обязателен в проде** (≥32 символа для HS256); в репо только dev-значение |
| `OpenTelemetry__OtlpEndpoint` | URL OTLP-коллектора; пусто — экспорт выключен |
| `ApiSettings__BaseUrl` | URL API для Web-клиента |

## База данных

Используется `db.Database.Migrate()` при старте API — миграции лежат в `Finbridge.Data/Migrations/`.
Для greenfield-развёртывания миграции накатываются автоматически; при изменении сущностей добавьте
новую миграцию (`dotnet ef migrations add <Name> --project Finbridge.Data --startup-project Finbridge.Api`)
и пересоберите образ.

## Контакты

Telegram: [@null_ref_ex](https://t.me/null_ref_ex)
