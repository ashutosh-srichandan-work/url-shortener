namespace UrlShortener.Api.DTOs;

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
    public string ShortCode { get; init; } = string.Empty;
    public string ShortUrl { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public int ClickCount { get; init; }
    public DateTime? LastAccessedAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
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
