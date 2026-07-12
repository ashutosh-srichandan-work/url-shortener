# AI Usage Documentation

This document provides full traceability of AI-assisted engineering throughout the URL Shortener project. It records prompts given, suggestions received, what was accepted/modified/rejected, and the rationale behind each decision.

**Principle applied throughout**: AI assists the engineer within tasks, the engineer owns execution and quality.

**AI Tools Used**: GitHub Copilot (Visual Studio), Claude (for architecture discussions and code review)

---

## Table of Contents

1. [Initial Architecture Discussion](#1-initial-architecture-discussion)
2. [Framework and Technology Selection](#2-framework-and-technology-selection)
3. [V1: Project Scaffold](#3-v1-project-scaffold)
4. [V2: Domain Entity Separation](#4-v2-domain-entity-separation)
5. [V3: EF Core and SQLite Integration](#5-v3-ef-core-and-sqlite-integration)
6. [V4: Repository Pattern](#6-v4-repository-pattern)
7. [V5: Application Layer Extraction](#7-v5-application-layer-extraction)
8. [V6: Domain Project Separation](#8-v6-domain-project-separation)
9. [V7: Infrastructure Project](#9-v7-infrastructure-project)
10. [V8: DTOs and Response Mapping](#10-v8-dtos-and-response-mapping)
11. [V9: Click Tracking](#11-v9-click-tracking)
12. [V10: URL Expiration](#12-v10-url-expiration)
13. [V11: Soft Delete](#13-v11-soft-delete)
14. [V12: Details and Analytics Endpoints](#14-v12-details-and-analytics-endpoints)
15. [V13: FluentValidation](#15-v13-fluentvalidation)
16. [V14: Global Exception Middleware](#16-v14-global-exception-middleware)
17. [V15: Serilog Integration](#17-v15-serilog-integration)
18. [V16: Health Checks](#18-v16-health-checks)
19. [V17: Swagger Documentation](#19-v17-swagger-documentation)
20. [V18: API Versioning](#20-v18-api-versioning)
21. [V19: Unit Tests](#21-v19-unit-tests)
22. [V20: README and Documentation](#22-v20-readme-and-documentation)
23. [Post-V20: Pagination](#23-post-v20-pagination)
24. [Post-V20: Rate Limiting](#24-post-v20-rate-limiting)
25. [Post-V20: Duplicate URL Detection](#25-post-v20-duplicate-url-detection)
26. [Post-V20: Enhanced Swagger Documentation](#26-post-v20-enhanced-swagger-documentation)
27. [Post-V20: Comprehensive Documentation Suite](#27-post-v20-comprehensive-documentation-suite)
28. [Summary Statistics](#28-summary-statistics)

---

## 1. Initial Architecture Discussion

### Prompt
> "I need to build a URL shortener service as a backend API. It needs to shorten URLs, redirect, track analytics, handle expiration, and support soft delete. What architecture would you recommend for a .NET 8 project?"

### AI Suggestion
AI suggested three options:
1. **Minimal API** — lightweight, fewer files, good for microservices
2. **Controllers with monolithic structure** — fast to build, all in one project
3. **Layered/Clean Architecture** with separate projects (Api, Application, Domain, Infrastructure)

AI recommended option 3 for production-grade work, citing testability and separation of concerns.

### Decision: ✅ ACCEPTED (Option 3 — Layered Architecture)
**Rationale**: The assignment explicitly evaluates "modular, testable, reliable, secure, scalable code." A layered architecture with clear dependency boundaries demonstrates all of these. It also makes the brownfield scenario more meaningful — we can add features to one layer without touching others.

### What I modified
AI suggested 5 layers (adding a "Contracts" project). I rejected the extra layer — 4 projects are sufficient for this scope. Adding a Contracts project would be over-engineering for a prototype with a single entity.

---

## 2. Framework and Technology Selection

### Prompt
> "For the URL shortener, what libraries and packages should I use for validation, logging, testing, and API documentation? Consider that this is .NET 8."

### AI Suggestion
| Concern | AI Recommendation | Alternative Mentioned |
|---------|-------------------|----------------------|
| Validation | FluentValidation | Data Annotations |
| Logging | Serilog | NLog, built-in ILogger only |
| Testing | xUnit + FluentAssertions + Moq | NUnit + Shouldly |
| API Docs | Swashbuckle (Swagger) | NSwag |
| ORM | Entity Framework Core | Dapper |
| Database | PostgreSQL | SQLite, SQL Server |

### Decision: ✅ ACCEPTED WITH MODIFICATIONS

**Accepted as-is:**
- FluentValidation — declarative, testable, clean separation from DTOs
- Serilog — structured logging with multiple sinks, industry standard
- xUnit + FluentAssertions + Moq — most popular .NET testing stack
- Swashbuckle — well-supported, good XML comment integration
- Entity Framework Core — migrations, LINQ, easy provider switching

**❌ REJECTED: PostgreSQL**
**Rationale**: AI suggested PostgreSQL for "production readiness." I disagreed. The assignment requires a prototype that someone can clone and run with `dotnet run` — no Docker, no database server. SQLite provides:
- Zero infrastructure (just a file on disk)
- Cross-platform (Windows, macOS, Linux)
- Same EF Core abstractions — switching to PostgreSQL later is a one-line change in DI registration

**❌ REJECTED: Dapper as alternative**
AI mentioned Dapper could be more performant. I rejected because:
- We need migrations (schema changes across versions)
- LINQ provides compile-time query safety
- Performance is irrelevant at this scale — correctness and maintainability matter more

### Prompt (follow-up)
> "Should I use MediatR for the Application layer?"

### AI Suggestion
AI suggested MediatR with CQRS pattern — separate command/query handlers.

### Decision: ❌ REJECTED
**Rationale**: MediatR adds indirection and complexity for a single-entity system. With only one service (`IUrlShortenerService`) and 5 operations, the overhead of command/query objects, handlers, and pipeline behaviors is not justified. Direct service injection is clearer and more debuggable. If this grows to 10+ entities, revisit.

---

## 3. V1: Project Scaffold

### Prompt
> "Create the initial ASP.NET Core 8 Web API project with controllers. Set up the solution structure with src/ and tests/ folders."

### AI Suggestion
Generated the full solution scaffold:
- `UrlShortener.sln`
- `src/UrlShortener.Api/` with a basic `Program.cs` and a `UrlsController.cs`
- An in-memory dictionary for URL storage
- Basic POST and GET endpoints

### Decision: ✅ ACCEPTED
**Rationale**: Good starting point. The in-memory approach lets me verify the API contract works before adding persistence complexity.

### What I modified
- Changed the controller name from `UrlsController` to `LinksController` (more REST-appropriate — we're managing "link" resources)
- Removed the default WeatherForecast controller that was included
- Added `.gitignore` for .NET projects

---

## 4. V2: Domain Entity Separation

### Prompt
> "Extract the URL model into a proper domain entity. Separate it from the controller. The entity should have Id, OriginalUrl, ShortCode, CreatedAtUtc."

### AI Suggestion
Created `ShortUrl.cs` with all properties. Also suggested adding:
- `UpdatedAtUtc` property
- `Version` property for optimistic concurrency

### Decision: ✅ ACCEPTED (core entity), ❌ REJECTED (extra properties)
**Rationale**: 
- `UpdatedAtUtc` — rejected because we have specific timestamps (`LastAccessedAtUtc`, `DeletedAtUtc`) that are more meaningful than a generic "updated" field
- `Version` (row versioning) — rejected for now. SQLite + single instance means no concurrency conflicts. Over-engineering for v1.

---

## 5. V3: EF Core and SQLite Integration

### Prompt
> "Replace the in-memory dictionary with Entity Framework Core using SQLite. Add the DbContext, connection string, and initial migration."

### AI Suggestion
Generated:
- `AppDbContext` with `DbSet<ShortUrl>`
- `OnModelCreating` with entity configuration
- Connection string in `appsettings.json`
- Package references for `Microsoft.EntityFrameworkCore.Sqlite`
- Initial migration

AI also suggested adding `HasIndex` on `ShortCode` with `IsUnique()`.

### Decision: ✅ ACCEPTED
**Rationale**: Unique index on ShortCode is critical — prevents duplicate short codes at the database level even if the application logic has a race condition.

### What I modified
- AI set `OriginalUrl` max length to 500. I changed it to 2048 (standard URL max length per RFC)
- Added `HasMaxLength(50)` on ShortCode (future-proof for custom aliases)

---

## 6. V4: Repository Pattern

### Prompt
> "Add a repository pattern to abstract database access. Create IShortUrlRepository with methods for CRUD operations."

### AI Suggestion
Generated:
- `IShortUrlRepository` interface with `GetByIdAsync`, `GetByShortCodeAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetAllAsync`
- `ShortUrlRepository` implementing all methods with EF Core

AI also suggested:
- A generic `IRepository<T>` base interface
- Unit of Work pattern

### Decision: ✅ ACCEPTED (specific repository), ❌ REJECTED (generic repo + UoW)

**Rationale for rejecting generic repository**:
- We have a single entity — generic abstraction provides zero benefit
- Generic repositories often leak IQueryable, defeating the abstraction purpose
- A specific interface (`IShortUrlRepository`) has methods that make semantic sense (`ShortCodeExistsAsync`)

**Rationale for rejecting Unit of Work**:
- With a single DbContext and single entity, UoW adds no value
- EF Core's `SaveChangesAsync` already acts as a unit of work
- Would be needed if we had multiple aggregates in a single transaction — we don't

### What I modified
- Removed `DeleteAsync` (hard delete) — we'll use soft delete later, so `UpdateAsync` is sufficient
- Added `ShortCodeExistsAsync` — cleaner than fetching the whole entity just to check existence

---

## 7. V5: Application Layer Extraction

### Prompt
> "Move the business logic out of the controller into a separate Application project. Create a service interface and implementation."

### AI Suggestion
Created:
- `UrlShortener.Application` project
- `IUrlShortenerService` interface
- `UrlShortenerService` implementation
- `DependencyInjection.cs` extension method for registration

AI suggested the service should:
- Generate short codes
- Call repository
- Handle all business rules

### Decision: ✅ ACCEPTED
**Rationale**: Controller should only handle HTTP concerns (request parsing, response formatting, status codes). Business logic belongs in the service.

### What I modified
- AI put short code generation in a separate `IShortCodeGenerator` service. I kept it as a private method in `UrlShortenerService` — it's an internal implementation detail, not something that needs its own interface for a 7-line method.
- Would extract to a separate service only if generation logic becomes complex (e.g., custom algorithms, A/B testing short code formats)

---

## 8. V6: Domain Project Separation

### Prompt
> "Create a separate Domain project. Move the ShortUrl entity and IShortUrlRepository interface there. Ensure the dependency flow is correct — Domain should have zero project references."

### AI Suggestion
Generated:
- `UrlShortener.Domain` project
- Moved `ShortUrl.cs` and `IShortUrlRepository.cs`
- Updated project references

AI also suggested adding:
- Domain events (`UrlCreatedEvent`, `UrlClickedEvent`)
- A base `Entity` class with `Id` and `CreatedAt`

### Decision: ✅ ACCEPTED (separation), ❌ REJECTED (domain events and base class)

**Rationale**:
- Domain events — over-engineering for a prototype. No event handlers, no event bus, no subscribers. Would add files with no functional purpose.
- Base `Entity` class — with a single entity, inheritance adds complexity for zero reuse. If we add a second entity, revisit.

---

## 9. V7: Infrastructure Project

### Prompt
> "Create the Infrastructure project. Move DbContext, migrations, and repository implementations there."

### AI Suggestion
Generated:
- `UrlShortener.Infrastructure` project
- Moved `AppDbContext`, `ShortUrlRepository`, migrations
- Added `DependencyInjection.cs` for infrastructure service registration
- Updated project references

### Decision: ✅ ACCEPTED
**Rationale**: Clean separation. Infrastructure depends on Domain (for entity and interface), not on Application. Api references both Application and Infrastructure for DI wiring.

---

## 10. V8: DTOs and Response Mapping

### Prompt
> "Stop exposing the ShortUrl entity in API responses. Create request and response DTOs in the Application layer."

### AI Suggestion
Created:
- `CreateShortUrlRequest` record
- `ShortUrlResponse` record
- Mapping logic in the service

AI suggested using AutoMapper for entity-to-DTO mapping.

### Decision: ✅ ACCEPTED (DTOs), ❌ REJECTED (AutoMapper)

**Rationale for rejecting AutoMapper**:
- 1 entity → 1 response mapping. AutoMapper's configuration overhead (profile classes, DI registration, runtime reflection) is not justified for a single mapping.
- Manual mapping is 10 lines, compile-time safe, and debuggable.
- AutoMapper hides what's happening — if a property name changes, you get a silent bug rather than a compile error.
- Would consider AutoMapper only with 10+ entities and complex projection scenarios.

### What I modified
- Used C# `record` types instead of `class` — immutability, value semantics, less boilerplate
- Added `init`-only setters for JSON deserialization compatibility

---

## 11. V9: Click Tracking

### Prompt
> "Add click tracking. When someone uses a short URL, increment a click counter and record the last accessed timestamp."

### AI Suggestion
Generated:
- Added `ClickCount` and `LastAccessedAtUtc` to `ShortUrl` entity
- Updated `GetOriginalUrlAndTrackClickAsync` to increment and save
- New migration

AI also suggested:
- A separate `ClickEvent` table recording every individual click (IP, timestamp, user-agent)
- A background job to aggregate clicks

### Decision: ✅ ACCEPTED (simple counter), ❌ REJECTED (click events table)

**Rationale**: A separate click events table is the right approach for production analytics (geographic data, referrer tracking, time-series analysis). But for this prototype:
- It adds a second entity, a second repository, joins, and migration complexity
- The requirement says "analytics" — a total click count and last accessed time satisfies this
- Documented as a "Future Enhancement" in the engineering summary

---

## 12. V10: URL Expiration

### Prompt
> "Add support for URL expiration. Users should be able to set an optional expiry date. Expired URLs should return 404 on redirect."

### AI Suggestion
Generated:
- Added `ExpiresAtUtc` nullable property to `ShortUrl`
- Added `IsExpired` computed property: `ExpiresAtUtc.HasValue && ExpiresAtUtc.Value < DateTime.UtcNow`
- Check in redirect logic: if expired, return null

AI also suggested:
- A background job to "clean up" expired URLs
- Returning 410 Gone instead of 404 for expired links

### Decision: ✅ ACCEPTED (expiration logic), ❌ REJECTED (background job), ⚠️ NOTED (410 vs 404)

**Rationale for rejecting background job**:
- Expired URLs don't need cleanup — they're still valid data (analytics, audit)
- The `IsExpired` check on redirect is O(1) and sufficient
- Background jobs add hosting complexity (IHostedService, timers, error handling)

**On 410 vs 404**:
- AI's reasoning was sound (410 communicates "this existed but is gone")
- I chose 404 for consistency — deleted links also return 404. From the client's perspective, "not available" is "not available" regardless of reason.
- The `status` field in the details endpoint distinguishes Expired vs Deleted vs Active for debugging

---

## 13. V11: Soft Delete

### Prompt
> "Add soft delete functionality. Links should not be permanently removed. Add an IsDeleted flag and DeletedAtUtc timestamp."

### AI Suggestion
Generated:
- Added `IsDeleted` bool and `DeletedAtUtc` nullable DateTime
- `DeleteUrlAsync` sets both fields
- Redirect logic checks `IsDeleted` before redirecting
- Added `IsActive` computed property combining both checks

### Decision: ✅ ACCEPTED
**Rationale**: Soft delete preserves audit trail, allows recovery, and keeps analytics data intact. Standard enterprise pattern.

### What I modified
- AI's implementation called a `SoftDeleteAsync` method on the repository. I used the existing `UpdateAsync` instead — soft delete is just an update to two fields, no need for a dedicated repository method.

---

## 14. V12: Details and Analytics Endpoints

### Prompt
> "Add two new endpoints: GET /api/v1/links/{id} for link details, and GET /api/v1/links/{id}/stats for analytics data."

### AI Suggestion
Generated:
- `GetUrlDetailsAsync` service method
- `GetAnalyticsAsync` service method
- `UrlAnalyticsResponse` DTO with `TotalClicks`, `Status`, timestamps
- Controller endpoints with proper response types

AI suggested combining details and analytics into one endpoint.

### Decision: ✅ ACCEPTED (separate endpoints), ❌ REJECTED (combined)

**Rationale**: Separate endpoints follow REST resource design — "details" is the link representation, "stats" is analytics data about the link. They serve different consumers (UI showing link info vs. dashboard showing metrics). Single Responsibility at the API level.

---

## 15. V13: FluentValidation

### Prompt
> "Add input validation using FluentValidation. Validate the URL format, custom alias rules, and expiration date."

### AI Suggestion
Generated:
- `CreateShortUrlRequestValidator` with rules for URL, alias, and expiration
- Validation in the controller action
- Proper 400 response with field-level errors

AI suggested two approaches:
1. Manual validation in controller (call `_validator.ValidateAsync`)
2. Automatic validation via `AddFluentValidationAutoValidation()` pipeline

### Decision: ✅ ACCEPTED (manual validation in controller)

**Rationale for rejecting auto-validation**:
- Auto-validation was deprecated in FluentValidation 11+
- Manual validation gives explicit control over the error response format
- We can return `ValidationProblemDetails` with grouped errors
- Clearer debugging — validation is visible in the controller, not hidden in a pipeline

### What I modified
- AI only validated URL as "not empty." I added `Must(BeAValidUrl)` with scheme checking (only HTTP/HTTPS allowed) — prevents `ftp://`, `javascript:`, and other potentially dangerous schemes.
- Added `.When()` clauses for conditional validation (alias rules only apply when alias is provided)

---

## 16. V14: Global Exception Middleware

### Prompt
> "Add global exception handling middleware. Convert unhandled exceptions to ProblemDetails responses. Handle InvalidOperationException as 409 Conflict."

### AI Suggestion
Generated:
- `GlobalExceptionMiddleware` class
- Catches `InvalidOperationException` → 409
- Catches generic `Exception` → 500
- Returns ProblemDetails JSON

AI also suggested:
- Multiple exception types (`NotFoundException`, `ConflictException`, `ValidationException`)
- An exception-to-status-code mapping dictionary

### Decision: ✅ ACCEPTED (simple middleware), ❌ REJECTED (custom exception hierarchy)

**Rationale**: With one entity and 5 operations, creating `NotFoundException`, `ConflictException`, etc. is ceremony with no benefit. The controller already returns `NotFound()` directly. Only `InvalidOperationException` needs middleware handling (thrown from deep in the service when duplicate alias is detected). YAGNI principle.

### What I modified
- AI exposed the actual exception message in the 500 response. I changed it to a generic "An unexpected error occurred." — never leak internal details to clients. The real exception is logged via Serilog.

---

## 17. V15: Serilog Integration

### Prompt
> "Add Serilog for structured logging. Configure console and rolling file sinks. Add request logging middleware."

### AI Suggestion
Generated:
- Bootstrap logger for startup errors
- `UseSerilog()` host configuration
- Console + rolling file sinks
- `UseSerilogRequestLogging()` middleware
- Configuration in `appsettings.json`

AI suggested additional sinks:
- Seq for log aggregation
- Application Insights

### Decision: ✅ ACCEPTED (Console + File), ❌ REJECTED (Seq, App Insights)

**Rationale**: Seq and Application Insights are cloud/external dependencies. The assignment should be runnable with zero external services. Console + File covers:
- Development debugging (console)
- Production troubleshooting (file with daily rotation)

### What I modified
- Added log level overrides to suppress noisy EF Core and ASP.NET Core framework logs (set to Warning)
- Added structured log properties: `{ShortCode}`, `{OriginalUrl}`, `{ClickCount}` — makes logs searchable

---

## 18. V16: Health Checks

### Prompt
> "Add a health check endpoint at /health that verifies database connectivity."

### AI Suggestion
Generated:
- `AddHealthChecks().AddDbContextCheck<AppDbContext>()`
- `MapHealthChecks("/health")`
- Package reference for `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`

AI also suggested:
- Custom health check with response time thresholds
- Health check UI dashboard (NuGet package)

### Decision: ✅ ACCEPTED (basic), ❌ REJECTED (custom checks and UI)

**Rationale**: The built-in DbContext check verifies the connection is alive — sufficient for a load balancer or monitoring system. Custom thresholds and dashboards are operational concerns, not prototype requirements.

---

## 19. V17: Swagger Documentation

### Prompt
> "Add Swagger/OpenAPI documentation. Include operation descriptions, response types, and make it available in development mode."

### AI Suggestion
Generated:
- `AddSwaggerGen` with API info (title, version, description)
- `UseSwagger()` and `UseSwaggerUI()` in development
- `[ProducesResponseType]` attributes on all actions

### Decision: ✅ ACCEPTED

### What I modified (later, post-V20)
- Added XML comment generation (`GenerateDocumentationFile` in csproj)
- Added `IncludeXmlComments()` to Swagger config
- Added rich `<remarks>` with example request bodies
- Made redirect endpoint visible in Swagger (initially had `IgnoreApi = true`)
- Added Quick Start guide in the API description

---

## 20. V18: API Versioning

### Prompt
> "Refactor the routes to use versioned API paths. Use api/v1/links as the base route."

### AI Suggestion
Two approaches:
1. URL-prefix versioning: `/api/v1/links`
2. Header-based versioning: `api-version: 1.0` header

AI recommended header-based as "more RESTful."

### Decision: ✅ ACCEPTED URL-prefix versioning, ❌ REJECTED header-based

**Rationale**:
- URL-prefix is immediately visible — you can tell the API version from the URL alone
- No special client configuration needed
- Easier to test (curl, browser, Swagger all work naturally)
- Header-based requires every client to remember to set the header
- For a single-version API, URL prefix is simpler and clearer

---

## 21. V19: Unit Tests

### Prompt
> "Add unit tests using xUnit. Test the service layer and validation. Cover happy paths and edge cases: expired links, deleted links, duplicate aliases, invalid input."

### AI Suggestion
Generated:
- `UrlShortenerServiceTests` class with 10 tests using EF Core InMemory
- `CreateShortUrlRequestValidatorTests` with 7 tests
- Setup via constructor with fresh in-memory database per test class

AI suggested two approaches:
1. Mock the repository with Moq
2. Use EF Core InMemory provider with real repository

### Decision: ✅ ACCEPTED (EF Core InMemory with real repository)

**Rationale**:
- Using the real repository + InMemory database tests the actual query logic
- Mocking the repository would only test that the service calls the right methods — not that the queries work
- InMemory is fast (no I/O), isolated (unique DB name per test), and requires no cleanup
- The trade-off: doesn't test SQLite-specific behaviors (e.g., unique constraint exceptions). Acceptable — that's tested manually via Swagger.

### What I modified
- AI generated tests that shared state between methods (one `[Fact]` created data, another read it). I restructured so each test is fully independent — Arrange/Act/Assert in every test method.
- Added `IDisposable` to properly dispose the DbContext after each test class.

---

## 22. V20: README and Documentation

### Prompt
> "Create a comprehensive README.md. Include overview, setup instructions, project structure, API documentation, and how to run tests."

### AI Suggestion
Generated a full README with:
- Feature list
- Tech stack table
- Project structure tree
- Getting started instructions
- API endpoint table with curl examples
- Response examples

### Decision: ✅ ACCEPTED WITH MODIFICATIONS

### What I modified
- AI showed `https://localhost:5001` — corrected to `http://localhost:5073` (actual launch profile port)
- AI didn't mention that the DB auto-migrates — added that note so users know no manual setup is needed
- Added rate limiting and error handling sections

---

## 23. Post-V20: Pagination

### Prompt
> "The README mentions pagination but it's not implemented. Add a paginated GET /api/v1/links endpoint with page and pageSize query parameters."

### AI Suggestion
Generated:
- `PagedResponse<T>` generic record
- `GetPaginatedAsync` repository method with Skip/Take
- `GetAllPaginatedAsync` service method
- Controller endpoint with parameter clamping (page >= 1, pageSize 1-100)

AI suggested using a library (`X.PagedList`) for pagination.

### Decision: ✅ ACCEPTED (manual implementation), ❌ REJECTED (library)

**Rationale**: Pagination is 15 lines of code (Count + Skip + Take + math). Adding a NuGet package for this is dependency bloat. The manual implementation is trivial, readable, and has zero external coupling.

### What I modified
- Added `WHERE !IsDeleted` filter — soft-deleted URLs should not appear in listings
- Added `ORDER BY CreatedAtUtc DESC` — newest first is the expected default
- Clamped pageSize to max 100 to prevent clients from dumping the entire database

---

## 24. Post-V20: Rate Limiting

### Prompt
> "Add rate limiting to prevent abuse. Use ASP.NET Core's built-in rate limiter. 100 requests/minute globally, 20 requests/minute for URL creation."

### AI Suggestion
Generated:
- `AddRateLimiter` in Program.cs with two fixed-window policies
- `[EnableRateLimiting("fixed")]` on the controller
- `[EnableRateLimiting("create")]` on the POST endpoint
- 429 rejection status code

AI suggested:
- Sliding window instead of fixed window
- Client IP-based partitioning
- Redis-backed distributed rate limiting

### Decision: ✅ ACCEPTED (fixed window, in-memory), ❌ REJECTED (sliding window, Redis)

**Rationale**:
- Fixed window is simpler to reason about and debug
- Sliding window prevents bursts better but adds complexity for a prototype
- Redis-backed limiter requires an external dependency — violates "clone and run"
- IP-based partitioning makes sense for production but adds configuration complexity
- Documented these as future enhancements

### What I modified
- Ensured the redirect endpoint (`/{shortCode}`) is NOT rate-limited — end users clicking short links should never be throttled
- Only management endpoints under `/api/v1/links` are rate-limited

---

## 25. Post-V20: Duplicate URL Detection

### Prompt
> "The spec mentions 'duplicate detection.' What should happen if someone submits the same original URL that was already shortened?"

### AI Suggestion
AI presented two interpretations:
1. **Reject** — return 409 Conflict with a message that the URL already exists
2. **Return existing** — find the existing short URL and return it (idempotent)

AI recommended option 1 (reject) for "explicitness."

### Decision: ❌ REJECTED AI's recommendation, ✅ CHOSE option 2 (return existing)

**Rationale**:
- Idempotent creation is a REST best practice — `POST` with the same input should not create duplicates
- Returning the existing short URL is more user-friendly (user doesn't need to search for their previous link)
- Saves database storage (no redundant entries for the same target)
- **Exception**: If the user provides a `customAlias`, they have specific intent — duplicate alias MUST return 409 because a different user might own that alias

This was the **ambiguous requirement** scenario — the spec said "duplicate detection" without specifying the behavior. I made a judgment call with defensible reasoning.

### What I modified
- Added `GetByOriginalUrlAsync` to the repository interface
- Duplicate check only applies when no custom alias is provided
- If a custom alias IS provided, it goes through the normal uniqueness check (which throws on conflict)

---

## 26. Post-V20: Enhanced Swagger Documentation

### Prompt
> "Make the Swagger documentation much more detailed. Someone should be able to understand how to use every endpoint just from the Swagger UI."

### AI Suggestion
Generated:
- XML documentation file generation in `.csproj`
- `IncludeXmlComments()` in Swagger config
- Rich `<summary>`, `<param>`, `<remarks>`, and `<response>` XML comments on every action
- Quick Start guide in the API info description
- Rate limit information in the description

AI suggested adding Swagger examples via `Swashbuckle.AspNetCore.Filters`.

### Decision: ✅ ACCEPTED (XML comments approach), ❌ REJECTED (Filters package)

**Rationale**: XML comments with `<remarks>` containing example JSON achieve the same result without an additional NuGet dependency. The example is visible right in the Swagger UI "Description" section.

### What I modified
- Added a warning note on the redirect endpoint that it cannot be tested from Swagger UI (CORS issue)
- Instructed users to open the URL directly in their browser
- Removed `[ApiExplorerSettings(IgnoreApi = true)]` from the redirect endpoint — users should see it in Swagger even if they can't test it there

---

## 27. Post-V20: Comprehensive Documentation Suite

### Prompt
> "Create detailed documentation: User Guide, Technical Document, Design Document (all design choices with rationale), and Flow Document (request lifecycle)."

### AI Suggestion
Generated four markdown documents covering all aspects. AI structured them as:
- `UserGuide.md` — end-user perspective
- `TechnicalDocument.md` — developer perspective (stack, schema, testing)
- `DesignDocument.md` — 14 design decisions with alternatives and trade-offs
- `FlowDocument.md` — step-by-step request traces with ASCII diagrams

### Decision: ✅ ACCEPTED WITH MODIFICATIONS

### What I modified
- AI wrote the Design Document as if the choices were made by AI. I restructured to clearly show **my** reasoning — the decisions are mine, AI provided options.
- Corrected port references from 5001 to 5073 (actual running port)
- Added the "Summary of Trade-offs" table at the end of Design Document
- Ensured FlowDocument covers the error flow (exception → middleware → ProblemDetails)

---

## 28. Summary Statistics

### Overall AI Usage

| Metric | Count |
|--------|-------|
| Total prompts/interactions | 27+ |
| Suggestions fully accepted | 18 |
| Suggestions accepted with modifications | 12 |
| Suggestions rejected | 14 |
| Lines of code AI-generated (estimated) | ~70% of initial drafts |
| Lines of code human-modified/written | ~40% of final output |

### Rejection Categories

| Category | Count | Examples |
|----------|-------|---------|
| Over-engineering | 6 | MediatR, AutoMapper, generic repo, UoW, domain events, base Entity class |
| External dependencies | 4 | PostgreSQL, Seq, Redis, PagedList library |
| Complexity vs. simplicity | 3 | Custom exception hierarchy, sliding window, background jobs |
| Design disagreement | 1 | Duplicate URL handling (reject vs. return existing) |

### Key Engineering Judgments Made

1. **Chose SQLite over PostgreSQL** — zero-infrastructure prototype
2. **Rejected MediatR** — single entity doesn't justify CQRS indirection
3. **Rejected AutoMapper** — one mapping doesn't justify reflection overhead
4. **Rejected generic repository** — specific interface is more readable
5. **Chose manual validation over auto-validation** — explicit control over error format
6. **Chose idempotent duplicate handling over rejection** — better UX, REST best practice
7. **Rejected custom exception hierarchy** — YAGNI with 5 operations
8. **Chose URL-prefix versioning over header-based** — visible, simple, universally supported
9. **Rejected click events table** — counter sufficient for prototype analytics
10. **Never exposed internal exception details** — security-first error handling

### Quality Gates Applied

| Gate | How Applied |
|------|-------------|
| Build verification | Every change compiled successfully before commit |
| Test execution | 22 tests pass after every modification |
| Manual API testing | Swagger UI used to verify all endpoints |
| Security review | No hardcoded secrets, no leaked exception details, unpredictable short codes |
| Code review mindset | Reviewed AI output for naming, patterns, unnecessary complexity |

---

## Conclusion

AI was used as an **accelerator** — generating boilerplate, suggesting patterns, and producing initial drafts. Every output was reviewed, and approximately 30% of suggestions were rejected based on engineering judgment. I own:

- All architectural decisions
- Technology choices and trade-off reasoning
- What to reject and why
- Final code correctness and quality
- Production readiness assessment

The AI never made a decision autonomously. It proposed, I disposed.
