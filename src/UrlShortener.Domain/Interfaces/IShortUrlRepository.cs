using UrlShortener.Domain.Entities;

namespace UrlShortener.Domain.Interfaces;

public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByIdAsync(Guid id);
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode);
    Task<bool> ShortCodeExistsAsync(string shortCode);
    Task<ShortUrl> CreateAsync(ShortUrl shortUrl);
    Task UpdateAsync(ShortUrl shortUrl);
}
