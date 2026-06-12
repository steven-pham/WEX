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

# 3. Apply database migrations
cd src/Wex.Cards.Api
dotnet ef database update --project ../Wex.Cards.Infrastructure

# 4. Run the API
dotnet run
```

The API will be available at `http://localhost:5112` (or as shown in terminal output).
Swagger UI: `http://localhost:5112/swagger`
Health check: `http://localhost:5112/health`

## Running Tests

Run these from the **repo root** (not from `src/Wex.Cards.Api`):

```bash
# Return to the repo root if you followed the Quick Start steps
cd <repo-root>

# Unit tests (no external dependencies)
dotnet test tests/Wex.Cards.UnitTests

# Integration tests (spins up Postgres automatically via Testcontainers — Docker required)
dotnet test tests/Wex.Cards.IntegrationTests
```

## Treasury Exchange Rate Integration

The Treasury Reporting Rates of Exchange client is registered via `IHttpClientFactory` with
`AddStandardResilienceHandler()`. The following resilience defaults are in effect (no overrides):

| Policy | Default |
|---|---|
| Total request timeout | 30 s |
| Per-attempt timeout | 10 s |
| Retry | 3 retries, exponential-with-jitter backoff, base delay 2 s |
| Circuit breaker | Opens at 10 % failure ratio over a 30 s sampling window (min 100 requests); break duration 5 s |

The base URL is configurable via `TreasuryApi:BaseUrl` in `appsettings.json` (or the
`TreasuryApi__BaseUrl` environment variable).

### Supported currencies

The following ISO 4217 codes are mapped to the Treasury `country_currency_desc` field.
Any other code returns "no rate available" without calling the API.

| ISO 4217 | Treasury description |
|---|---|
| `AUD` | Australia-Dollar |
| `BRL` | Brazil-Real |
| `CAD` | Canada-Dollar |
| `CNY` | China-Renminbi |
| `DKK` | Denmark-Krone |
| `EUR` | Euro Zone-Euro |
| `GBP` | United Kingdom-Pound |
| `HKD` | Hong Kong-Dollar |
| `INR` | India-Rupee |
| `JPY` | Japan-Yen |
| `KRW` | Korea-Won |
| `MXN` | Mexico-Peso |
| `NOK` | Norway-Krone |
| `NZD` | New Zealand-Dollar |
| `SEK` | Sweden-Krona |
| `SGD` | Singapore-Dollar |
| `CHF` | Switzerland-Franc |
| `ZAR` | South Africa-Rand |

`USD` is the base currency — a rate of `1.0` is returned without an API call.

## Assumptions

> The following decisions were made to keep scope within the assessment timeframe.
> Each one is a candidate for discussion.

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

### API Examples

Replace `<CARD_ID>` and `<TX_ID>` with the UUIDs returned from the create calls.

**Create a card** (Requirement #1)
```bash
curl -s -X POST http://localhost:5112/cards \
  -H "Content-Type: application/json" \
  -d '{"creditLimit": 1000.00}' | jq
```

**Retrieve a card**
```bash
curl -s http://localhost:5112/cards/<CARD_ID> | jq
```

**Store a purchase transaction** (Requirement #2)
```bash
curl -s -X POST http://localhost:5112/cards/<CARD_ID>/transactions \
  -H "Content-Type: application/json" \
  -d '{"description": "Coffee shop", "transactionDate": "2025-01-15", "amount": 5.75}' | jq
```

**Retrieve a transaction — original amount**
```bash
curl -s http://localhost:5112/transactions/<TX_ID> | jq
```

**Retrieve a transaction in a target currency** (Requirement #3)
```bash
curl -s "http://localhost:5112/transactions/<TX_ID>?currency=EUR" | jq
```

**Available balance in native currency** (Requirement #4)
```bash
curl -s http://localhost:5112/cards/<CARD_ID>/balance | jq
```

**Available balance in a target currency**
```bash
curl -s "http://localhost:5112/cards/<CARD_ID>/balance?currency=GBP" | jq
```

An HTTP requests file covering all scenarios is also available at
[`src/Wex.Cards.Api/Wex.Cards.Api.http`](src/Wex.Cards.Api/Wex.Cards.Api.http)
(open in VS Code REST Client or JetBrains HTTP Client).

## Open Questions

The following points were intentionally left open and are worth discussing in a real world scenario:

1. **Base currency assumption** — stored amounts (credit limit, transaction amounts) are
   treated as USD. The Treasury API quotes units of foreign currency per 1 USD, so
   conversion is `amount_usd × rate`. Happy to revisit if a different base currency
   is expected.

2. **Combined vs separate endpoint** — `GET /transactions/{id}?currency=XXX` serves both
   Requirement #2 (no `currency` param → original amount in USD) and Requirement #3
   (with `currency` param → converted amount) from a single endpoint. A separate endpoint
   could make the contract more explicit; the current design favours a smaller surface.

3. **Unsupported currency behaviour** — an ISO 4217 code not in the curated map (e.g.
   `THB`) returns `422 Unprocessable Entity` with a ProblemDetails body rather than
   `400 Bad Request`. Open to adjusting if a different status code is preferred.

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
