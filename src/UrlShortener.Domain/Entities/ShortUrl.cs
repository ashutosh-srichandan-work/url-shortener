namespace UrlShortener.Domain.Entities;

public class ShortUrl
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int ClickCount { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
}
