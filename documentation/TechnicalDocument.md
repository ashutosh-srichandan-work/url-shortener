# Technical Document

## System Overview

The URL Shortener is a backend web service built with ASP.NET Core 8 that provides REST APIs for creating, managing, and redirecting shortened URLs. It uses a four-layer clean architecture with SQLite persistence.

---

## Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 8.0 |
| Web Framework | ASP.NET Core Web API | 8.0 |
| ORM | Entity Framework Core | 8.0 |
| Database | SQLite | via EF Core provider |
| Validation | FluentValidation | 12.1.1 |
| Logging | Serilog (Console + File sinks) | 10.0.0 |
| API Docs | Swashbuckle (Swagger/OpenAPI) | 6.6.2 |
| Testing | xUnit + FluentAssertions + Moq | See test csproj |
| Rate Limiting | ASP.NET Core built-in RateLimiter | 8.0 |

---

## Project Structure

```
UrlShortener/
├── src/
│   ├── UrlShortener.Api/                  # Presentation layer
│   │   ├── Controllers/
│   │   │   ├── LinksController.cs         # CRUD endpoints (api/v1/links)
│   │   │   └── RedirectController.cs      # Redirect endpoint (/{shortCode})
│   │   ├── Middleware/
│   │   │   └── GlobalExceptionMiddleware.cs
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── Program.cs                     # App composition root
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   ├── UrlShortener.Application/          # Business logic layer
│   │   ├── DTOs/
│   │   │   └── ShortUrlDtos.cs            # Request/Response records
│   │   ├── Interfaces/
│   │   │   └── IUrlShortenerService.cs
│   │   ├── Services/
│   │   │   └── UrlShortenerService.cs
│   │   ├── Validators/
│   │   │   └── CreateShortUrlRequestValidator.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── UrlShortener.Domain/               # Domain layer
│   │   ├── Entities/
│   │   │   └── ShortUrl.cs
│   │   └── Interfaces/
│   │       └── IShortUrlRepository.cs
│   │
│   └── UrlShortener.Infrastructure/       # Data access layer
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   └── Migrations/
│       ├── Repositories/
│       │   └── ShortUrlRepository.cs
│       └── DependencyInjection.cs
│
├── tests/
│   └── UrlShortener.Tests/
│       ├── UrlShortenerServiceTests.cs
│       └── CreateShortUrlRequestValidatorTests.cs
│
├── documentation/                          # Project documentation
├── docs/                                   # Specification files
└── README.md
```

---

## API Specification

### Base URL
```
http://localhost:5073
```

### Endpoints

#### `POST /api/v1/links` — Create Short URL
- **Rate limit**: 20 req/min
- **Request body**:
  ```json
  {
	"url": "https://example.com",
	"customAlias": "my-link",
	"expiresAtUtc": "2025-12-31T23:59:59Z"
  }
  ```
- **201**: Returns `ShortUrlResponse`
- **400**: Validation failed
- **409**: Alias already exists

#### `GET /api/v1/links?page=1&pageSize=10` — List All (Paginated)
- **200**: Returns `PagedResponse<ShortUrlResponse>`
- Excludes soft-deleted URLs
- Ordered by `CreatedAtUtc` descending

#### `GET /api/v1/links/{id}` — Get Details
- **200**: Returns `ShortUrlResponse`
- **404**: Not found

#### `GET /api/v1/links/{id}/stats` — Get Analytics
- **200**: Returns `UrlAnalyticsResponse`
- **404**: Not found

#### `DELETE /api/v1/links/{id}` — Soft Delete
- **204**: Deleted
- **404**: Not found

#### `GET /{shortCode}` — Redirect
- **302**: Redirects to original URL
- **404**: Not found / expired / deleted

#### `GET /health` — Health Check
- **200**: Returns `Healthy` / `Unhealthy`

---

## Database Schema

**Provider**: SQLite  
**Connection string**: `Data Source=urlshortener.db`  
**Migrations**: Applied automatically on startup via `db.Database.Migrate()`

### ShortUrls Table

| Column | Type | Nullable | Constraints |
|--------|------|----------|-------------|
| Id | GUID | No | Primary Key |
| OriginalUrl | TEXT(2048) | No | Required |
| ShortCode | TEXT(50) | No | Required, Unique Index |
| CreatedAtUtc | DATETIME | No | |
| ExpiresAtUtc | DATETIME | Yes | |
| LastAccessedAtUtc | DATETIME | Yes | Updated on each redirect |
| ClickCount | INTEGER | No | Default 0, incremented on redirect |
| IsDeleted | BOOLEAN | No | Default false |
| DeletedAtUtc | DATETIME | Yes | Set on soft-delete |
| CreatedBy | TEXT(256) | Yes | |

---

## Middleware Pipeline

The middleware executes in this order:

```
1. GlobalExceptionMiddleware     → Catches unhandled exceptions → ProblemDetails
2. SerilogRequestLogging         → Logs every HTTP request with timing
3. Swagger (Development only)    → Serves OpenAPI spec and Swagger UI
4. HttpsRedirection (Prod only)  → Redirects HTTP to HTTPS
5. RateLimiter                   → Enforces request rate limits
6. Authorization                 → (placeholder for future auth)
7. Controller routing            → Maps to controller actions
```

---

## Configuration

### `appsettings.json`
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=urlshortener.db"
  },
  "Serilog": {
	"MinimumLevel": {
	  "Default": "Information",
	  "Override": {
		"Microsoft.AspNetCore": "Warning",
		"Microsoft.EntityFrameworkCore": "Warning"
	  }
	}
  }
}
```

### Logging
- **Console**: All logs streamed to stdout
- **File**: Rolling daily log files in `src/UrlShortener.Api/logs/`
- **Sensitive data**: Never logged (no URLs containing credentials, no PII)

---

## Testing Strategy

### Unit Tests (22 tests)

| Category | Tests | What's Covered |
|----------|-------|----------------|
| Service - Create | 3 | Valid creation, custom alias, duplicate alias conflict |
| Service - Redirect | 4 | Valid redirect + click tracking, missing code, expired, deleted |
| Service - Delete | 2 | Soft delete success, non-existent ID |
| Service - Analytics | 1 | Returns analytics for existing URL |
| Service - Details | 2 | Returns details, non-existent ID |
| Service - Pagination | 2 | Correct paging, excludes deleted |
| Service - Duplicate URL | 1 | Returns existing instead of creating duplicate |
| Validator | 7 | Valid request, empty URL, invalid URL, short alias, invalid chars, past expiry, valid alias |

### Test Infrastructure
- **Database**: EF Core InMemory provider (per-test isolated database)
- **Mocking**: Moq for ILogger
- **Assertions**: FluentAssertions for readable test expectations

### Running Tests
```bash
dotnet test
```
