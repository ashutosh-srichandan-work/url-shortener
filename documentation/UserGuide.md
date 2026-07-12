# User Guide

## What is URL Shortener?

URL Shortener is a web service that converts long URLs into short, shareable links. When someone visits the short link, they are automatically redirected to the original URL.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed on your machine

### Running the Application

**Option 1 — From the solution root directory:**
```bash
dotnet run --project src/UrlShortener.Api
```

**Option 2 — Navigate to the API project first:**
```bash
cd src/UrlShortener.Api
dotnet run
```

The server starts at `http://localhost:5073`. Open `http://localhost:5073/swagger` in your browser to access the interactive API documentation.

> **Note:** The database is created and migrated automatically on first run. No manual setup required.

---

## How to Use

### 1. Create a Short Link

Send a POST request to `/api/v1/links` with the URL you want to shorten.

**Using Swagger UI:**
1. Open `http://localhost:5073/swagger`
2. Expand `POST /api/v1/links`
3. Click **Try it out**
4. Enter the request body:
```json
{
  "url": "https://www.example.com/some/very/long/path",
  "customAlias": "my-link",
  "expiresAtUtc": "2025-12-31T23:59:59Z"
}
```
5. Click **Execute**

**Using curl:**
```bash
curl -X POST http://localhost:5073/api/v1/links \
  -H "Content-Type: application/json" \
  -d '{"url": "https://www.example.com", "customAlias": "my-link"}'
```

**Request fields:**
| Field | Required | Description |
|-------|----------|-------------|
| `url` | Yes | The original URL (must be valid HTTP or HTTPS) |
| `customAlias` | No | A custom short code (3-50 chars, letters/numbers/hyphens/underscores). If omitted, a random 7-character code is generated. |
| `expiresAtUtc` | No | Expiration date in UTC. If omitted, the link never expires. |

### 2. Use the Short Link

Copy the `shortUrl` from the response and open it in your browser:

```
http://localhost:5073/my-link
```

You will be automatically redirected to the original URL. Each visit is tracked for analytics.

> ⚠️ **The redirect endpoint cannot be tested from Swagger UI** due to browser CORS restrictions. Always test redirects by opening the URL directly in your browser's address bar.

### 3. List All Links

```
GET http://localhost:5073/api/v1/links?page=1&pageSize=10
```

Returns a paginated list of all active (non-deleted) short URLs, ordered by newest first.

### 4. View Link Details

```
GET http://localhost:5073/api/v1/links/{id}
```

Replace `{id}` with the GUID returned when you created the link.

### 5. View Analytics

```
GET http://localhost:5073/api/v1/links/{id}/stats
```

Returns click count, last accessed time, creation time, and current status (Active/Expired/Deleted).

### 6. Delete a Link

```
DELETE http://localhost:5073/api/v1/links/{id}
```

This performs a **soft delete** — the link is marked as deleted and will no longer redirect, but the record is preserved in the database.

### 7. Health Check

```
GET http://localhost:5073/health
```

Returns `Healthy` if the API and database are operational.

---

## Error Responses

All errors follow the RFC 7807 Problem Details format:

| HTTP Status | Meaning |
|-------------|---------|
| `400` | Invalid input (bad URL format, alias too short, past expiration date) |
| `404` | Link not found, expired, or deleted |
| `409` | Custom alias already taken |
| `429` | Rate limit exceeded (wait and retry) |
| `500` | Unexpected server error |

---

## Rate Limits

To prevent abuse, the API enforces rate limits:

| Scope | Limit |
|-------|-------|
| All management endpoints | 100 requests per minute |
| Create link (`POST`) | 20 requests per minute |

The redirect endpoint (`/{shortCode}`) is **not** rate-limited.

---

## Running Tests

From the solution root:
```bash
dotnet test
```

This runs all unit tests for service logic and input validation.
