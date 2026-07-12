using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
public class UrlsController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, string> _urlStore = new();

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

        _urlStore[shortCode] = request.Url;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = new ShortenUrlResponse
        {
            ShortCode = shortCode,
            ShortUrl = $"{baseUrl}/{shortCode}",
            OriginalUrl = request.Url,
            CreatedAt = DateTime.UtcNow
        };

        return Created(response.ShortUrl, response);
    }

    /// <summary>
    /// Redirects to the original URL.
    /// </summary>
    [HttpGet("/{shortCode}")]
    public IActionResult Redirect(string shortCode)
    {
        if (_urlStore.TryGetValue(shortCode, out var originalUrl))
            return Redirect(originalUrl);

        return NotFound("Short URL not found.");
    }

    private static string GenerateShortCode()
    {
        return Guid.NewGuid().ToString("N")[..7];
    }
}
