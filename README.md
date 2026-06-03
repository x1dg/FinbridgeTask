# Finbridge Task

A web service for managing users and their balances built with ASP.NET Core 10.0.

## Features

- Create users with Full Name, Date of Birth, Place of Birth
- Update user balance (with validation: non-negative and not exceeding max balance)
- Batch update balances for multiple users
- Retrieve user balance history (last 20 changes)
- Send user balance update events to Apache Kafka topic "users.events"
- REST API with JSON exchange format
- PostgreSQL database
- Swagger/OpenAPI documentation
- Unit tests for core services

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/) (or use Docker)
- [Apache Kafka](https://kafka.apache.org/) (or use Docker for local development)

## Configuration

Update `Finbridge.Api/appsettings.json` with your PostgreSQL and Kafka settings:

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

## Running the Application

1. Ensure PostgreSQL is running and the database `finbridge` exists (or update connection string).
2. Ensure Kafka is running and the topic `users.events` exists (or update bootstrap servers and topic).
3. Run the API:

```bash
dotnet run --project Finbridge.Api/Finbridge.Api.csproj
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000` if HTTPS is not configured).

Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### Users
- `POST /api/users` - Create a new user
- `GET /api/users/{id}` - Get user by ID
- `GET /api/users` - Get all users

### Balances
- `POST /api/balances` - Update balance for a single user
- `POST /api/balances/batch` - Update balances for multiple users
- `GET /api/balances/history/{userId}` - Get balance history for a user (last 20 changes)

## Running Tests

```bash
dotnet test Finbridge.Tests/Finbridge.Tests.csproj
```

## Project Structure

- `Finbridge.Api` - ASP.NET Core Web API project
- `Finbridge.Core` - Contains core models (User, BalanceHistory)
- `Finbridge.Data` - Data access layer with Entity Framework Core
- `Finbridge.Tests` - Unit tests using xUnit

## Implementation Notes

- Balance updates are wrapped in a transaction for batch operations to ensure consistency.
- After each balance update, an event is published to Kafka with the current user state.
- The service validates that balance cannot be negative and cannot exceed the configured maximum.
- Balance history is stored in the database and can be retrieved via the API.

## License

This project is for educational purposes.