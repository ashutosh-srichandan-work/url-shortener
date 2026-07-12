namespace UrlShortener.Application.DTOs;

public record CreateShortUrlRequest
{
    public string Url { get; init; } = string.Empty;
    public string? CustomAlias { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}

public record ShortUrlResponse
{
    public Guid Id { get; init; }
    public string OriginalUrl { get; init; } = string.Empty;
    public string ShortUrl { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public int ClickCount { get; init; }
    public DateTime? LastAccessedAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

public record UrlAnalyticsResponse
{
    public Guid Id { get; init; }
    public string OriginalUrl { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public int TotalClicks { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastAccessedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
}
