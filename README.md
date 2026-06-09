# Finbridge

Веб-сервис для управления пользователями и их балансами на **ASP.NET Core 10.0**.
Состоит из REST API и независимой Blazor-админки **Backoffice** с UI на Ant Design Blazor.

## Сервисы после запуска

| Сервис | Адрес | Описание |
|--------|-------|----------|
| **Backoffice** (админка) | `http://localhost:5000` | Blazor Server UI: управление пользователями, балансами, темы |
| **API** | `http://localhost:8080` | REST API (CRUD пользователей, балансы, Kafka, outbox) |
| **Swagger** | `http://localhost:8080/swagger` | Интерактивная документация API (кнопка "Authorize" для JWT) |
| **Health (live)** | `http://localhost:8080/health/live` | Liveness —活着 если процесс жив |
| **Health (ready)** | `http://localhost:8080/health/ready` | Readiness — Postgres + Kafka + outbox |
| **PostgreSQL** | `localhost:5432` | БД: `finbridge`, пользователь `postgres` |
| **Kafka** | `localhost:29092` | Топик `users.events` (Plaintext) |
| **Zookeeper** | `localhost:2181` | Zookeeper для Kafka |

## Запуск

```bash
docker compose up --build
```

Поднимает все 5 сервисов: `postgres`, `zookeeper`, `kafka`, `api`, `backoffice`.

Только инфраструктура (для локальной разработки):

```bash
docker compose up postgres zookeeper kafka -d
dotnet run --project Finbridge.Api
dotnet run --project Finbridge.Backoffice
```

Force-rebuild с нуля (если менялся `.csproj` или `Dockerfile`):

```bash
docker compose down -v
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
| `POST` | `/api/balances` | Изменить баланс (+/-) | Bearer |
| `POST` | `/api/balances/batch` | Пакетное обновление | Bearer |
| `GET`  | `/api/balances/history/{userId}?limit=20` | История баланса | Bearer |
| `GET`  | `/health/live` | Liveness | — |
| `GET`  | `/health/ready` | Readiness (Postgres + Kafka + outbox) | — |

## Backoffice (админка)

Blazor Server на **Ant Design Blazor**. Зависит **только от Postgres** — API может лежать,
админка продолжит работать. Backoffice ходит в БД через общий с API слой `Finbridge.Data` +
`Finbridge.Application` (тот же `IRequestDispatcher`, та же валидация).

### Страницы

| Путь | Описание |
|------|----------|
| `/` | Список пользователей: таблица, создание (автокомплит городов), изменение баланса |
| `/balance-history/{userId}` | История баланса пользователя (Tag: зелёный/красный) |
| `/stalker-mods` | Обзор мода **Priboi Story — Eternal OGSR**, отзывы, YouTube-летсплеи |

### Функционал

- **Создание пользователя** — ФИО, дата рождения (01.01.1999 по умолчанию), город (автокомплит 60 городов России или ручной ввод)
- **Изменение баланса** — модалка с валидацией: нельзя опустить ниже 0, нельзя превысить 1 000 000
- **История баланса** — таблица с изменениями, теги зелёный/красный
- **Темы** — 3 светлые темы (Лавандовая по умолчанию, Арктическая, Белая), выбор в шапке, сохраняется в localStorage

### Шапка

В шапке Backoffice — виджет погоды (температура, описание, восход/закат, дни до полнолуния)
и переключатель тем. Источник погоды — Open-Meteo (без API-ключа).

## Архитектура

```
Finbridge.Domain       агрегаты, VO, доменные события, исключения
Finbridge.Application  CQRS-обработчики (Commands/Queries), контракты, dispatcher
Finbridge.Data         EF Core (Npgsql), репозитории, outbox-таблица, optimistic locking
Finbridge.Api          REST-контроллеры, JWT, Kafka-продюсер, outbox-relay, resilience, OTel
Finbridge.Backoffice   Blazor Server + Ant Design — UI прямо в БД через общий слой
Finbridge.Tests        xUnit
```

### Бизнес-правила баланса

- Баланс **неотрицательный** — операция, ведущая к минусу, отвергается (`NegativeBalanceException`)
- Баланс **не выше MaxBalance** (1 000 000) — превышение отвергается (`BalanceLimitExceededException`)
- Оптимистичная конкурентность: `User.Version` + retry до 3 раз при `ConcurrencyConflictException` (HTTP 409)
- Outbox: атомарная запись в `outbox_messages` в той же транзакции
- OutboxRelay: `FOR UPDATE SKIP LOCKED` для multi-instance безопасности, до 10 ретраев, dead-letter после 10 ошибок

### Kafka pipeline

`retry → circuit breaker → rate limiter` для продюсера. OutboxRelay раз в 5с достаёт
необработанные сообщения и публикует в топик `users.events`.

### CQRS

И API-контроллеры, и Backoffice вызывают `IRequestDispatcher` → конкретный
`IRequestHandler<,>`. Бизнес-правила живут в `Finbridge.Application` и
исполняются в обоих процессах.

## Телеметрия

OpenTelemetry: распределённые трейсы (HTTP, EF, ASP.NET Core, outbox) и метрики.
Экспорт OTLP опционален: `OpenTelemetry__OtlpEndpoint=http://otel-collector:4317`.

Кастомные метрики `Finbridge.Outbox`:
- `outbox.published` / `outbox.failed` — счётчики публикаций
- `outbox.publish.duration` — гистограмма длительности
- `outbox.pending` — gauge необработанных сообщений

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

Переопределение через ENV:

| ENV | Назначение |
|-----|------------|
| `ConnectionStrings__DefaultConnection` | строка подключения к Postgres |
| `BalanceSettings__MaxBalance` | лимит баланса |
| `KafkaSettings__BootstrapServers` / `KafkaSettings__Topic` | адрес и топик Kafka |
| `JwtSettings__SigningKey` | **обязателен в проде** (≥32 символа для HS256) |
| `OpenTelemetry__OtlpEndpoint` | URL OTLP-коллектора; пусто = выключен |

## База данных

`db.Database.Migrate()` при старте API — миграции в `Finbridge.Data/Migrations/`.
Новая миграция:

```bash
dotnet ef migrations add <Name> --project Finbridge.Data --startup-project Finbridge.Api
```

> API накатывает миграции при старте. Backoffice стартует с тем же `DbContext`, но без `Migrate()`.

## Контакты

Telegram: [@null_ref_ex](https://t.me/null_ref_ex)
