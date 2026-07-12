# Design Document

## Overview

This document explains every design decision made in the URL Shortener project — from framework choice to database design to error handling strategy — and the reasoning behind each.

---

## 1. Framework: ASP.NET Core 8 Web API (Controllers)

**Choice**: ASP.NET Core 8 with traditional Controllers pattern  
**Alternatives considered**: Minimal APIs, Node.js/Express, Python/FastAPI

**Why ASP.NET Core 8:**
- Latest LTS-aligned .NET version with mature ecosystem
- Built-in dependency injection, middleware pipeline, and configuration
- Strong typing catches errors at compile time
- Excellent performance characteristics
- Required by the assignment specification

**Why Controllers over Minimal APIs:**
- Controllers provide natural grouping of related endpoints (all link operations in one class)
- Built-in model binding, validation integration, and attribute routing
- Better Swagger documentation generation with XML comments
- More familiar pattern for enterprise teams
- Easier to add cross-cutting concerns (filters, attributes) per-controller

---

## 2. Architecture: Four-Layer Clean Architecture

**Choice**: Api → Application → Domain ← Infrastructure

**Why layered architecture:**
- **Separation of concerns**: Each layer has a single responsibility
- **Testability**: Business logic (Application) is testable without a database — just mock the repository interface
- **Flexibility**: Infrastructure can be swapped (e.g., SQLite → PostgreSQL) without touching business logic
- **Domain purity**: The Domain layer has zero dependencies — entities and interfaces only

**Why four layers instead of two or three:**
- **Api**: HTTP concerns only (controllers, middleware, request/response)
- **Application**: Business logic, DTOs, validation — this is where "what the system does" lives
- **Domain**: "What the system is" — entity definitions and repository contracts
- **Infrastructure**: "How the system persists" — EF Core, database specifics

Splitting Domain from Infrastructure ensures the entity model never depends on EF Core annotations or database concepts.

---

## 3. Database: SQLite via Entity Framework Core

**Choice**: SQLite with EF Core as the ORM  
**Alternatives considered**: SQL Server, PostgreSQL, In-Memory only, Dapper

**Why SQLite:**
- **Zero infrastructure**: No database server needed — just a file on disk
- **Cross-platform**: Works identically on Windows, macOS, and Linux
- **Clone-and-run**: After `git clone`, `dotnet run` creates the database automatically
- **Sufficient for prototype**: Handles concurrent reads well; write contention is minimal for single-instance
- Required by the assignment specification

**Why EF Core (not Dapper or raw SQL):**
- **Migrations**: Schema changes are versioned and applied automatically on startup
- **Abstraction**: Switching to SQL Server later requires only changing the provider registration
- **LINQ**: Compile-time checked queries reduce runtime SQL errors
- **Convention-based mapping**: Less boilerplate than manual mapping

**Why auto-migrate on startup (`db.Database.Migrate()`):**
- Eliminates manual migration steps for developers
- Database is always in sync with the code
- Acceptable for development/prototype; production would use a migration pipeline

---

## 4. Short Code Generation: Cryptographic Randomness

**Choice**: `RandomNumberGenerator.GetBytes()` with 7-character alphanumeric codes  
**Alternatives considered**: GUIDs, sequential IDs, hash-based, Base62 encoding

**Why cryptographic randomness:**
- **Unpredictable**: Codes cannot be guessed or enumerated (security requirement)
- **No external dependencies**: Uses built-in .NET cryptography
- **Collision-resistant**: 62^7 ≈ 3.5 trillion possible codes

**Why 7 characters:**
- Short enough to be human-friendly in URLs
- Long enough to avoid collisions for millions of URLs
- Combined with uniqueness check loop + DB unique constraint as safety net

**Why not hash-based (e.g., MD5/SHA of URL):**
- Same URL could legitimately need different short codes (different expiration dates, etc.)
- Hash collisions would need handling anyway
- We have duplicate URL detection as a separate feature

---

## 5. Duplicate URL Detection: Idempotent Return

**Choice**: If the same original URL is submitted without a custom alias, return the existing short URL  
**Alternative**: Reject as a duplicate (409 Conflict)

**Why return existing:**
- **Idempotent**: Calling the same operation twice produces the same result — a REST best practice
- **User-friendly**: Users don't need to remember if they already shortened a URL
- **Saves storage**: Avoids creating redundant entries
- Custom alias conflicts still return 409 — because the user has specific intent that must be honored

---

## 6. Soft Delete over Hard Delete

**Choice**: Set `IsDeleted = true` and `DeletedAtUtc` instead of removing the row  
**Alternative**: `DELETE FROM ShortUrls WHERE Id = ...`

**Why soft delete:**
- **Audit trail**: Know when and what was deleted
- **Recovery**: Accidentally deleted links can be restored (with a future endpoint)
- **Analytics preservation**: Click history is retained even for deleted links
- **Referential safety**: No orphaned references if other systems track link IDs

---

## 7. Validation: FluentValidation

**Choice**: FluentValidation library with a dedicated validator class  
**Alternatives considered**: Data Annotations, manual if-checks, custom middleware

**Why FluentValidation:**
- **Declarative rules**: Easy to read — `RuleFor(x => x.Url).NotEmpty()`
- **Testable**: Validator is a plain class — unit test it independently
- **Separation**: Validation logic lives in Application layer, not on the DTO itself
- **Rich error messages**: Customizable, consistent error responses
- **Conditional rules**: `.When()` clauses for optional field validation

