using System.Security.Cryptography;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace UrlShortener.Application.Services;

public class UrlShortenerService : IUrlShortenerService
{
    private readonly IShortUrlRepository _repository;
    private readonly ILogger<UrlShortenerService> _logger;
    private const int ShortCodeLength = 7;
    private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public UrlShortenerService(IShortUrlRepository repository, ILogger<UrlShortenerService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ShortUrlResponse> CreateShortUrlAsync(CreateShortUrlRequest request, string baseUrl, CancellationToken cancellationToken = default)
    {
        // Duplicate detection: return existing short URL if same original URL already shortened
        var existing = await _repository.GetByOriginalUrlAsync(request.Url, cancellationToken);
        if (existing is not null && request.CustomAlias is null)
        {
            _logger.LogInformation("Duplicate URL detected, returning existing short URL for {OriginalUrl}", request.Url);
            return MapToResponse(existing, baseUrl);
        }

        var shortCode = request.CustomAlias ?? await GenerateUniqueShortCodeAsync(cancellationToken);

        if (await _repository.ShortCodeExistsAsync(shortCode, cancellationToken))
        {
            _logger.LogWarning("Short code {ShortCode} already exists", shortCode);
            throw new InvalidOperationException($"The alias '{shortCode}' is already in use.");
        }

        var shortUrl = new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = request.Url,
            ShortCode = shortCode,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = request.ExpiresAtUtc,
            ClickCount = 0,
            IsDeleted = false
        };

        await _repository.CreateAsync(shortUrl, cancellationToken);

        _logger.LogInformation("Created short URL {ShortCode} for {OriginalUrl}", shortCode, request.Url);

        return MapToResponse(shortUrl, baseUrl);
    }

    public async Task<string?> GetOriginalUrlAndTrackClickAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var shortUrl = await _repository.GetByShortCodeAsync(shortCode, cancellationToken);

        if (shortUrl is null || shortUrl.IsDeleted)
        {
            _logger.LogWarning("Short code {ShortCode} not found or deleted", shortCode);
            return null;
        }

        if (shortUrl.IsExpired)
        {
            _logger.LogWarning("Short code {ShortCode} has expired", shortCode);
            return null;
        }

        shortUrl.ClickCount++;
        shortUrl.LastAccessedAtUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(shortUrl, cancellationToken);

        _logger.LogInformation("Redirecting short code {ShortCode}, click count: {ClickCount}", shortCode, shortUrl.ClickCount);

        return shortUrl.OriginalUrl;
    }

    public async Task<ShortUrlResponse?> GetUrlDetailsAsync(Guid id, string baseUrl, CancellationToken cancellationToken = default)
    {
        var shortUrl = await _repository.GetByIdAsync(id, cancellationToken);
        if (shortUrl is null) return null;
        return MapToResponse(shortUrl, baseUrl);
    }

    public async Task<UrlAnalyticsResponse?> GetAnalyticsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shortUrl = await _repository.GetByIdAsync(id, cancellationToken);
        if (shortUrl is null) return null;

        return new UrlAnalyticsResponse
        {
            Id = shortUrl.Id,
            OriginalUrl = shortUrl.OriginalUrl,
            ShortCode = shortUrl.ShortCode,
            TotalClicks = shortUrl.ClickCount,
            CreatedAtUtc = shortUrl.CreatedAtUtc,
            LastAccessedAtUtc = shortUrl.LastAccessedAtUtc,
            ExpiresAtUtc = shortUrl.ExpiresAtUtc,
            Status = shortUrl.IsDeleted ? "Deleted" : shortUrl.IsExpired ? "Expired" : "Active"
        };
    }

    public async Task<bool> DeleteUrlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shortUrl = await _repository.GetByIdAsync(id, cancellationToken);
        if (shortUrl is null) return false;

        shortUrl.IsDeleted = true;
        shortUrl.DeletedAtUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(shortUrl, cancellationToken);

        _logger.LogInformation("Soft-deleted short URL {Id}", id);
        return true;
    }

    public async Task<PagedResponse<ShortUrlResponse>> GetAllPaginatedAsync(int page, int pageSize, string baseUrl, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _repository.GetPaginatedAsync(page, pageSize, cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<ShortUrlResponse>
        {
            Items = items.Select(s => MapToResponse(s, baseUrl)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    private async Task<string> GenerateUniqueShortCodeAsync(CancellationToken cancellationToken)
    {
        string shortCode;
        do
        {
            shortCode = GenerateShortCode();
        } while (await _repository.ShortCodeExistsAsync(shortCode, cancellationToken));

        return shortCode;
    }

    private static string GenerateShortCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(ShortCodeLength);
        var chars = new char[ShortCodeLength];
        for (int i = 0; i < ShortCodeLength; i++)
        {
            chars[i] = AllowedChars[bytes[i] % AllowedChars.Length];
        }
        return new string(chars);
    }

    private static ShortUrlResponse MapToResponse(ShortUrl shortUrl, string baseUrl)
    {
        return new ShortUrlResponse
        {
            Id = shortUrl.Id,
            OriginalUrl = shortUrl.OriginalUrl,
            ShortUrl = string.IsNullOrEmpty(baseUrl) ? shortUrl.ShortCode : $"{baseUrl.TrimEnd('/')}/{shortUrl.ShortCode}",
            ShortCode = shortUrl.ShortCode,
            CreatedAtUtc = shortUrl.CreatedAtUtc,
            ExpiresAtUtc = shortUrl.ExpiresAtUtc,
            ClickCount = shortUrl.ClickCount,
            LastAccessedAtUtc = shortUrl.LastAccessedAtUtc,
            Status = shortUrl.IsDeleted ? "Deleted" : shortUrl.IsExpired ? "Expired" : "Active"
        };
    }
}
