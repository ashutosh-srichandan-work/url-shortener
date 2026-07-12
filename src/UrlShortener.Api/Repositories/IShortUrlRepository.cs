using UrlShortener.Api.Entities;

namespace UrlShortener.Api.Repositories;

public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByIdAsync(Guid id);
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode);
    Task<bool> ShortCodeExistsAsync(string shortCode);
    Task<ShortUrl> CreateAsync(ShortUrl shortUrl);
    Task UpdateAsync(ShortUrl shortUrl);
}