using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/v1/links")]
[EnableRateLimiting("fixed")]
public class LinksController : ControllerBase
{
    private readonly IUrlShortenerService _urlShortenerService;
    private readonly IValidator<CreateShortUrlRequest> _validator;
    private readonly ILogger<LinksController> _logger;

    public LinksController(
        IUrlShortenerService urlShortenerService,
        IValidator<CreateShortUrlRequest> validator,
        ILogger<LinksController> logger)
    {
        _urlShortenerService = urlShortenerService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Lists all short URLs with pagination.
    /// </summary>
    /// <param name="page">Page number (default: 1). Must be >= 1.</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of short URLs.</returns>
    /// <remarks>
    /// Soft-deleted URLs are excluded from results.
    /// Results are ordered by creation date (newest first).
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ShortUrlResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _urlShortenerService.GetAllPaginatedAsync(page, pageSize, baseUrl, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new short URL.
    /// </summary>
    /// <param name="request">The URL to shorten, with optional custom alias and expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created short URL details.</returns>
    /// <remarks>
    /// **Example request:**
    /// 
    ///     POST /api/v1/links
    ///     {
    ///         "url": "https://www.example.com/very-long-article-path",
    ///         "customAlias": "my-link",
    ///         "expiresAtUtc": "2025-12-31T23:59:59Z"
    ///     }
    /// 
    /// - `url` (required): Must be a valid HTTP or HTTPS URL.
    /// - `customAlias` (optional): 3-50 chars, letters/numbers/hyphens/underscores only. If omitted, a random 7-char code is generated.
    /// - `expiresAtUtc` (optional): Must be a future UTC date. If omitted, the link never expires.
    /// 
    /// If the same original URL was already shortened (without a custom alias), the existing short URL is returned.
    /// If a custom alias is already taken, returns **409 Conflict**.
    /// </remarks>
    /// <response code="201">Short URL created successfully.</response>
    /// <response code="400">Validation failed (invalid URL, alias format, or past expiration).</response>
    /// <response code="409">The custom alias is already in use.</response>
    [HttpPost]
    [EnableRateLimiting("create")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _urlShortenerService.CreateShortUrlAsync(request, baseUrl, cancellationToken);

        _logger.LogInformation("Short URL created: {ShortCode}", result.ShortCode);

        return CreatedAtAction(nameof(GetDetails), new { id = result.Id }, result);
    }

    /// <summary>
    /// Gets full details of a short URL by its ID.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the short URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The short URL details including click count and status.</returns>
    /// <response code="200">Short URL found.</response>
    /// <response code="404">No short URL exists with the given ID.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(Guid id, CancellationToken cancellationToken)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _urlShortenerService.GetUrlDetailsAsync(id, baseUrl, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Gets click analytics for a short URL.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the short URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics including total clicks, last accessed time, and current status (Active/Expired/Deleted).</returns>
    /// <response code="200">Analytics retrieved.</response>
    /// <response code="404">No short URL exists with the given ID.</response>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(UrlAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalytics(Guid id, CancellationToken cancellationToken)
    {
        var result = await _urlShortenerService.GetAnalyticsAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes a short URL (it will no longer redirect).
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the short URL to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <remarks>
    /// This performs a **soft delete** — the record is preserved in the database with a deleted timestamp,
    /// but the short code will return 404 on redirect attempts.
    /// </remarks>
    /// <response code="204">Successfully deleted.</response>
    /// <response code="404">No short URL exists with the given ID.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _urlShortenerService.DeleteUrlAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
