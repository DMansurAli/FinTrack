# FinTrack API

A personal finance REST API built with ASP.NET Core 10, EF Core, PostgreSQL and JWT auth.

This project is built **step by step** — each step introduces new patterns and concepts on top of the previous one.

## Current Step: Step 1 — Minimal REST API

### Tech Stack
| Concern | Technology |
|---|---|
| Runtime | .NET 10, ASP.NET Core 10 |
| Database | PostgreSQL (EF Core 9) |
| Auth | JWT (HS256) + BCrypt passwords |
| Docs | Swagger UI |
| Tests | xUnit + FluentAssertions |

### Endpoints
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | /api/auth/register | ❌ | Create account, returns JWT |
| POST | /api/auth/login | ❌ | Login, returns JWT |
| GET | /api/wallets | ✅ | List your wallets |
| GET | /api/wallets/{id} | ✅ | Get one wallet |
| POST | /api/wallets | ✅ | Create wallet |
| PUT | /api/wallets/{id} | ✅ | Rename wallet |
| DELETE | /api/wallets/{id} | ✅ | Delete wallet |

### Running Locally

**Prerequisites:** .NET 10 SDK, Docker Desktop
```bash
# Start PostgreSQL
docker compose up -d

# Run the API (auto-migrates on first start)
dotnet run --project src/FinTrack.Api

# Open Swagger
# http://localhost:5168/docs
```

### Running Tests
```bash
dotnet test tests/FinTrack.Tests
```

19 tests, all passing, no Docker required.

### Project Structure
```
FinTrack/
├── src/
│   └── FinTrack.Api/
│       ├── Controllers/     ← AuthController, WalletsController
│       ├── Data/            ← AppDbContext, Migrations
│       ├── Middleware/      ← Global error handling
│       ├── Models/          ← User, Wallet
│       ├── Services/        ← JwtService
│       └── Program.cs
├── tests/
│   └── FinTrack.Tests/
│       ├── Auth/            ← 8 auth tests
│       └── Wallets/         ← 11 wallet tests
└── docker-compose.yml
```

### Key Decisions
- **Ownership isolation** — every wallet query filters by the logged-in user's ID
- **No email enumeration** — wrong password and unknown email both return 401 with the same message
- **BCrypt work factor 12** — secure password hashing
- **Auto-migrate on startup** — convenient for development
- **In-memory DB for tests** — fast tests, no Docker needed

---

## Roadmap

- [x] Step 1 — Minimal REST API
- [ ] Step 2 — Clean Architecture (Domain / Application / Infrastructure / Api layers, MediatR, CQRS)
- [ ] Step 3 — Full Feature Set (domain model, business rules, TestContainers)
- [ ] Step 4 — Resilience (Serilog, rate limiting, pagination, Repository + UoW)
- [ ] Step 5 — Events (domain events, Outbox pattern, background jobs)
- [ ] Step 6 — Microservices (gRPC, RabbitMQ, API Gateway)
