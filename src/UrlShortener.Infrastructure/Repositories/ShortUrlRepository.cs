using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;
using UrlShortener.Infrastructure.Data;

namespace UrlShortener.Infrastructure.Repositories;

public class ShortUrlRepository : IShortUrlRepository
{
    private readonly AppDbContext _context;

    public ShortUrlRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ShortUrl?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShortUrls.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return await _context.ShortUrls
            .FirstOrDefaultAsync(s => s.ShortCode == shortCode, cancellationToken);
    }

    public async Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return await _context.ShortUrls
            .AnyAsync(s => s.ShortCode == shortCode, cancellationToken);
    }

    public async Task<ShortUrl?> GetByOriginalUrlAsync(string originalUrl, CancellationToken cancellationToken = default)
    {
        return await _context.ShortUrls
            .FirstOrDefaultAsync(s => s.OriginalUrl == originalUrl && !s.IsDeleted, cancellationToken);
    }

    public async Task<ShortUrl> CreateAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default)
    {
        _context.ShortUrls.Add(shortUrl);
        await _context.SaveChangesAsync(cancellationToken);
        return shortUrl;
    }

    public async Task UpdateAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default)
    {
        _context.ShortUrls.Update(shortUrl);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShortUrl>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ShortUrls.ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.ShortUrls.CountAsync(s => !s.IsDeleted, cancellationToken);
        var items = await _context.ShortUrls
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
