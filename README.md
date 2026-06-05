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

| Метод | Путь | Описание |
|-------|------|----------|
| `POST` | `/api/users` | Создать пользователя |
| `GET`  | `/api/users/{id}` | Получить пользователя |
| `GET`  | `/api/users` | Список пользователей |
| `POST` | `/api/balances` | Обновить баланс |
| `POST` | `/api/balances/batch` | Пакетное обновление |
| `GET`  | `/api/balances/history/{userId}` | История баланса (последние 20) |
| `GET`  | `/health/live` | Liveness (всегда 200 если процесс жив) |
| `GET`  | `/health/ready` | Readiness (Postgres + Kafka + outbox) |

Swagger: `http://localhost:8080/swagger`.

## Архитектура

```
Finbridge.Domain      агрегаты, VO, доменные события, исключения
Finbridge.Application сервисы, контракты, диспетчер доменных событий
Finbridge.Data        EF Core (Npgsql), репозитории, optimistic locking
Finbridge.Api         REST-контроллеры, Kafka-продюсер, resilience pipeline
Finbridge.Web         MVC-клиент
Finbridge.Tests       xUnit
```

Изменения баланса валидируются агрегатом (`неотрицательный`, `не выше MaxBalance`),
сохраняются с optimistic concurrency (`User.Version` + retry до 3 раз),
и публикуются в Kafka-топик `users.events` через pipeline
`retry → circuit breaker → rate limiter`.

## Конфигурация

`Finbridge.Api/appsettings.json`:

```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=localhost;Port=5432;Database=finbridge;Username=postgres;Password=postgres" },
  "BalanceSettings":   { "MaxBalance": 1000000 },
  "KafkaSettings":     { "BootstrapServers": "localhost:9092", "Topic": "users.events" }
}
```

Переопределение через ENV: `ConnectionStrings__DefaultConnection`, `KafkaSettings__BootstrapServers` и т.д.

## Контакты

Telegram: [@null_ref_ex](https://t.me/null_ref_ex)
