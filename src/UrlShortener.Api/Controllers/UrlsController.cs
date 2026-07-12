using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Entities;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
public class UrlsController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, ShortUrl> _urlStore = new();

    /// <summary>
    /// Creates a shortened URL.
    /// </summary>
    [HttpPost]
    public IActionResult Create([FromBody] ShortenUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("URL is required.");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return BadRequest("Invalid URL format.");

        var shortCode = request.CustomAlias ?? GenerateShortCode();

        if (_urlStore.ContainsKey(shortCode))
            return Conflict($"The alias '{shortCode}' is already in use.");

        var entity = new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = request.Url,
            ShortCode = shortCode,
            CreatedAtUtc = DateTime.UtcNow
        };

        _urlStore[shortCode] = entity;

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
    public IActionResult GetById(Guid id)
    {
        var entity = _urlStore.Values.FirstOrDefault(u => u.Id == id);
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
    public IActionResult Redirect(string shortCode)
    {
        if (_urlStore.TryGetValue(shortCode, out var entity))
            return Redirect(entity.OriginalUrl);

        return NotFound("Short URL not found.");
    }

    private static string GenerateShortCode()
    {
        return Guid.NewGuid().ToString("N")[..7];
    }
}
