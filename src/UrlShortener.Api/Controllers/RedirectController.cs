using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class RedirectController : ControllerBase
{
    private readonly IUrlShortenerService _urlShortenerService;
    private readonly ILogger<RedirectController> _logger;

    public RedirectController(IUrlShortenerService urlShortenerService, ILogger<RedirectController> logger)
    {
        _urlShortenerService = urlShortenerService;
        _logger = logger;
    }

    /// <summary>
    /// Redirects to the original URL for the given short code.
    /// </summary>
    /// <param name="shortCode">The short code (e.g., "my-link" or "aBc1234").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>302 redirect to the original URL.</returns>
    /// <remarks>
    /// ⚠️ **This endpoint cannot be tested from Swagger UI** because the browser blocks cross-origin redirects.
    /// 
    /// **To test:** Open your browser and navigate directly to:
    /// 
    ///     http://localhost:5073/{shortCode}
    /// 
    /// For example: `http://localhost:5073/my-link`
    /// 
    /// The browser will automatically redirect you to the original URL.
    /// Each visit increments the click counter (visible via the analytics endpoint).
    /// Returns **404** if the short code doesn't exist, is expired, or was deleted.
    /// </remarks>
    /// <response code="302">Redirecting to the original URL.</response>
    /// <response code="404">Short code not found, expired, or deleted.</response>
    [HttpGet("/{shortCode}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToOriginal(string shortCode, CancellationToken cancellationToken)
    {
        var originalUrl = await _urlShortenerService.GetOriginalUrlAndTrackClickAsync(shortCode, cancellationToken);

        if (originalUrl is null)
        {
            _logger.LogWarning("Redirect failed for short code: {ShortCode}", shortCode);
            return NotFound();
        }

        _logger.LogInformation("Redirecting {ShortCode} to {OriginalUrl}", shortCode, originalUrl);
        return Redirect(originalUrl);
    }
}
