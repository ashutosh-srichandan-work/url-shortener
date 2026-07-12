# Architecture

## Layering

```
┌─────────────────────────┐
│   UrlShortener.Api      │  Controllers, Middleware, Program.cs
├─────────────────────────┤
│ UrlShortener.Application│  Services, DTOs, Validators, Interfaces
├─────────────────────────┤
│   UrlShortener.Domain   │  Entities, Repository Interfaces
├─────────────────────────┤
│UrlShortener.Infrastructure│ DbContext, Migrations, Repository Implementations
└─────────────────────────┘
```

## Dependency Flow

- **Api** depends on Application and Infrastructure (for DI registration)
- **Application** depends on Domain only
- **Infrastructure** depends on Domain only
- **Domain** has no project dependencies

This ensures business logic is testable without infrastructure concerns.

## Request Lifecycle

1. HTTP request arrives at controller
2. FluentValidation validates the request DTO
3. Controller calls Application service
4. Service executes business logic, calls repository interface
5. Infrastructure repository performs EF Core operations
6. Response DTO returned through the stack
7. Global exception middleware catches unhandled errors → ProblemDetails

## Database Design

### ShortUrls Table

| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | Primary Key |
| OriginalUrl | string(2048) | Required |
| ShortCode | string(50) | Required, Unique Index |
| CreatedAtUtc | DateTime | Required |
| ExpiresAtUtc | DateTime? | Nullable |
| LastAccessedAtUtc | DateTime? | Nullable |
| ClickCount | int | Default 0 |
| IsDeleted | bool | Default false |
| DeletedAtUtc | DateTime? | Nullable |
| CreatedBy | string(256)? | Nullable |

## Extension Points

- **New storage backends**: Implement `IShortUrlRepository` for SQL Server, CosmosDB, etc.
- **Authentication**: Add middleware before controllers
- **Caching**: Decorate repository with caching layer
- **Analytics enrichment**: Extend `ShortUrl` entity with IP, user-agent, referrer
- **Custom domains**: Add domain mapping table and resolution logic
- **Rate limiting**: Add ASP.NET Core rate limiting middleware