---

## 8. Error Handling: Global Exception Middleware + ProblemDetails

**Choice**: Custom middleware that catches exceptions and returns RFC 7807 ProblemDetails  
**Alternative**: Try-catch in every controller action, exception filters

**Why global middleware:**
- **Single point**: All unhandled exceptions are caught in one place
- **Consistent format**: Every error response has the same JSON structure
- **Clean controllers**: No try-catch noise in business logic
- **Type-based routing**: `InvalidOperationException` → 409, everything else → 500

**Why ProblemDetails (RFC 7807):**
- Industry standard for HTTP API errors
- Structured format that clients can parse programmatically
- Includes `status`, `title`, `detail`, and `instance` (the request path)

---

## 9. Logging: Serilog with Structured Logging

**Choice**: Serilog with Console + File sinks  
**Alternatives considered**: Built-in `ILogger` only, NLog, log4net

**Why Serilog:**
- **Structured logging**: Log properties (ShortCode, OriginalUrl) are searchable, not just embedded in strings
- **Multiple sinks**: Console for development, rolling file for persistence
- **Request logging middleware**: `UseSerilogRequestLogging()` logs every HTTP request with timing
- **Bootstrap logger**: Captures startup failures before DI is configured
- Required by the assignment specification

**What we log:**
- HTTP requests (method, path, status, duration) — via Serilog middleware
- Business events (created, redirected, deleted) — with short code context
- Warnings (not found, expired, duplicate) — for operational visibility
- Errors (unhandled exceptions) — with full stack traces

**What we don't log:**
- Full original URLs in production (could contain tokens/credentials)
- Request/response bodies
- User IP addresses

---

## 10. Rate Limiting: Built-in ASP.NET Core Middleware

**Choice**: Fixed-window rate limiter with two policies  
**Alternatives considered**: Third-party libraries (AspNetCoreRateLimit), API gateway rate limiting

**Why built-in:**
- **No extra dependency**: Ships with ASP.NET Core 8
- **Simple configuration**: Defined in `Program.cs` with fluent API
- **Per-policy flexibility**: Different limits for different endpoints

**Policy design:**
- `fixed` (100 req/min): Applied to all management endpoints — prevents scraping
- `create` (20 req/min): Applied to POST only — prevents URL creation abuse
- Redirect endpoint is exempt — should never be throttled for end users

**Limitation**: In-memory only — resets on restart, not shared across instances. A production deployment would use Redis-backed rate limiting.

---

## 11. DTOs: Records with `init` Properties

**Choice**: C# `record` types with `init`-only setters  
**Alternative**: Plain classes, mutable DTOs

**Why records:**
- **Immutability**: Once created, response objects can't be accidentally modified
- **Value semantics**: Two responses with the same data are considered equal
- **Concise**: Less boilerplate than classes with constructors
- **`init`-only**: Properties can be set during construction but not after

---

## 12. API Design: REST with Versioned Routes

**Choice**: `api/v1/links` with RESTful verbs  
**Alternative**: RPC-style (`/createLink`, `/deleteLink`), GraphQL

**Why REST:**
- Standard, well-understood API style
- Maps naturally to CRUD operations
- Proper HTTP status codes convey meaning (201 Created, 204 No Content, 409 Conflict)

**Why URL-prefix versioning (`/v1/`):**
- Simple and visible — clients always know which version they're using
- Easy to run v1 and v2 side-by-side
- No header negotiation complexity

---

## 13. Testing: xUnit with In-Memory Database

**Choice**: xUnit + FluentAssertions + EF Core InMemory + Moq  
**Alternative**: NUnit, MSTest, real SQLite for tests

**Why xUnit:**
- Most popular .NET testing framework
- Constructor-based setup (no `[SetUp]` methods)
- `IDisposable` for cleanup
- Required by the assignment specification

**Why EF Core InMemory (not real SQLite in tests):**
- **Speed**: Tests run in milliseconds
- **Isolation**: Each test gets a fresh database (unique database name per test class)
- **No file cleanup**: No `.db` files to manage

**Why Moq for ILogger:**
- Logger is a dependency but we don't need to verify log calls in most tests
- Mock satisfies the constructor requirement without complex setup

---

## 14. Health Checks: EF Core DbContext Check

**Choice**: Built-in ASP.NET Core health check with `AddDbContextCheck<AppDbContext>()`

**Why:**
- Verifies the database connection is alive
- Standard `/health` endpoint for load balancers and monitoring
- Zero custom code — uses the built-in EF Core health check provider

---

## Summary of Trade-offs

| Decision | Benefit | Trade-off |
|----------|---------|-----------|
| SQLite | Zero infrastructure, clone-and-run | Limited concurrent writes |
| Soft delete | Audit trail, recovery | Table grows indefinitely |
| In-memory rate limiting | Simple, no dependencies | Not distributed, resets on restart |
| Auto-migrate on startup | No manual steps | Not suitable for production pipelines |
| 7-char short codes | Human-friendly | Collision possible at very large scale |
| EF Core InMemory tests | Fast, isolated | Doesn't test SQLite-specific behavior |
| No authentication | Simple to use | Anyone can create/delete links |
