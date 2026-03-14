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

### Project Structure
```
FinTrack/
├── src/FinTrack.Api/
│   ├── Controllers/     ← AuthController, WalletsController
│   ├── Data/            ← AppDbContext, Migrations
│   ├── Middleware/      ← Global error handling
│   ├── Models/          ← User, Wallet
│   ├── Services/        ← JwtService
│   └── Program.cs
└── tests/FinTrack.Tests/
    ├── Auth/            ← 8 tests
    └── Wallets/         ← 11 tests
```

### Key Decisions
- **Ownership isolation** — every wallet query filters by the logged-in user's ID
- **No email enumeration** — wrong password and unknown email both return 401 with the same message
- **BCrypt work factor 12** — secure password hashing
- **Auto-migrate on startup** — convenient for development
- **In-memory DB for tests** — fast tests, no Docker needed

---

## Step 2 — Clean Architecture (`/FinTrackV2`)

Same endpoints, rebuilt with 4-layer Clean Architecture, CQRS, and the Result pattern. Every layer has a single responsibility and strict dependency rules.

### Architecture
```
Api → Application → Domain
Infrastructure → Domain + Application
```

| Layer | Responsibility |
|---|---|
| Domain | Entities, Result pattern, error constants — zero dependencies |
| Application | Commands, Queries, handlers, validators — no EF Core, no HTTP |
| Infrastructure | EF Core, BCrypt, JWT — implements Application interfaces |
| Api | Controllers, middleware — translates HTTP to/from MediatR |

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

### Project Structure
```
FinTrackV2/
├── src/
│   ├── FinTrack.Domain/
│   │   ├── Entities/        ← User, Wallet (factory methods, private setters)
│   │   ├── Common/          ← Result<T>, Error, AggregateRoot
│   │   └── Errors/          ← UserErrors, WalletErrors
│   ├── FinTrack.Application/
│   │   ├── Auth/Commands/   ← RegisterUser, LoginUser
│   │   ├── Wallets/         ← Commands + Queries
│   │   ├── Interfaces/      ← IUserRepository, IWalletRepository, IJwtService
│   │   └── Common/Behaviors/← ValidationBehavior (MediatR pipeline)
│   ├── FinTrack.Infrastructure/
│   │   ├── Persistence/     ← AppDbContext, UserRepository, WalletRepository
│   │   └── Auth/            ← JwtService, PasswordHasher
│   └── FinTrack.Api/
│       ├── Controllers/     ← Thin controllers (~5 lines each)
│       └── Middleware/      ← Global error handling
└── tests/FinTrack.Tests/
    ├── Auth/                ← 8 handler tests
    ├── Wallets/             ← 9 handler tests
    └── Common/              ← FakePasswordHasher, FakeJwtService, TestDbContext
```

---

## Step 3 — Full Feature Set (`/FinTrackV2`, extended)

Built on top of Step 2. Adds transactions, domain events, an audit log, and integration tests against a real database.

### New Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/wallets/{id}/transactions | ✅ | List transactions for a wallet |
| POST | /api/wallets/{id}/transactions | ✅ | Deposit or withdraw |

### New Patterns
| Pattern | What it does |
|---|---|
| Domain events | Entities raise events (e.g. `WalletCreatedEvent`) — handlers react without coupling |
| AggregateRoot | Base class that collects domain events raised during an operation |
| Business rules on entities | `Wallet.Deposit()` and `Wallet.Withdraw()` enforce rules and update balance atomically |
| Audit log | Every domain event is persisted to `AuditLogs` table as a JSON payload |
| TestContainers | Integration tests spin up a real PostgreSQL container, run migrations, verify SQL |

### Domain Event Flow
```
POST /api/wallets/{id}/transactions
  → CreateTransactionHandler
    → wallet.Deposit(100)           ← business rule enforced, event raised
      → TransactionCreatedEvent
    → DomainEventDispatcher.Publish()
      → TransactionCreatedAuditHandler  ← writes audit record
```

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
# 22 tests total:
#   18 unit tests  — run in <2s, no Docker needed
#    4 integration tests — spin up real PostgreSQL via TestContainers
```

### New Project Structure (additions to Step 2)
```
FinTrackV2/
├── src/
│   ├── FinTrack.Domain/
│   │   ├── Entities/Transaction.cs
│   │   ├── Enums/TransactionType.cs
│   │   ├── Events/              ← IDomainEvent, WalletCreatedEvent, TransactionCreatedEvent
│   │   └── Errors/TransactionErrors.cs
│   ├── FinTrack.Application/
│   │   ├── Transactions/        ← CreateTransaction command, GetTransactions query
│   │   └── Interfaces/          ← ITransactionRepository, IDomainEventDispatcher
│   ├── FinTrack.Infrastructure/
│   │   ├── Audit/               ← AuditLog entity, WalletCreatedAuditHandler, TransactionCreatedAuditHandler
│   │   └── Events/DomainEventDispatcher.cs
└── tests/FinTrack.Tests/
    └── Integration/             ← DatabaseFixture (TestContainers), WalletIntegrationTests
```

---

## Step 4 — Resilience (`/FinTrackV2`, extended)

Built on top of Step 3. Adds structured logging, rate limiting, and pagination.

### New Patterns
| Pattern | What it does |
|---|---|
| Serilog | Structured logging to console + rolling daily file — every request logs userId, method, path, status, duration |
| Rate limiting | Auth endpoints: 5 req/min per IP. API endpoints: 60 req/min. Returns `429` with JSON error |
| Pagination | `GET /transactions` accepts `?page=1&pageSize=20`, returns `PagedResult<T>` with metadata |

### Pagination Response Shape
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 347,
  "totalPages": 18,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### Running Locally
```bash
cd FinTrackV2
docker compose up -d
dotnet run --project src/FinTrack.Api
# API:  http://localhost:5153/docs
# Logs: FinTrackV2/src/FinTrack.Api/logs/
```

### Tests
```bash
dotnet test tests/FinTrack.Tests
# 22 tests — all passing (pagination tested via integration tests)
```

---

## Roadmap

- [x] Step 1 — Minimal REST API
- [x] Step 2 — Clean Architecture (CQRS, MediatR, Result pattern, FluentValidation)
- [x] Step 3 — Full Feature Set (domain events, transactions, audit log, TestContainers)
- [x] Step 4 — Resilience (Serilog structured logging, rate limiting, pagination)
- [ ] Step 5 — Events (Outbox pattern, background jobs, notifications)
- [ ] Step 6 — Microservices (gRPC, RabbitMQ, MassTransit, API Gateway)
