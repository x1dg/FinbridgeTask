# Finbridge

Веб-сервис для управления пользователями и их балансами на **ASP.NET Core 10.0**.
Состоит из REST API и независимой Blazor-админки **Backoffice** с UI на Ant Design Blazor.

## Запуск

```bash
docker compose up --build
```

Поднимает `postgres`, `zookeeper`, `kafka`, `api` (`http://localhost:8080`) и `backoffice` (`http://localhost:5000`).

Только инфраструктура:

```bash
docker compose up postgres zookeeper kafka -d
dotnet run --project Finbridge.Api
dotnet run --project Finbridge.Backoffice
```

Force-rebuild с нуля (если менялся `.csproj` или `Dockerfile`):

```bash
docker compose down
docker compose build --no-cache
docker compose up
```

## Тесты

```bash
dotnet test
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

## Backoffice

Blazor Server на **Ant Design Blazor**. Зависит **только от Postgres** — API может лежать,
админка продолжит работать. Это достигается за счёт того, что Backoffice ходит в БД
через общие с API слой `Finbridge.Data` + `Finbridge.Application` (тот же `IRequestDispatcher`,
та же валидация, те же outbox-механики).

### Страницы

| Путь | Описание |
|------|----------|
| `/` | Список пользователей (таблица + создание через модалку) |
| `/balance-history/{userId}` | История баланса пользователя |
| `/stalker-mods` | Бонусная страница: обзор мода **Priboi Story — Eternal OGSR**, отзывы, фотограф, карусель летсплеев |

### Шапка

В шапке Backoffice — виджет погоды: температура, описание, восход/закат, дни до полнолуния
(рассчитываются по сидерическому месяцу), город. Источник — Open-Meteo (без API-ключа).

## Архитектура

```
Finbridge.Domain       агрегаты, VO, доменные события, исключения
Finbridge.Application  CQRS-обработчики (Commands/Queries), контракты, dispatcher
Finbridge.Data         EF Core (Npgsql), репозитории, outbox-таблица, optimistic locking
Finbridge.Api          REST-контроллеры, JWT, Kafka-продюсер, outbox-relay, resilience, OTel
Finbridge.Backoffice   Blazor Server + Ant Design — UI прямо в БД через общий слой
Finbridge.Tests        xUnit
```

Изменения баланса валидируются агрегатом (`неотрицательный`, `не выше MaxBalance`),
сохраняются с optimistic concurrency (`User.Version` + retry до 3 раз) и атомарно
пишутся в outbox-таблицу в той же транзакции. `OutboxRelayService` (BackgroundService)
раз в 5с достаёт необработанные сообщения и публикует в Kafka-топик `users.events`
через pipeline `retry → circuit breaker → rate limiter` (до 10 ретраев).

CQRS: и API-контроллеры, и Backoffice вызывают `IRequestDispatcher` → конкретный
`IRequestHandler<,>`. Бизнес-правила и валидации живут в `Finbridge.Application` и
исполняются в обоих процессах.

Backoffice **не** ходит в API по HTTP: у него свой `DbContext`, свои инстансы
хендлеров, общая БД. Если API упал — UI остаётся живым.

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

`Finbridge.Backoffice/appsettings.json`:

```json
{ "ConnectionStrings": { "DefaultConnection": "Host=localhost;Port=5432;Database=finbridge;Username=postgres;Password=postgres" } }
```

Переопределение через ENV (приоритет над `appsettings.json`):

| ENV | Назначение |
|-----|------------|
| `ConnectionStrings__DefaultConnection` | строка подключения к Postgres (для API и для Backoffice) |
| `BalanceSettings__MaxBalance` | лимит баланса |
| `KafkaSettings__BootstrapServers` / `KafkaSettings__Topic` | адрес и топик Kafka |
| `KafkaResilience__*` | параметры resilience pipeline (см. `KafkaResilienceOptions`) |
| `JwtSettings__SigningKey` | **обязателен в проде** (≥32 символа для HS256); в репо только dev-значение |
| `OpenTelemetry__OtlpEndpoint` | URL OTLP-коллектора; пусто — экспорт выключен |

В `docker-compose.yml` уже выставлены: `ConnectionStrings__DefaultConnection`,
`BalanceSettings__MaxBalance`, `KafkaSettings__BootstrapServers`, `KafkaSettings__Topic`.

## База данных

`db.Database.Migrate()` при старте API — миграции лежат в `Finbridge.Data/Migrations/`.
Для greenfield-развёртывания миграции накатываются автоматически; при изменении сущностей
добавьте новую миграцию:

```bash
dotnet ef migrations add <Name> --project Finbridge.Data --startup-project Finbridge.Api
```

и пересоберите образ.

> **Важно:** миграции накатывает только API. Backoffice стартует с тем же `DbContext`,
> но без `Migrate()`. Это нормально — схема уже актуальна после того, как API поднялся.

## Контакты

Telegram: [@null_ref_ex](https://t.me/null_ref_ex)
