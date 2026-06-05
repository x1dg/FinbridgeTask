# Finbridge Task

Веб-сервис для управления пользователями и их балансами на ASP.NET Core 10.0.

## Возможности

- Создание пользователей (ФИО, дата рождения, место рождения)
- Обновление баланса пользователя (с валидацией: неотрицательный, не выше максимума)
- Пакетное обновление балансов нескольких пользователей
- Получение истории изменений баланса (последние 20 записей)
- Публикация событий изменения баланса в Kafka-топик `users.events`
- REST API с обменом через JSON
- PostgreSQL с **оптимистичной блокировкой** (concurrency token) и **повтором при конфликте**
- MVC-веб-интерфейс (Bootstrap) для просмотра пользователей и истории
- Глобальный middleware обработки исключений (маппит доменные ошибки в HTTP-коды)
- Rate limiting (фиксированное окно: 10 запр. / 10 сек., очередь 5)
- Документация Swagger/OpenAPI
- xUnit-тесты для ключевых сервисов
- Docker: `docker compose up` поднимает Postgres + Kafka + API + Web
- CI на GitHub Actions (сборка + тесты + сборка Docker-образов)

## Требования

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/) (или через Docker)
- [Apache Kafka](https://kafka.apache.org/) (или через Docker)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — рекомендуется для запуска одной командой

## Быстрый старт (Docker)

```bash
docker compose up --build
```

Поднимает:
- **Postgres** на `localhost:5432` (БД `finbridge`, пользователь/пароль `postgres/postgres`)
- **Zookeeper** на `localhost:2181`
- **Kafka** на `localhost:29092` (внутри сети `kafka:9092`)
- **Finbridge.Api** на `http://localhost:8080` (Swagger UI: `http://localhost:8080/swagger`)
- **Finbridge.Web** на `http://localhost:5000`

Схема БД создаётся автоматически при первом запуске (`EnsureCreated()`).

Остановить всё:

```bash
docker compose down
```

Удалить ещё и volume с данными Postgres:

```bash
docker compose down -v
```

## Ручной запуск (без Docker)

### 1. Поднять Postgres и Kafka

Любым удобным способом — локально, в WSL или частично через compose:

```bash
docker compose up postgres zookeeper kafka -d
```

### 2. Настроить конфигурацию

Дефолтный `Finbridge.Api/appsettings.json` указывает на `localhost:5432` и `localhost:9092`.
Переопределить что-либо можно через переменные окружения (синтаксис с двойным подчёркиванием):

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=finbridge;Username=postgres;Password=postgres"
export KafkaSettings__BootstrapServers="localhost:9092"
```

### 3. Запустить API

```bash
dotnet run --project Finbridge.Api/Finbridge.Api.csproj
```

Swagger UI: `https://localhost:5001/swagger`

### 4. Запустить Web-интерфейс

```bash
dotnet run --project Finbridge.Web/Finbridge.Web.csproj
```

Web UI: `https://localhost:5002` — по умолчанию ходит к API на `https://localhost:5001`
(переопределяется через `ApiSettings__BaseUrl`).

## Конфигурация

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

## Эндпоинты API

### Пользователи
- `POST /api/users` — создать пользователя
- `GET  /api/users/{id}` — получить пользователя по ID
- `GET  /api/users` — получить список пользователей

### Балансы
- `POST /api/balances` — обновить баланс одного пользователя
- `POST /api/balances/batch` — пакетное обновление балансов
- `GET  /api/balances/history/{userId}` — история изменений баланса (последние 20)

Все ответы — JSON.

## Архитектура

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

### Проекты

- **`Finbridge.Core`** — доменные модели: `User`, `BalanceHistory` (POCO, без зависимостей).
- **`Finbridge.Data`** — `FinbridgeDbContext` (EF Core), `User.Version` как concurrency token.
- **`Finbridge.Api`** — REST-контроллеры, `UserService` / `BalanceService`, `KafkaProducer`,
  `ExceptionHandlingMiddleware`, rate limiting, Swagger.
- **`Finbridge.Web`** — ASP.NET Core MVC-клиент с `ApiService` (типизированный `HttpClient`).
- **`Finbridge.Tests`** — xUnit-тесты для `UserService` (InMemory EF Core).

### Модель конкурентности

`User.Version` — это `uint` row version, помеченный как concurrency token в EF Core.
`BalanceService.UpdateBalance` повторяет транзакцию до 3 раз при
`DbUpdateConcurrencyException`, так что два одновременных обновления одного
пользователя не перетрут друг друга тихо — проигравший перечитает данные и повторит.

### События Kafka

При каждом успешном изменении баланса `KafkaProducer` публикует JSON-сообщение
в топик `users.events`:

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

Внутри Docker API ходит к брокеру по `kafka:9092`, снаружи — по `localhost:9092`.

### Обработка ошибок

`ExceptionHandlingMiddleware` маппит:

| Исключение                 | HTTP |
|----------------------------|------|
| `KeyNotFoundException`     | 404  |
| `InvalidOperationException`| 400  |
| `ArgumentException`        | 400  |
| всё остальное              | 500  |

### Rate limiting

`AddRateLimiter` — фиксированное окно: 10 запросов в 10 секунд, в очереди до 5
лишних, обработка от старых к новым. Применяется глобально через `app.UseRateLimiter()`.

### Современные фичи C# в коде

В проекте используется ряд языковых возможностей .NET 7–10 (примеры лежат
в `Finbridge.Api/Features/`):

- Raw string literals и UTF-8 string literals
- Сопоставление с образцом (`is { }` / `is not null`)
- Init-only сеттеры и `required` члены
- Records и выражения `with`
- File-scoped namespaces
- Target-typed `new()`
- `IAsyncEnumerable<T>` (async streams)
- Улучшения лямбд (natural type, атрибуты)
- Inline arrays (struct-буферы)
- Ковариантные возвращаемые типы

## Запуск тестов

```bash
dotnet test Finbridge.slnx
```

## CI

GitHub Actions workflow в `.github/workflows/ci.yml`:

1. `build-and-test` — restore, сборка Release, прогон xUnit на `ubuntu-latest`
2. `docker-build` — сборка обоих Docker-образов (после прохождения тестов)
3. `docker-compose-validate` — `docker compose config` для отлова синтаксических ошибок

## Заметки по реализации

- Пакетные обновления балансов выполняются в транзакции ради консистентности.
- После каждого обновления баланса в Kafka уходит событие с актуальным состоянием пользователя.
- Баланс валидируется: не может быть отрицательным и не может превышать настроенный максимум.
- История балансов хранится в БД и доступна через API.
- `EnsureCreated()` используется для удобства первого запуска; в проде стоит перейти на EF Core-миграции.

## Лицензия

Проект в учебных целях.
