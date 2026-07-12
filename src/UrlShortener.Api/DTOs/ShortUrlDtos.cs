namespace UrlShortener.Api.DTOs;

public record CreateShortUrlRequest
{
    public string Url { get; init; } = string.Empty;
    public string? CustomAlias { get; init; }
}

public record ShortUrlResponse
{
    public Guid Id { get; init; }
    public string OriginalUrl { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public string ShortUrl { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
