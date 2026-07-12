namespace UrlShortener.Api.Models;

public class ShortenUrlRequest
{
    public string Url { get; set; } = string.Empty;
    public string? CustomAlias { get; set; }
}
