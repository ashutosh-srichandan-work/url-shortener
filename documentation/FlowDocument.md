# Request Flow Document

This document traces the complete lifecycle of each API operation through the system.

---

## System Flow Overview

```
						 ┌──────────────────────────────┐
						 │        HTTP Client            │
						 │  (Browser / Swagger / curl)   │
						 └──────────┬───────────────────┘
									│
									▼
					┌───────────────────────────────┐
					│     ASP.NET Core Pipeline      │
					│  ┌─────────────────────────┐  │
					│  │ GlobalExceptionMiddleware│  │
					│  ├─────────────────────────┤  │
					│  │ SerilogRequestLogging    │  │
					│  ├─────────────────────────┤  │
					│  │ RateLimiter              │  │
					│  ├─────────────────────────┤  │
					│  │ Controller Routing       │  │
					│  └─────────────────────────┘  │
					└──────────┬────────────────────┘
							   │
					┌──────────▼────────────────────┐
					│       Controller Layer         │
					│  LinksController               │
					│  RedirectController             │
					└──────────┬────────────────────┘
							   │
					┌──────────▼────────────────────┐
					│     Application Layer          │
					│  UrlShortenerService            │
					│  CreateShortUrlRequestValidator │
					└──────────┬────────────────────┘
							   │
					┌──────────▼────────────────────┐
					│     Domain Layer               │
					│  ShortUrl Entity               │
					│  IShortUrlRepository            │
					└──────────┬────────────────────┘
							   │
					┌──────────▼────────────────────┐
					│    Infrastructure Layer        │
					│  ShortUrlRepository (EF Core)  │
					│  AppDbContext → SQLite          │
					└───────────────────────────────┘
```

---

## Flow 1: Create Short URL

```
POST /api/v1/links
```

```
Step 1: Request enters middleware pipeline
	│
	├── GlobalExceptionMiddleware wraps the entire pipeline
	├── SerilogRequestLogging records start time
	├── RateLimiter checks "create" policy (20 req/min)
	│   └── If exceeded → 429 Too Many Requests (stops here)
	│
Step 2: LinksController.Create() receives the request
	│
	├── FluentValidation validates the request DTO:
	│   ├── Is URL non-empty?
	│   ├── Is URL a valid HTTP/HTTPS URL?
	│   ├── Is custom alias 3-50 chars, valid characters? (if provided)
	│   └── Is expiration date in the future? (if provided)
	│   └── If validation fails → 400 Bad Request with error details
	│
Step 3: UrlShortenerService.CreateShortUrlAsync()
	│
	├── Check for duplicate original URL (if no custom alias):
	│   ├── Call repository.GetByOriginalUrlAsync()
	│   └── If found → Return existing ShortUrlResponse (idempotent)
	│
	├── Determine short code:
	│   ├── Use custom alias if provided
	│   └── Otherwise generate random 7-char code (cryptographic)
	│
	├── Check if short code already exists:
	│   ├── Call repository.ShortCodeExistsAsync()
	│   └── If exists → throw InvalidOperationException
	│       └── Caught by middleware → 409 Conflict
	│
	├── Create ShortUrl entity (Id, OriginalUrl, ShortCode, timestamps)
	├── Call repository.CreateAsync() → EF Core INSERT → SQLite
	├── Log: "Created short URL {ShortCode} for {OriginalUrl}"
	│
Step 4: Controller returns 201 Created
	│
	├── Location header points to GET /api/v1/links/{id}
	└── Body contains ShortUrlResponse with full short URL
```

---

## Flow 2: Redirect (Use Short Link)

```
GET /{shortCode}
```

```
Step 1: Request enters middleware pipeline
	│
	├── GlobalExceptionMiddleware wraps
	├── SerilogRequestLogging records
	├── RateLimiter: redirect is NOT rate-limited
	│
Step 2: RedirectController.RedirectToOriginal()
	│
Step 3: UrlShortenerService.GetOriginalUrlAndTrackClickAsync()
	│
	├── Call repository.GetByShortCodeAsync()
	│
	├── If not found OR IsDeleted == true:
	│   ├── Log warning: "Short code not found or deleted"
	│   └── Return null → Controller returns 404 Not Found
	│
	├── If IsExpired == true (ExpiresAtUtc < now):
	│   ├── Log warning: "Short code has expired"
	│   └── Return null → Controller returns 404 Not Found
	│
	├── Increment ClickCount by 1
	├── Set LastAccessedAtUtc = DateTime.UtcNow
	├── Call repository.UpdateAsync() → EF Core UPDATE → SQLite
	├── Log: "Redirecting {ShortCode}, click count: {ClickCount}"
	│
Step 4: Controller returns 302 Found
	│
	└── Location header contains the original URL
		└── Browser automatically follows the redirect
```

---

## Flow 3: Get Link Details

