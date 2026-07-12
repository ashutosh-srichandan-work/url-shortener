using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Services;
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
        var logger = new Mock<ILogger<UrlShortenerService>>();
        _service = new UrlShortenerService(repository, logger.Object);
    }

    [Fact]
    public async Task CreateShortUrl_WithValidRequest_ReturnsShortUrlResponse()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://www.example.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        var result = await _service.CreateShortUrlAsync(request, "https://localhost");

        result.Should().NotBeNull();
        result.OriginalUrl.Should().Be("https://www.example.com");
        result.ShortCode.Should().NotBeNullOrEmpty();
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task CreateShortUrl_WithCustomAlias_UsesAlias()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://www.example.com",
            CustomAlias = "my-alias"
        };

        var result = await _service.CreateShortUrlAsync(request, "https://localhost");

        result.ShortCode.Should().Be("my-alias");
        result.ShortUrl.Should().Be("https://localhost/my-alias");
    }

    [Fact]
    public async Task CreateShortUrl_WithDuplicateAlias_ThrowsInvalidOperationException()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://www.example.com",
            CustomAlias = "duplicate"
        };

        await _service.CreateShortUrlAsync(request, "https://localhost");

        var act = () => _service.CreateShortUrlAsync(request, "https://localhost");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplicate*");
    }

    [Fact]
    public async Task GetOriginalUrlAndTrackClick_WithValidCode_ReturnsUrlAndIncrementsCount()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://www.example.com",
            CustomAlias = "click-test"
        };

        await _service.CreateShortUrlAsync(request, "https://localhost");

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
        var request = new CreateShortUrlRequest
        {
            Url = "https://www.example.com",
            CustomAlias = "to-delete"
        };
        var created = await _service.CreateShortUrlAsync(request, "https://localhost");

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
        var request = new CreateShortUrlRequest
        {
            Url = "https://www.example.com",
            CustomAlias = "analytics-test"
        };
        var created = await _service.CreateShortUrlAsync(request, "https://localhost");

        var result = await _service.GetAnalyticsAsync(created.Id);

        result.Should().NotBeNull();
        result!.TotalClicks.Should().Be(0);
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetUrlDetails_WithNonExistentId_ReturnsNull()
    {
        var result = await _service.GetUrlDetailsAsync(Guid.NewGuid(), "https://localhost");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUrlDetails_WithExistingId_ReturnsDetails()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://www.example.com",
            CustomAlias = "details-test"
        };
        var created = await _service.CreateShortUrlAsync(request, "https://localhost");

        var result = await _service.GetUrlDetailsAsync(created.Id, "https://localhost");

        result.Should().NotBeNull();
        result!.ShortUrl.Should().Be("https://localhost/details-test");
        result.OriginalUrl.Should().Be("https://www.example.com");
    }

    [Fact]
    public async Task CreateShortUrl_WithDuplicateUrl_ReturnsExistingShortUrl()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://www.duplicate-test.com"
        };

        var first = await _service.CreateShortUrlAsync(request, "https://localhost");
        var second = await _service.CreateShortUrlAsync(request, "https://localhost");

        second.Id.Should().Be(first.Id);
        second.ShortCode.Should().Be(first.ShortCode);
    }

    [Fact]
    public async Task GetAllPaginated_ReturnsCorrectPage()
    {
        for (int i = 0; i < 5; i++)
        {
            await _service.CreateShortUrlAsync(
                new CreateShortUrlRequest { Url = $"https://example.com/{i}", CustomAlias = $"page-{i}" },
                "https://localhost");
        }

        var result = await _service.GetAllPaginatedAsync(1, 2, "https://localhost");

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllPaginated_ExcludesDeletedUrls()
    {
        var created = await _service.CreateShortUrlAsync(
            new CreateShortUrlRequest { Url = "https://example.com/del", CustomAlias = "del-pag" },
            "https://localhost");
        await _service.DeleteUrlAsync(created.Id);

        var result = await _service.GetAllPaginatedAsync(1, 10, "https://localhost");

        result.Items.Should().NotContain(x => x.Id == created.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
