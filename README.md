# WEX Cards API

Take-home assessment: a card payment API in .NET 10.

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

# Integration tests (spins up Postgres automatically via Testcontainers; Docker required)
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

`USD` is the base currency; a rate of `1.0` is returned without an API call.

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

**Retrieve a transaction (original amount)**
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

1. **Base currency assumption.** Stored amounts (credit limit, transaction amounts) are
   treated as USD. The Treasury API quotes units of foreign currency per 1 USD, so
   conversion is `amount_usd × rate`. Happy to revisit if a different base currency
   is expected.

2. **Combined vs separate endpoint.** `GET /transactions/{id}?currency=XXX` serves both
   Requirement #2 (no `currency` param → original amount in USD) and Requirement #3
   (with `currency` param → converted amount) from a single endpoint. A separate endpoint
   could make the contract more explicit; the current design favours a smaller surface.

3. **Unsupported currency behaviour.** An ISO 4217 code not in the curated map (e.g.
   `THB`) returns `422 Unprocessable Entity` with a ProblemDetails body rather than
   `400 Bad Request`. Open to adjusting if a different status code is preferred.

## Future Improvements

The following are improvements that would be prioritised in a production system:

1. **Enforce balance on transaction creation.** `TransactionService.AddAsync` currently accepts any
   positive amount without checking the card's available balance. A balance check (credit limit minus
   total spent) should happen inside a database transaction with a row-level lock on the card row,
   so two concurrent purchases cannot both succeed when only one fits within the remaining balance.

2. **Idempotency keys.** Payment APIs need protection against duplicate submissions (network retries,
   double-clicks). Adding an `Idempotency-Key` header on `POST /cards/{cardId}/transactions` and
   persisting the key alongside the transaction would let the API return the original response for
   replayed requests rather than creating duplicates.

3. **Transaction reversal / refunds.** There is currently no way to void or refund a transaction.
   A `POST /transactions/{id}/reversal` endpoint (creating a negative-amount transaction in the same
   domain model) would restore the balance and keep a full audit trail without deleting data.

4. **Authentication & authorisation.** All endpoints are open. In production, cards and transactions
   would be scoped to an authenticated account (e.g. JWT bearer), and a card owner should not be
   able to read or transact against another owner's card.

5. **Exchange rate caching.** Treasury rates are published quarterly, not in real time. Every
   balance or transaction query currently hits the external API. A short-lived in-memory or
   distributed cache (e.g. `IMemoryCache` with a 1-hour TTL) would eliminate the latency and
   reduce the risk of the circuit breaker opening under load.

6. **Pagination on transaction history.** `GET /cards/{id}/transactions` does not exist yet, and
   when it does, returning unbounded rows would be unsafe. Standard cursor- or offset-based
   pagination (e.g. `?page=1&pageSize=20`) should be built in from the start.

7. **Domain events & outbox pattern.** Side-effects like sending a spend-alert notification or
   updating a downstream ledger should not happen inside the same database transaction as the write.
   Raising domain events (e.g. `TransactionCreated`) and publishing them via an outbox table
   decouples these concerns and guarantees at-least-once delivery even if the process crashes
   mid-request.

8. **Card lifecycle management.** Cards have no status field. Production cards need states
   (active, frozen, cancelled) so that transactions can be rejected against a frozen or cancelled
   card without deleting any data.

## Project Structure

```
src/
  Wex.Cards.Api/            ASP.NET Core Web API (endpoints, DTOs, DI wiring)
  Wex.Cards.Application/    Use-case services and ports (interfaces)
  Wex.Cards.Domain/         Entities, value objects, domain rules
  Wex.Cards.Infrastructure/ EF Core DbContext + repositories + migrations, Treasury HTTP client
tests/
  Wex.Cards.UnitTests/        Pure unit tests (no I/O)
  Wex.Cards.IntegrationTests/ Integration tests using WebApplicationFactory + Testcontainers
```

## Design Notes & Trade-offs

