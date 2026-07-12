using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
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
    public async Task<IActionResult> Create([FromBody] ShortenUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("URL is required.");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return BadRequest("Invalid URL format.");

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _service.CreateShortUrlAsync(request.Url, request.CustomAlias, baseUrl);

            var response = new ShortenUrlResponse
            {
                Id = result.Id,
                ShortCode = result.ShortCode,
                ShortUrl = result.ShortUrl,
                OriginalUrl = result.OriginalUrl,
                CreatedAt = result.CreatedAtUtc
            };

            return Created(response.ShortUrl, response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var details = await _service.GetUrlDetailsAsync(id);
        if (details is null)
            return NotFound();

        return Ok(details);
    }

    [HttpGet("/{shortCode}")]
    public async Task<IActionResult> RedirectToUrl(string shortCode)
    {
        var originalUrl = await _service.GetOriginalUrlAsync(shortCode);
        if (originalUrl is null)
            return NotFound("Short URL not found.");

        return Redirect(originalUrl);
    }
}