using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Api.Services;

public interface IUrlShortenerService
{
    Task<CreateShortUrlResult> CreateShortUrlAsync(string url, string? customAlias, DateTime? expiresAtUtc, string baseUrl);
    Task<string?> GetOriginalUrlAndTrackClickAsync(string shortCode);
    Task<ShortUrlDetails?> GetUrlDetailsAsync(Guid id);
    Task<ShortUrlAnalytics?> GetAnalyticsAsync(Guid id);
    Task<bool> DeleteUrlAsync(Guid id);
}

public class CreateShortUrlResult
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public class ShortUrlDetails
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int ClickCount { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ShortUrlAnalytics
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int TotalClicks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class UrlShortenerService : IUrlShortenerService
{
    private readonly IShortUrlRepository _repository;

    public UrlShortenerService(IShortUrlRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateShortUrlResult> CreateShortUrlAsync(string url, string? customAlias, DateTime? expiresAtUtc, string baseUrl)
    {
        var shortCode = customAlias ?? GenerateShortCode();

        if (await _repository.ShortCodeExistsAsync(shortCode))
            throw new InvalidOperationException($"The alias '{shortCode}' is already in use.");

        var entity = new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = url,
            ShortCode = shortCode,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        };

        await _repository.CreateAsync(entity);

        return new CreateShortUrlResult
        {
            Id = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ShortCode = entity.ShortCode,
            ShortUrl = $"{baseUrl.TrimEnd('/')}/{entity.ShortCode}",
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc
        };
    }

    public async Task<string?> GetOriginalUrlAndTrackClickAsync(string shortCode)
    {
        var entity = await _repository.GetByShortCodeAsync(shortCode);

        if (entity is null || entity.IsDeleted || entity.IsExpired)
            return null;

        entity.ClickCount++;
        entity.LastAccessedAtUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(entity);

        return entity.OriginalUrl;
    }

    public async Task<ShortUrlDetails?> GetUrlDetailsAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;

        return new ShortUrlDetails
        {
            Id = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ShortCode = entity.ShortCode,
            ShortUrl = entity.ShortCode,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            ClickCount = entity.ClickCount,
            LastAccessedAtUtc = entity.LastAccessedAtUtc,
            Status = GetStatus(entity)
        };
    }

    public async Task<ShortUrlAnalytics?> GetAnalyticsAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;

        return new ShortUrlAnalytics
        {
            Id = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ShortCode = entity.ShortCode,
            TotalClicks = entity.ClickCount,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastAccessedAtUtc = entity.LastAccessedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            Status = GetStatus(entity)
        };
    }

    public async Task<bool> DeleteUrlAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return false;

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(entity);

        return true;
    }

    private static string GetStatus(ShortUrl entity)
    {
        if (entity.IsDeleted) return "Deleted";
        if (entity.IsExpired) return "Expired";
        return "Active";
    }

    private static string GenerateShortCode()
    {
        return Guid.NewGuid().ToString("N")[..7];
    }
}
