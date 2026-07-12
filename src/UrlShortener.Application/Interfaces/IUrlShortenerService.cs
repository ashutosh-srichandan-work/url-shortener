namespace UrlShortener.Application.Interfaces;

public interface IUrlShortenerService
{
    Task<CreateShortUrlResult> CreateShortUrlAsync(string url, string? customAlias, string baseUrl);
    Task<string?> GetOriginalUrlAsync(string shortCode);
    Task<ShortUrlDetails?> GetUrlDetailsAsync(Guid id);
}

public class CreateShortUrlResult
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class ShortUrlDetails
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}