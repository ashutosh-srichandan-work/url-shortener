# URL Shortener API

A production-ready URL Shortener Service built with **ASP.NET Core (.NET 8)**, Entity Framework Core, and SQLite. Developed incrementally using AI-assisted programming to demonstrate real-world, commit-by-commit software evolution.

---

## Features

- **URL Shortening** - Create short links with auto-generated or custom aliases
- **Redirect** - `GET /{shortCode}` redirects to the original URL
- **Click Tracking** - Tracks total clicks and last accessed timestamp per link
- **Analytics** - Per-link stats endpoint with click count, status, and timestamps
- **Pagination** - List all links with `page`/`pageSize` query parameters
- **Expiration** - Optional expiry date; expired links return 404 on redirect
- **Soft Delete** - Links are soft-deleted (recoverable), not permanently removed
- **Input Validation** - FluentValidation for URL format, alias rules, and expiry checks
- **Rate Limiting** - Fixed-window rate limiting (100/min global, 20/min for creation)
- **Health Checks** - `GET /health` endpoint with database connectivity check
- **Structured Logging** - Serilog with console and rolling file sinks
- **Global Error Handling** - ProblemDetails responses for all error types
- **Swagger/OpenAPI** - Interactive API docs available in development mode
- **Unit Tests** - xUnit + FluentAssertions + EF Core InMemory for service and validation tests

---

## Tech Stack

| Layer            | Technology                                      |
|------------------|--------------------------------------------------|
| Framework        | ASP.NET Core 8.0 Web API                        |
| Database         | SQLite via Entity Framework Core 8               |
| Validation       | FluentValidation                                 |
| Logging          | Serilog (Console + File)                         |
| Testing          | xUnit, FluentAssertions, Moq, EF Core InMemory  |
| Documentation    | Swagger / OpenAPI (Swashbuckle)                  |
| Rate Limiting    | ASP.NET Core built-in RateLimiter middleware     |
| Health Checks    | ASP.NET Core Health Checks + EF Core DbContext   |

---

## Project Structure

```
url-shortener/
+-- src/
|   +-- UrlShortener.Api/            # Web API layer
|   |   +-- Controllers/             # LinksController (api/v1/links)
|   |   +-- DTOs/                    # Request/response records
|   |   +-- Middleware/              # GlobalExceptionMiddleware
|   |   +-- Services/                # UrlShortenerService + interface
|   |   +-- Validators/              # FluentValidation rules
|   |   +-- Program.cs              # Composition root
|   +-- UrlShortener.Domain/         # Domain layer
|   |   +-- Entities/                # ShortUrl entity
|   |   +-- Interfaces/              # IShortUrlRepository
|   +-- UrlShortener.Infrastructure/ # Data access layer
|       +-- Data/                    # AppDbContext + Migrations
|       +-- Repositories/            # ShortUrlRepository (EF Core)
+-- tests/
|   +-- UrlShortener.Tests/          # Unit tests
+-- UrlShortener.slnx                # Solution file
+-- README.md
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run the API

```bash
cd src/UrlShortener.Api
dotnet run
```

The API starts at `https://localhost:5001` (or the port configured in `launchSettings.json`).
Swagger UI is available at `/swagger` in development mode.

### Run Tests

```bash
dotnet test
```

---

## API Endpoints

All management endpoints are under `api/v1/links`. The redirect endpoint is at the root.

| Method   | Route                        | Description                      |
|----------|------------------------------|----------------------------------|
| `GET`    | `/api/v1/links`              | List all links (paginated)       |
| `POST`   | `/api/v1/links`              | Create a new short link          |
| `GET`    | `/api/v1/links/{id}`         | Get link details by ID           |
| `GET`    | `/api/v1/links/{id}/stats`   | Get link analytics               |
| `DELETE` | `/api/v1/links/{id}`         | Soft-delete a link               |
| `GET`    | `/{shortCode}`               | Redirect to original URL         |
| `GET`    | `/health`                    | Health check                     |

### Create a Short Link

```bash
curl -X POST https://localhost:5001/api/v1/links \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com", "customAlias": "ex", "expiresAtUtc": "2025-12-31T23:59:59Z"}'
```

**Response** (`201 Created`):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "originalUrl": "https://example.com",
  "shortCode": "ex",
  "shortUrl": "https://localhost:5001/ex",
  "createdAtUtc": "2025-01-15T10:30:00Z",
  "expiresAtUtc": "2025-12-31T23:59:59Z",
  "clickCount": 0,
  "lastAccessedAtUtc": null,
  "status": "Active"
}
```

### List Links (Paginated)

```bash
curl https://localhost:5001/api/v1/links?page=1&pageSize=10
```

**Response** (`200 OK`):
```json
{
  "items": [ ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

## Rate Limiting

| Policy    | Limit        | Scope                      |
|-----------|-------------|----------------------------|
| `fixed`   | 100 req/min | All management endpoints   |
| `create`  | 20 req/min  | `POST /api/v1/links` only  |

Exceeding the limit returns `429 Too Many Requests`.
The redirect endpoint (`/{shortCode}`) is exempt from rate limiting.

---

## Error Handling

All errors return RFC 7807 Problem Details:

| Status | Condition                   |
|--------|-----------------------------|
| `400`  | Validation failure          |
| `404`  | Resource not found          |
| `409`  | Duplicate alias conflict    |
| `429`  | Rate limit exceeded         |
| `500`  | Unhandled server error      |

## License

This project is for demonstration and educational purposes.
