# FinTrack API

A personal finance REST API built progressively with .NET 10 — each step introduces new patterns and concepts on top of the previous one. The project demonstrates the full journey from a minimal REST API to a production-grade microservices architecture.

---

## Project Structure
```
FinTrack/
├── FinTrack/              ← Step 1: Minimal REST API
├── FinTrackV2/            ← Steps 2–5: Clean Architecture monolith
├── FinTrack.Contracts/    ← Step 6: Shared gRPC + MassTransit contracts
├── FinTrack.Gateway/      ← Step 6: YARP API Gateway (port 5000)
├── FinTrack.IdentityService/  ← Step 6: Auth service (REST:5001, gRPC:5011)
├── FinTrack.WalletService/    ← Step 6: Wallet service (port 5002)
├── FinTrack.NotifyService/    ← Step 6: Notification service (port 5003)
└── README.md
```

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
| POST | /api/auth/register | — | Create account, returns JWT |
| POST | /api/auth/login | — | Login, returns JWT |
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
dotnet test
# 19 tests, no Docker required
```

---

## Step 2 — Clean Architecture (`/FinTrackV2`)

Same endpoints, rebuilt with 4-layer Clean Architecture, CQRS, and the Result pattern.

### New Patterns
| Pattern | What it does |
|---|---|
| Clean Architecture | Domain / Application / Infrastructure / Api — dependencies point inward only |
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
dotnet test
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
| Business rules on entities | `Wallet.Deposit()` and `Wallet.Withdraw()` enforce rules |
| Audit log | Every domain event persisted to `AuditLogs` table as JSON |
| TestContainers | Integration tests spin up a real PostgreSQL Docker container |

### Tests
```bash
dotnet test
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
dotnet test
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
    → Single DB transaction:
        INSERT INTO Transactions ...
        UPDATE Wallets SET Balance = ...
        INSERT INTO OutboxMessages (Type, Payload)   ← event stored atomically
    → HTTP 201 returned immediately

  [5 seconds later — background]
  OutboxProcessor (BackgroundService)
    → SELECT * FROM OutboxMessages WHERE ProcessedAt IS NULL
    → Deserialise JSON payload → MediatR.Publish()
        → AuditHandler           → writes AuditLog
        → NotificationHandler    → creates Notification + sends email stub
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

### Tests
```bash
dotnet test
# 31 tests: 27 unit + 4 integration
```

---

## Step 6 — Microservices

Splits the monolith into three independent services with an API Gateway, gRPC for
internal communication, and RabbitMQ for async messaging.

### Architecture
```
Client
  └─→ Gateway:5000 (YARP)
        ├─→ /api/auth/*          → IdentityService:5001  (REST)
        ├─→ /api/wallets/*       → WalletService:5002    (REST)
        └─→ /api/notifications/* → NotifyService:5003    (REST)

WalletService → IdentityService:5011  (gRPC — user lookup)
WalletService → RabbitMQ             (publish TransactionCreatedMessage)
NotifyService ← RabbitMQ             (consume TransactionCreatedMessage)
```

### Services
| Service | Port | Database | Responsibility |
|---|---|---|---|
| FinTrack.Gateway | 5000 | — | YARP reverse proxy, routes all traffic |
| FinTrack.IdentityService | 5001 (REST) 5011 (gRPC) | identity_db:5433 | Register, login, JWT, gRPC user lookup |
| FinTrack.WalletService | 5002 | wallet_db:5434 | Wallets, transactions, publishes events |
| FinTrack.NotifyService | 5003 | notification_db:5435 | Consumes events, creates notifications |

### New Technologies
| Technology | What it does |
|---|---|
| YARP | Microsoft reverse proxy — single entry point, routes by path prefix |
| gRPC + Protobuf | Binary HTTP/2 protocol for fast internal service calls |
| RabbitMQ | Message broker — guarantees delivery even if consumer is down |
| MassTransit | .NET abstraction over RabbitMQ — consumers, retries, dead-letter |

### Running Locally
```bash
# 1. Start infrastructure (3 databases + RabbitMQ)
docker compose -f docker-compose.microservices.yml up -d

# 2. Start each service in a separate terminal
dotnet run --project FinTrack.IdentityService --launch-profile "FinTrack.IdentityService"
dotnet run --project FinTrack.WalletService   --launch-profile "FinTrack.WalletService"
dotnet run --project FinTrack.NotifyService   --launch-profile "FinTrack.NotifyService"
dotnet run --project FinTrack.Gateway         --launch-profile "FinTrack.Gateway"

# 3. All traffic goes through the gateway
# http://localhost:5000/api/auth/register
# http://localhost:5000/api/wallets
# http://localhost:5000/api/notifications

# RabbitMQ management UI: http://localhost:15672  (guest/guest)
```

### End-to-End Flow
```
POST /api/auth/register   → IdentityService creates user, issues JWT
POST /api/wallets         → WalletService calls IdentityService via gRPC to verify user
POST /api/wallets/{id}/transactions
                          → WalletService saves deposit
                          → publishes TransactionCreatedMessage to RabbitMQ
                          → NotifyService consumes message (async, decoupled)
                          → notification written to notification_db
                          → [EMAIL STUB] logged to console
GET  /api/notifications   → NotifyService returns notification feed
```

---

## Roadmap

- [x] Step 1 — Minimal REST API
- [x] Step 2 — Clean Architecture (CQRS, MediatR, Result pattern, FluentValidation)
- [x] Step 3 — Full Feature Set (domain events, transactions, audit log, TestContainers)
- [x] Step 4 — Resilience (Serilog structured logging, rate limiting, pagination)
- [x] Step 5 — Events (Outbox pattern, BackgroundService, notifications, email stub)
- [x] Step 6 — Microservices (YARP Gateway, gRPC, RabbitMQ, MassTransit)
