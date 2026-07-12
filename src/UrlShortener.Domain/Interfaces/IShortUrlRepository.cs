using UrlShortener.Domain.Entities;

namespace UrlShortener.Domain.Interfaces;

public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByIdAsync(Guid id);
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode);
    Task<bool> ShortCodeExistsAsync(string shortCode);
    Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<ShortUrl> CreateAsync(ShortUrl shortUrl);
    Task UpdateAsync(ShortUrl shortUrl);
}