```
GET /api/v1/links/{id}
```

```
Step 1: Middleware pipeline (same as above)
	│
Step 2: LinksController.GetDetails(id)
	│
Step 3: UrlShortenerService.GetUrlDetailsAsync(id, baseUrl)
	│
	├── Call repository.GetByIdAsync(id) → EF Core FindAsync
	│
	├── If not found → Return null → Controller returns 404
	│
	├── Map entity to ShortUrlResponse:
	│   ├── Build full shortUrl: "{baseUrl}/{shortCode}"
	│   ├── Determine status: "Deleted" / "Expired" / "Active"
	│   └── Include all timestamps and click count
	│
Step 4: Controller returns 200 OK with ShortUrlResponse
```

---

## Flow 4: Get Analytics

```
GET /api/v1/links/{id}/stats
```

```
Step 1: Middleware pipeline
	│
Step 2: LinksController.GetAnalytics(id)
	│
Step 3: UrlShortenerService.GetAnalyticsAsync(id)
	│
	├── Call repository.GetByIdAsync(id)
	├── If not found → 404
	├── Map to UrlAnalyticsResponse:
	│   ├── TotalClicks = entity.ClickCount
	│   ├── Status = "Active" / "Expired" / "Deleted"
	│   └── All timestamps
	│
Step 4: Controller returns 200 OK with UrlAnalyticsResponse
```

---

## Flow 5: Soft Delete

```
DELETE /api/v1/links/{id}
```

```
Step 1: Middleware pipeline
	│
Step 2: LinksController.Delete(id)
	│
Step 3: UrlShortenerService.DeleteUrlAsync(id)
	│
	├── Call repository.GetByIdAsync(id)
	├── If not found → Return false → Controller returns 404
	│
	├── Set entity.IsDeleted = true
	├── Set entity.DeletedAtUtc = DateTime.UtcNow
	├── Call repository.UpdateAsync() → EF Core UPDATE
	├── Log: "Soft-deleted short URL {Id}"
	│
Step 4: Controller returns 204 No Content
```

---

## Flow 6: List All (Paginated)

```
GET /api/v1/links?page=1&pageSize=10
```

```
Step 1: Middleware pipeline
	│
Step 2: LinksController.GetAll(page, pageSize)
	│
	├── Clamp parameters: page >= 1, 1 <= pageSize <= 100
	│
Step 3: UrlShortenerService.GetAllPaginatedAsync()
	│
	├── Call repository.GetPaginatedAsync(page, pageSize):
	│   ├── COUNT all non-deleted URLs → totalCount
	│   ├── SELECT with WHERE !IsDeleted
	│   ├── ORDER BY CreatedAtUtc DESC
	│   ├── SKIP (page-1)*pageSize, TAKE pageSize
	│   └── Return (items, totalCount)
	│
	├── Calculate: totalPages, hasNextPage, hasPreviousPage
	├── Map each item to ShortUrlResponse
	│
Step 4: Controller returns 200 OK with PagedResponse<ShortUrlResponse>
```

---

## Error Flow: Unhandled Exception

```
Any endpoint throws an unexpected exception
```

```
Step 1: Exception bubbles up from controller/service
	│
Step 2: GlobalExceptionMiddleware catches it
	│
	├── If InvalidOperationException:
	│   ├── Log as Warning
	│   └── Return 409 Conflict with ProblemDetails
	│
	├── If any other Exception:
	│   ├── Log as Error with full stack trace
	│   └── Return 500 Internal Server Error with ProblemDetails
	│       └── Detail: "An unexpected error occurred." (no leak)
	│
Step 3: ProblemDetails JSON response:
	{
		"status": 409,
		"title": "Conflict",
		"detail": "The alias 'my-link' is already in use.",
		"instance": "/api/v1/links"
	}
```

---

## Startup Flow

```
Application starts (dotnet run)
	│
	├── Bootstrap Serilog logger (captures startup failures)
	│
	├── Build WebApplication:
	│   ├── Configure Serilog (console + file)
	│   ├── Register rate limiter (fixed + create policies)
	│   ├── Register controllers
	│   ├── Register Swagger with XML comments
	│   ├── Register Application services (DI)
	│   ├── Register Infrastructure (DbContext + Repository)
	│   └── Register health checks
	│
	├── Build the app
	│
	├── Apply EF Core migrations automatically
	│   └── Creates urlshortener.db if it doesn't exist
	│
	├── Configure middleware pipeline:
	│   ├── GlobalExceptionMiddleware
	│   ├── SerilogRequestLogging
	│   ├── Swagger (Development only)
	│   ├── HttpsRedirection (Production only)
	│   ├── RateLimiter
	│   └── Authorization
	│
	├── Map controller routes
	├── Map /health endpoint
	│
	└── Start listening on http://localhost:5073
```
