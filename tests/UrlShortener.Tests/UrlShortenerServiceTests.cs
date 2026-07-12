using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Services;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Data;
using UrlShortener.Infrastructure.Repositories;

namespace UrlShortener.Tests;

public class UrlShortenerServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UrlShortenerService _service;

    public UrlShortenerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var repository = new ShortUrlRepository(_context);
        _service = new UrlShortenerService(repository);
    }

    [Fact]
    public async Task CreateShortUrl_WithValidRequest_ReturnsResult()
    {
        var result = await _service.CreateShortUrlAsync("https://www.example.com", null, null, "https://localhost");

        result.Should().NotBeNull();
        result.OriginalUrl.Should().Be("https://www.example.com");
        result.ShortCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateShortUrl_WithCustomAlias_UsesAlias()
    {
        var result = await _service.CreateShortUrlAsync("https://www.example.com", "my-alias", null, "https://localhost");

        result.ShortCode.Should().Be("my-alias");
        result.ShortUrl.Should().Be("https://localhost/my-alias");
    }

    [Fact]
    public async Task CreateShortUrl_WithDuplicateAlias_ThrowsException()
    {
        await _service.CreateShortUrlAsync("https://www.example.com", "duplicate", null, "https://localhost");

        var act = () => _service.CreateShortUrlAsync("https://other.com", "duplicate", null, "https://localhost");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplicate*");
    }

    [Fact]
    public async Task GetOriginalUrlAndTrackClick_WithValidCode_ReturnsUrlAndIncrementsCount()
    {
        var created = await _service.CreateShortUrlAsync("https://www.example.com", "click-test", null, "https://localhost");

        var result = await _service.GetOriginalUrlAndTrackClickAsync("click-test");

        result.Should().Be("https://www.example.com");

        var entity = await _context.ShortUrls.FirstAsync(s => s.ShortCode == "click-test");
        entity.ClickCount.Should().Be(1);
        entity.LastAccessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOriginalUrlAndTrackClick_WithMissingCode_ReturnsNull()
    {
        var result = await _service.GetOriginalUrlAndTrackClickAsync("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOriginalUrlAndTrackClick_WithExpiredUrl_ReturnsNull()
    {
        _context.ShortUrls.Add(new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = "https://www.example.com",
            ShortCode = "expired",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetOriginalUrlAndTrackClickAsync("expired");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOriginalUrlAndTrackClick_WithDeletedUrl_ReturnsNull()
    {
        _context.ShortUrls.Add(new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = "https://www.example.com",
            ShortCode = "deleted",
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetOriginalUrlAndTrackClickAsync("deleted");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUrl_WithExistingUrl_SoftDeletes()
    {
        var created = await _service.CreateShortUrlAsync("https://www.example.com", "to-delete", null, "https://localhost");

        var result = await _service.DeleteUrlAsync(created.Id);

        result.Should().BeTrue();
        var entity = await _context.ShortUrls.FindAsync(created.Id);
        entity!.IsDeleted.Should().BeTrue();
        entity.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteUrl_WithNonExistentId_ReturnsFalse()
    {
        var result = await _service.DeleteUrlAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAnalytics_WithExistingUrl_ReturnsAnalytics()
    {
        var created = await _service.CreateShortUrlAsync("https://www.example.com", "analytics-test", null, "https://localhost");

        var result = await _service.GetAnalyticsAsync(created.Id);

        result.Should().NotBeNull();
        result!.TotalClicks.Should().Be(0);
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetUrlDetails_WithNonExistentId_ReturnsNull()
    {
        var result = await _service.GetUrlDetailsAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
