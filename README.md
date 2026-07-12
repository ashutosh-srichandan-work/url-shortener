# URL Shortener API

A production-ready URL Shortener Service built with **ASP.NET Core (.NET 8)**, Entity Framework Core, and SQLite. Developed using AI-assisted engineering to demonstrate clean architecture, test-driven design, and production-grade API development.

---

## Features

- **URL Shortening** — Create short links with auto-generated or custom aliases
- **Redirect** — `GET /{shortCode}` redirects to the original URL
- **Click Tracking** — Tracks total clicks and last accessed timestamp per link
- **Analytics** — Per-link stats endpoint with click count, status, and timestamps
- **Duplicate Detection** — Submitting the same URL returns the existing short link (idempotent)
- **Pagination** — List all links with `page`/`pageSize` query parameters
- **Expiration** — Optional expiry date; expired links return 404 on redirect
- **Soft Delete** — Links are soft-deleted (recoverable), not permanently removed
- **Input Validation** — FluentValidation for URL format, alias rules, and expiry checks
- **Rate Limiting** — Fixed-window rate limiting (100/min global, 20/min for creation)
- **Health Checks** — `GET /health` endpoint with database connectivity check
- **Structured Logging** — Serilog with console and rolling file sinks
- **Global Error Handling** — ProblemDetails (RFC 7807) responses for all error types
- **Swagger/OpenAPI** — Interactive API docs with examples, available in development mode
- **Unit Tests** — 22 tests covering services, validators, and edge cases

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8.0 Web API |
| Database | SQLite via Entity Framework Core 8 |
| Validation | FluentValidation |
| Logging | Serilog (Console + Rolling File) |
| Testing | xUnit, FluentAssertions, Moq, EF Core InMemory |
| API Docs | Swagger / OpenAPI (Swashbuckle) |
| Rate Limiting | ASP.NET Core built-in RateLimiter middleware |
| Health Checks | ASP.NET Core Health Checks + EF Core DbContext |

---

## Project Structure

```
UrlShortener/
├── src/
│   ├── UrlShortener.Api/                # Web API layer (controllers, middleware)
│   │   ├── Controllers/
│   │   │   ├── LinksController.cs       # CRUD endpoints (api/v1/links)
│   │   │   └── RedirectController.cs    # Redirect endpoint (/{shortCode})
│   │   ├── Middleware/
│   │   │   └── GlobalExceptionMiddleware.cs
│   │   └── Program.cs                  # Composition root
│   │
│   ├── UrlShortener.Application/        # Business logic layer
│   │   ├── DTOs/                        # Request/Response records
│   │   ├── Interfaces/                  # Service contracts
│   │   ├── Services/                    # Business logic implementation
│   │   └── Validators/                  # FluentValidation rules
│   │
│   ├── UrlShortener.Domain/             # Domain layer (no dependencies)
│   │   ├── Entities/                    # ShortUrl entity
│   │   └── Interfaces/                  # Repository contracts
│   │
│   └── UrlShortener.Infrastructure/     # Data access layer
│       ├── Data/                        # AppDbContext + EF Core Migrations
│       └── Repositories/               # Repository implementations
│
├── tests/
│   └── UrlShortener.Tests/              # Unit tests (xUnit)
│
├── documentation/                        # Project documentation
│   ├── Architecture.md                  # Architecture overview
│   ├── DesignDocument.md                # Design decisions and rationale
│   ├── TechnicalDocument.md             # Technical specification
│   ├── FlowDocument.md                  # Request flow diagrams
│   ├── UserGuide.md                     # End-user guide
│   └── EngineeringSummary.md            # Engineering summary and trade-offs
│
├── docs/                                 # Assignment specifications
└── README.md
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

That's it. No database server, no Docker, no external services needed.

### Quick Start (After Cloning)

**Option 1 — From the solution root directory (recommended):**
```bash
dotnet run --project src/UrlShortener.Api
```

**Option 2 — Navigate to the API project first:**
```bash
cd src/UrlShortener.Api
dotnet run
```

> The database is created and migrated **automatically** on first run. No manual setup required.

The server starts at **http://localhost:5073**.  
Open **http://localhost:5073/swagger** in your browser to access the interactive API documentation.

### Run Tests

```bash
dotnet test
```

Runs all 22 unit tests for services, validators, and edge cases.

---

## API Endpoints

All management endpoints are under `api/v1/links`. The redirect endpoint is at the root.

| Method | Route | Description | Rate Limit |
|--------|-------|-------------|------------|
| `GET` | `/api/v1/links` | List all links (paginated) | 100/min |
| `POST` | `/api/v1/links` | Create a new short link | 20/min |
| `GET` | `/api/v1/links/{id}` | Get link details by ID | 100/min |
| `GET` | `/api/v1/links/{id}/stats` | Get link analytics | 100/min |
| `DELETE` | `/api/v1/links/{id}` | Soft-delete a link | 100/min |
| `GET` | `/{shortCode}` | Redirect to original URL | None |
| `GET` | `/health` | Health check | None |

### Create a Short Link

```bash
curl -X POST http://localhost:5073/api/v1/links \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com", "customAlias": "my-link", "expiresAtUtc": "2025-12-31T23:59:59Z"}'
```

**Request fields:**

| Field | Required | Description |
|-------|----------|-------------|
| `url` | Yes | The original URL (must be valid HTTP or HTTPS) |
| `customAlias` | No | Custom short code (3-50 chars, alphanumeric + hyphens + underscores). If omitted, a random 7-character code is generated. |
| `expiresAtUtc` | No | Expiration date in UTC. If omitted, the link never expires. |

**Response** (`201 Created`):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "originalUrl": "https://example.com",
  "shortCode": "my-link",
  "shortUrl": "http://localhost:5073/my-link",
  "createdAtUtc": "2025-07-13T10:30:00Z",
  "expiresAtUtc": "2025-12-31T23:59:59Z",
  "clickCount": 0,
  "lastAccessedAtUtc": null,
  "status": "Active"
}
```

### Use the Short Link

Open **http://localhost:5073/my-link** directly in your browser. You will be redirected to the original URL.

> ⚠️ The redirect endpoint cannot be tested from Swagger UI due to browser CORS restrictions. Always open the short URL directly in your browser's address bar.

### List Links (Paginated)

```bash
curl "http://localhost:5073/api/v1/links?page=1&pageSize=10"
```

**Response** (`200 OK`):
```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

## Error Handling

All errors return RFC 7807 Problem Details:

| Status | Condition |
|--------|-----------|
| `400` | Validation failure (invalid URL, alias too short, past expiration) |
| `404` | Resource not found, expired, or deleted |
| `409` | Duplicate custom alias conflict |
| `429` | Rate limit exceeded (wait and retry) |
| `500` | Unhandled server error |

---

## Documentation

Detailed documentation is available in the `documentation/` folder:

| Document | Description |
|----------|-------------|
| [Architecture.md](documentation/Architecture.md) | Layered architecture, dependency flow, database design |
| [DesignDocument.md](documentation/DesignDocument.md) | Every design decision with rationale (framework, DB, algorithms, etc.) |
| [TechnicalDocument.md](documentation/TechnicalDocument.md) | Full technical specification, API details, testing strategy |
| [FlowDocument.md](documentation/FlowDocument.md) | Step-by-step request flows for every endpoint |
| [UserGuide.md](documentation/UserGuide.md) | End-user guide with examples |
| [EngineeringSummary.md](documentation/EngineeringSummary.md) | Engineering summary, scenarios, risks, and trade-offs |

---

## License

This project is for demonstration and educational purposes.
