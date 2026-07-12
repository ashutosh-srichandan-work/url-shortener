# Engineering Summary

## Plan and Rationale

This URL Shortener was built as a greenfield project using ASP.NET Core 8, following clean/layered architecture to demonstrate production-grade engineering with AI-assisted development.

**Approach**: Incremental, commit-by-commit development with clear separation of concerns across four layers (Api, Application, Domain, Infrastructure).

**Key Decisions**:
- **Layered architecture** over minimal API to demonstrate enterprise patterns and testability
- **SQLite** for zero-infrastructure local development while maintaining EF Core abstractions for future migration
- **FluentValidation** for declarative, testable input validation
- **Serilog** for structured logging with correlation across request lifecycle
- **Rate limiting** using built-in ASP.NET Core middleware (no external dependencies)
- **Soft delete** over hard delete for audit trail and recoverability

## Artifacts

| Artifact | Purpose |
|----------|---------|
| `src/UrlShortener.Api` | Web API host, controllers, middleware, rate limiting |
| `src/UrlShortener.Application` | Business logic, DTOs, validators, service interfaces |
| `src/UrlShortener.Domain` | Entity definitions, repository contracts |
| `src/UrlShortener.Infrastructure` | EF Core context, migrations, repository implementations |
| `tests/UrlShortener.Tests` | Unit tests for services and validators |
| `documentation/Architecture.md` | Architecture overview |
| `README.md` | Setup, API docs, trade-offs |

## Scenarios

### Greenfield: Initial URL Shortener

- **Decomposition**: Entity model → Repository interface → EF Core implementation → Service layer → Controller → Middleware → Validation → Tests
- **Execution**: Each layer built independently, wired via DI; tests written against in-memory database
- **Validation**: Unit tests, Swagger manual testing, build verification

### Brownfield: Adding Pagination and Rate Limiting

- **Decomposition**: Identify existing `GetAllAsync` → Add paginated repository method → Surface through service → Add controller endpoint → Wire rate limiting middleware
- **Execution**: Extended existing contracts without breaking changes; added new DTO for paged response
- **Validation**: Existing tests still pass; new feature integrates with existing patterns

### Ambiguous: Duplicate URL Detection

- **Requirement interpretation**: "Duplicate detection" could mean reject duplicates or return existing. Chose to return existing short URL for identical original URLs (idempotent behavior) while still rejecting duplicate custom aliases (409 Conflict).
- **Rationale**: Idempotent creation is more user-friendly and avoids unnecessary database growth. Custom alias conflicts must be explicit errors since the user has specific intent.
- **Validation**: Existing duplicate alias test still passes; new behavior is additive.

## Risks and Trade-offs

| Risk | Mitigation |
|------|-----------|
| Short code collisions | Cryptographic randomness + uniqueness check loop + DB unique constraint |
| SQLite write contention | Acceptable for single-instance; migrate to SQL Server/PostgreSQL for scale |
| No authentication | Rate limiting provides basic abuse protection; auth can be added as middleware |
| In-memory rate limiting | Resets on restart; use Redis-backed limiter for distributed deployments |
| No caching | Redirect latency is DB-bound; add response caching or Redis for hot codes |
| URL abuse (phishing/malware) | Not addressed in v1; add URL reputation checking in future |

## Assumptions

- Single-instance deployment (no distributed coordination needed)
- SQLite sufficient for demonstration and low-traffic production
- No user authentication required for v1
- Short codes are globally unique (7-character alphanumeric provides ~3.5 trillion combinations)
- UTC timestamps throughout (no timezone handling needed)

## Limitations

- No distributed locking for concurrent short code generation
- No URL preview/safety scanning
- No custom domain support
- No bulk operations API
- Rate limiting is per-instance, not globally distributed
- No API versioning strategy beyond URL prefix

## Future Enhancements

- Redis caching for high-traffic redirect resolution
- Authentication and URL ownership
- API key management for integrations
- URL safety/reputation checking
- Custom domain mapping
- Bulk import/export
- Webhook notifications on click thresholds
- Geographic analytics (IP-based)
