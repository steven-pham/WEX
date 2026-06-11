# WEX Cards API

Take-home assessment — Card payment API in .NET 10.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Quick Start

```bash
# 1. Create the environment file (docker compose reads it via env_file)
cp .env.example .env

# 2. Start the database
docker compose up -d db

# 3. Run the API
cd src/Wex.Cards.Api
dotnet run
```

The API will be available at `http://localhost:5112` (or as shown in terminal output).
Swagger UI: `http://localhost:5112/swagger`
Health check: `http://localhost:5112/health`

## Running Tests

```bash
# Unit tests (no external dependencies)
dotnet test tests/Wex.Cards.UnitTests

# Integration tests (spins up Postgres automatically via Testcontainers — Docker required)
dotnet test tests/Wex.Cards.IntegrationTests
```

## Applying Migrations

```bash
cd src/Wex.Cards.Api
dotnet ef database update --project ../Wex.Cards.Infrastructure
```

## Assumptions

> These are documented for the reviewer and represent decisions made to move forward
> given the assessment timeframe. They are intentionally called out as questions.

1. **Native / base currency is USD.** Stored amounts (credit limit, transaction amounts)
   are assumed to be in USD. The Treasury Reporting Rates of Exchange API quotes units
   of foreign currency per 1 USD, so conversion is `amount_usd × rate`.

2. **Currency input format is ISO 4217 code** (e.g., `EUR`, `JPY`). The API accepts the
   three-letter ISO code and maps it internally to the Treasury dataset's
   `country_currency_desc` lookup key.

3. **`GET /transactions/{id}?currency=XXX`** serves both Requirement #2 (no param →
   original amount) and Requirement #3 (with `currency` param → converted amount) from
   a single endpoint.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/cards` | Create a card with a credit limit |
| `GET` | `/cards/{id}` | Retrieve a card |
| `GET` | `/cards/{id}/balance` | Available balance (optionally in a target currency) |
| `POST` | `/cards/{cardId}/transactions` | Store a purchase transaction |
| `GET` | `/transactions/{id}` | Retrieve a transaction (optionally converted to a target currency) |
| `GET` | `/health` | Health check |

## Project Structure

```
src/
  Wex.Cards.Api/           — ASP.NET Core Web API (endpoints, DTOs, DI wiring)
  Wex.Cards.Application/   — Use-case services and ports (interfaces)
  Wex.Cards.Domain/        — Entities, value objects, domain rules
  Wex.Cards.Infrastructure/— EF Core DbContext + repositories + migrations, Treasury HTTP client
tests/
  Wex.Cards.UnitTests/      — Pure unit tests (no I/O)
  Wex.Cards.IntegrationTests/— Integration tests using WebApplicationFactory + Testcontainers
```
