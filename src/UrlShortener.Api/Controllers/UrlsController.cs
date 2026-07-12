using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
public class UrlsController : ControllerBase
{
    private readonly IUrlShortenerService _service;

    public UrlsController(IUrlShortenerService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("URL is required.");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return BadRequest("A valid HTTP or HTTPS URL is required.");

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _service.CreateShortUrlAsync(request.Url, request.CustomAlias, baseUrl);

            var response = new ShortUrlResponse
            {
                Id = result.Id,
                OriginalUrl = result.OriginalUrl,
                ShortCode = result.ShortCode,
                ShortUrl = result.ShortUrl,
                CreatedAtUtc = result.CreatedAtUtc
            };

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

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
            CreatedAtUtc = details.CreatedAtUtc
        };

        return Ok(response);
    }

    [HttpGet("/{shortCode}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> RedirectToUrl(string shortCode)
    {
        var originalUrl = await _service.GetOriginalUrlAsync(shortCode);
        if (originalUrl is null)
            return NotFound("Short URL not found.");

        return Redirect(originalUrl);
    }
}
