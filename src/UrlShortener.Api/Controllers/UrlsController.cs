using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Entities;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
public class UrlsController : ControllerBase
{
    private readonly AppDbContext _context;

    public UrlsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a shortened URL.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShortenUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("URL is required.");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return BadRequest("Invalid URL format.");

        var shortCode = request.CustomAlias ?? GenerateShortCode();

        if (await _context.ShortUrls.AnyAsync(u => u.ShortCode == shortCode))
            return Conflict($"The alias '{shortCode}' is already in use.");

        var entity = new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = request.Url,
            ShortCode = shortCode,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ShortUrls.Add(entity);
        await _context.SaveChangesAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = new ShortenUrlResponse
        {
            Id = entity.Id,
            ShortCode = entity.ShortCode,
            ShortUrl = $"{baseUrl}/{entity.ShortCode}",
            OriginalUrl = entity.OriginalUrl,
            CreatedAt = entity.CreatedAtUtc
        };

        return Created(response.ShortUrl, response);
    }

    /// <summary>
    /// Gets URL details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.ShortUrls.FindAsync(id);
        if (entity is null)
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = new ShortenUrlResponse
        {
            Id = entity.Id,
            ShortCode = entity.ShortCode,
            ShortUrl = $"{baseUrl}/{entity.ShortCode}",
            OriginalUrl = entity.OriginalUrl,
            CreatedAt = entity.CreatedAtUtc
        };

        return Ok(response);
    }

    /// <summary>
    /// Redirects to the original URL.
    /// </summary>
    [HttpGet("/{shortCode}")]
    public async Task<IActionResult> RedirectToUrl(string shortCode)
    {
        var entity = await _context.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
        if (entity is null)
            return NotFound("Short URL not found.");

        return Redirect(entity.OriginalUrl);
    }

    private static string GenerateShortCode()
    {
        return Guid.NewGuid().ToString("N")[..7];
    }
}
