using UrlShortener.Api.Entities;
using UrlShortener.Api.Repositories;

namespace UrlShortener.Api.Services;

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

public class UrlShortenerService : IUrlShortenerService
{
    private readonly IShortUrlRepository _repository;

    public UrlShortenerService(IShortUrlRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateShortUrlResult> CreateShortUrlAsync(string url, string? customAlias, string baseUrl)
    {
        var shortCode = customAlias ?? GenerateShortCode();

        if (await _repository.ShortCodeExistsAsync(shortCode))
            throw new InvalidOperationException($"The alias '{shortCode}' is already in use.");

        var entity = new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = url,
            ShortCode = shortCode,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(entity);

        return new CreateShortUrlResult
        {
            Id = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ShortCode = entity.ShortCode,
            ShortUrl = $"{baseUrl.TrimEnd('/')}/{entity.ShortCode}",
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public async Task<string?> GetOriginalUrlAsync(string shortCode)
    {
        var entity = await _repository.GetByShortCodeAsync(shortCode);
        return entity?.OriginalUrl;
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
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    private static string GenerateShortCode()
    {
        return Guid.NewGuid().ToString("N")[..7];
    }
}