These are the deliberate engineering decisions behind the code, along with the alternatives I
weighed. Most are scoped for the assessment but reflect how I'd reason about the same calls in
production.

1. **Layered (ports-and-adapters) architecture.** The Domain and Application projects have no
   dependency on EF Core or `HttpClient`; persistence and the Treasury client sit behind ports
   (`ICardRepository`, `IExchangeRateProvider`) defined in Application and implemented in
   Infrastructure. The trade-off is more projects and a little wiring ceremony for a small app.
   I accepted that because it keeps the business rules independently testable, makes the Treasury
   integration swappable, and stops infrastructure concerns leaking into the core. For a CRUD-only
   brief this would be over-engineered; the currency-conversion requirement is what justifies the
   seam.

2. **Errors separate business failures from infrastructure failures.** A currency that has no
   qualifying rate within six months returns `422 Unprocessable Entity`: the request is
   well-formed but cannot be satisfied, and the client can act on it (pick another currency). An
   upstream Treasury outage surfaces as `503 Service Unavailable`: our dependency failed, the
   request was fine, and a retry may succeed. Collapsing both into one status is the easy mistake:
   it tells a caller to "fix your request" when the real answer is "try again later", and it
   pollutes alerting. A single `GlobalExceptionHandler` maps domain exceptions to status codes so
   this distinction is enforced in one place, and an integration test pins it
   (`GetTransaction_ProviderFailure_Returns5xxNotUnprocessableEntity`).

3. **Two rounding modes, on purpose.** Input validation uses `MidpointRounding.AwayFromZero`;
   there it's only a precision guard (reject a credit limit with more than 2 decimal places, an
   amount with more than 4), so the rounding direction is immaterial. Converted monetary amounts
   use `MidpointRounding.ToEven` (banker's rounding), which is the financial-reporting standard:
   it avoids the systematic upward bias that always-round-half-up introduces across many
   conversions. The modes are intentionally different, not an oversight. I'm flagging it here so
   it isn't "tidied up" into a single mode later.

4. **`Money` and `CurrencyCode` as value objects.** Amounts are modelled as `Money(amount,
   currency)` even though every stored value is USD today. The aim is to make the base-currency
   assumption explicit in the type system rather than leaving bare `decimal`s to mean "USD by
   convention", and to leave a seam for true multi-currency storage later. The honest trade-off:
   right now this is slightly richer than it needs to be: conversion results are returned as
   `decimal`, not `Money`. I'd rather have the assumption named in one type than implied across
   the codebase, but it's a conscious lean toward the likely next step.

5. **Curated ISO 4217 → Treasury mapping.** The Treasury dataset keys on `country_currency_desc`
   (e.g. `Euro Zone-Euro`) and has no ISO field, so an explicit dictionary is the authoritative
   list of supported currencies. The trade-off is manual maintenance versus deriving the set
   dynamically. I chose the explicit map because it's cheap, documented, and lets the API reject
   an unsupported code without a wasted network call, at the cost of having to add new currencies
   by hand.

6. **Testcontainers over EF Core InMemory.** Integration tests run against a real Postgres
   container so they exercise actual SQL translation (e.g. the `SumAsync` balance query), the real
   migration, and genuine `decimal` semantics. InMemory would run faster but gives false
   confidence: it silently passes on things that break against a real provider. For
   currency-conversion tests the `IExchangeRateProvider` port is substituted so the cases are
   deterministic and don't depend on the live API, and a single live test guards the real Treasury
   contract separately.

7. **Validation layered intentionally.** FluentValidation at the API edge handles shape and format
   and returns `400` with field-level errors; the domain factory methods (`Card.Create`,
   `Transaction.Create`) enforce the real invariants and are the source of truth, so an entity can
   never be constructed in an invalid state regardless of caller. The light guards in the service
   layer are defence in depth against a caller that bypasses the edge validators. The duplication
   is deliberate: the domain stays authoritative rather than trusting the boundary.
