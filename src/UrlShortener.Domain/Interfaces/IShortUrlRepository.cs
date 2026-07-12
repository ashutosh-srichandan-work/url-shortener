using UrlShortener.Domain.Entities;

namespace UrlShortener.Domain.Interfaces;

public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<ShortUrl?> GetByOriginalUrlAsync(string originalUrl, CancellationToken cancellationToken = default);
    Task<ShortUrl> CreateAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default);
    Task UpdateAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShortUrl>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
