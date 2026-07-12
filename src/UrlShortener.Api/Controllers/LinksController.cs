using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/v1/links")]
public class LinksController : ControllerBase
{
    private readonly IUrlShortenerService _service;
    private readonly IValidator<CreateShortUrlRequest> _validator;

    public LinksController(IUrlShortenerService service, IValidator<CreateShortUrlRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>
    /// Creates a new short link.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _service.CreateShortUrlAsync(request.Url, request.CustomAlias, request.ExpiresAtUtc, baseUrl);

        var response = new ShortUrlResponse
        {
            Id = result.Id,
            OriginalUrl = result.OriginalUrl,
            ShortCode = result.ShortCode,
            ShortUrl = result.ShortUrl,
            CreatedAtUtc = result.CreatedAtUtc,
            ExpiresAtUtc = result.ExpiresAtUtc,
            ClickCount = 0,
            LastAccessedAtUtc = null,
            Status = "Active"
        };

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets link details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var details = await _service.GetUrlDetailsAsync(id);
        if (details is null)
            return NotFound();

        var response = new ShortUrlResponse
        {
            Id = details.Id,
            OriginalUrl = details.OriginalUrl,
            ShortCode = details.ShortCode,
            ShortUrl = details.ShortUrl,
            CreatedAtUtc = details.CreatedAtUtc,
            ExpiresAtUtc = details.ExpiresAtUtc,
            ClickCount = details.ClickCount,
            LastAccessedAtUtc = details.LastAccessedAtUtc,
            Status = details.Status
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets analytics/stats for a link.
    /// </summary>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(UrlAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalytics(Guid id)
    {
        var analytics = await _service.GetAnalyticsAsync(id);
        if (analytics is null)
            return NotFound();

        var response = new UrlAnalyticsResponse
        {
            Id = analytics.Id,
            OriginalUrl = analytics.OriginalUrl,
            ShortCode = analytics.ShortCode,
            TotalClicks = analytics.TotalClicks,
            CreatedAtUtc = analytics.CreatedAtUtc,
            LastAccessedAtUtc = analytics.LastAccessedAtUtc,
            ExpiresAtUtc = analytics.ExpiresAtUtc,
            Status = analytics.Status
        };

        return Ok(response);
    }

    /// <summary>
    /// Soft-deletes a link.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteUrlAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Redirects to the original URL for the given short code.
    /// </summary>
    [HttpGet("/{shortCode}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> RedirectToUrl(string shortCode)
    {
        var originalUrl = await _service.GetOriginalUrlAndTrackClickAsync(shortCode);
        if (originalUrl is null)
            return NotFound("Short URL not found.");

        return Redirect(originalUrl);
    }
}
