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

    public async Task<ShortUrl?> GetByIdAsync(Guid id)
    {
        return await _context.ShortUrls.FindAsync(id);
    }

    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode)
    {
        return await _context.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
    }

    public async Task<bool> ShortCodeExistsAsync(string shortCode)
    {
        return await _context.ShortUrls.AnyAsync(u => u.ShortCode == shortCode);
    }

    public async Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = _context.ShortUrls
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAtUtc);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ShortUrl> CreateAsync(ShortUrl shortUrl)
    {
        _context.ShortUrls.Add(shortUrl);
        await _context.SaveChangesAsync();
        return shortUrl;
    }

    public async Task UpdateAsync(ShortUrl shortUrl)
    {
        _context.ShortUrls.Update(shortUrl);
        await _context.SaveChangesAsync();
    }
}
