# FinTrack API

A personal finance REST API built with ASP.NET Core 10, EF Core, PostgreSQL and JWT auth.

This project is built **step by step** — each step introduces new patterns and concepts on top of the previous one.

---

## Step 1 — Minimal REST API (`/FinTrack`)

Single-project API. The focus is on getting a working, tested REST API with auth and a real database.

### Tech Stack
| Concern | Technology |
|---|---|
| Runtime | .NET 10, ASP.NET Core 10 |
| Database | PostgreSQL (EF Core 10) |
| Auth | JWT (HS256) + BCrypt passwords |
| Docs | Swagger UI |
| Tests | xUnit + FluentAssertions |

### Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | /api/auth/register | ✅ | Create account, returns JWT |
| POST | /api/auth/login | ✅ | Login, returns JWT |
| GET | /api/wallets | ✅ | List your wallets |
| GET | /api/wallets/{id} | ✅ | Get one wallet |
| POST | /api/wallets | ✅ | Create wallet |
| PUT | /api/wallets/{id} | ✅ | Rename wallet |
| DELETE | /api/wallets/{id} | ✅ | Delete wallet |

### Running Locally
```bash
cd FinTrack
docker compose up -d
dotnet run --project src/FinTrack.Api
# API: http://localhost:5168/docs
```

### Tests
```bash
dotnet test tests/FinTrack.Tests
# 19 tests, no Docker required
```

---

## Step 2 — Clean Architecture (`/FinTrackV2`)

Same endpoints, rebuilt with 4-layer Clean Architecture, CQRS, and the Result pattern.

### New Patterns
| Pattern | What it does |
|---|---|
| CQRS + MediatR | Every operation is an explicit Command or Query |
| Result pattern | Handlers return `Result<T>` instead of throwing exceptions |
| Repository pattern | Handlers never touch EF Core directly |
| FluentValidation pipeline | Validation runs before every handler automatically |

### Running Locally
```bash
cd FinTrackV2
docker compose up -d
dotnet run --project src/FinTrack.Api
# API: http://localhost:5153/docs
```

### Tests
```bash
dotnet test tests/FinTrack.Tests
# 17 tests — handlers tested directly, no HTTP context needed
```

---

## Step 3 — Full Feature Set (`/FinTrackV2`, extended)

Adds transactions, domain events, audit log, and integration tests.

### New Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/wallets/{id}/transactions | ✅ | List transactions for a wallet |
| POST | /api/wallets/{id}/transactions | ✅ | Deposit or withdraw |

### New Patterns
| Pattern | What it does |
|---|---|
| Domain events | Entities raise events — handlers react without coupling |
| AggregateRoot | Base class that collects domain events |
| Business rules on entities | `Wallet.Deposit()` and `Wallet.Withdraw()` enforce rules atomically |
| Audit log | Every domain event persisted to `AuditLogs` table as JSON |
| TestContainers | Integration tests spin up a real PostgreSQL container |

### Tests
```bash
dotnet test tests/FinTrack.Tests
# 22 tests: 18 unit + 4 integration (real PostgreSQL via TestContainers)
```

---

## Step 4 — Resilience (`/FinTrackV2`, extended)

Adds structured logging, rate limiting, and pagination.

### New Patterns
| Pattern | What it does |
|---|---|
| Serilog | Structured logging to console + rolling daily file |
| Rate limiting | Auth: 5 req/min. API: 60 req/min. Returns 429 JSON |
| Pagination | `GET /transactions` returns `PagedResult<T>` with metadata |

### Running Locally
```bash
cd FinTrackV2
docker compose up -d
dotnet run --project src/FinTrack.Api
# API:  http://localhost:5153/docs
# Logs: src/FinTrack.Api/logs/
```

### Tests
```bash
dotnet test tests/FinTrack.Tests
# 22 tests passing
```

---

## Step 5 — Events (`/FinTrackV2`, extended)

Adds the Outbox pattern for reliable event delivery, a background job processor,
in-app notifications, and an email service abstraction.

### The Problem Step 5 Solves
In Step 4, domain events are dispatched in-process immediately after `SaveChanges`.
If the process crashes between the database commit and the dispatch, the event is
silently lost. Step 5 fixes this with the Outbox pattern.

### How the Outbox Pattern Works
```
POST /api/wallets/{id}/transactions
  → CreateTransactionHandler
    → wallet.Deposit(amount)
    → Single DB transaction:
        INSERT INTO Transactions ...
        UPDATE Wallets SET Balance = ...
        INSERT INTO OutboxMessages (Type, Payload)   ← event stored atomically
    → HTTP 201 returned immediately

  [5 seconds later — background]
  OutboxProcessor (BackgroundService)
    → SELECT * FROM OutboxMessages WHERE ProcessedAt IS NULL
    → Deserialise JSON payload → MediatR.Publish()
        → TransactionCreatedAuditHandler   → writes AuditLog
        → TransactionNotificationHandler   → creates Notification + sends email
    → UPDATE OutboxMessages SET ProcessedAt = NOW()
```

### New Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/notifications | ✅ | List notifications (newest first) |
| PATCH | /api/notifications/{id}/read | ✅ | Mark notification as read |

### New Patterns
| Pattern | What it does |
|---|---|
| Outbox pattern | Events written atomically with business data — never silently lost |
| BackgroundService | `OutboxProcessor` polls DB every 5s, dispatches via MediatR |
| IEmailService | Abstraction over email providers — `ConsoleEmailService` in dev |
| Notifications | In-app notification feed per user |

### Running Locally
```bash
cd FinTrackV2
docker compose up -d
dotnet run --project src/FinTrack.Api
# OutboxProcessor starts automatically
# [EMAIL STUB] lines appear in console when events are dispatched
```

### Tests
```bash
dotnet test tests/FinTrack.Tests
# 31 tests: 27 unit + 4 integration
```

---

## Roadmap

- [x] Step 1 — Minimal REST API
- [x] Step 2 — Clean Architecture (CQRS, MediatR, Result pattern, FluentValidation)
- [x] Step 3 — Full Feature Set (domain events, transactions, audit log, TestContainers)
- [x] Step 4 — Resilience (Serilog structured logging, rate limiting, pagination)
- [x] Step 5 — Events (Outbox pattern, BackgroundService, notifications, email stub)
- [ ] Step 6 — Microservices (gRPC, RabbitMQ, MassTransit, API Gateway)
