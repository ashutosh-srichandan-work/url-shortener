using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Entities;
using UrlShortener.Api.Models;
using UrlShortener.Api.Repositories;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
public class UrlsController : ControllerBase
{
    private readonly IShortUrlRepository _repository;

    public UrlsController(IShortUrlRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShortenUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("URL is required.");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return BadRequest("Invalid URL format.");

        var shortCode = request.CustomAlias ?? GenerateShortCode();

        if (await _repository.ShortCodeExistsAsync(shortCode))
            return Conflict($"The alias '{shortCode}' is already in use.");

        var entity = new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = request.Url,
            ShortCode = shortCode,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(entity);

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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
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

    [HttpGet("/{shortCode}")]
    public async Task<IActionResult> RedirectToUrl(string shortCode)
    {
        var entity = await _repository.GetByShortCodeAsync(shortCode);
        if (entity is null)
            return NotFound("Short URL not found.");

        return Redirect(entity.OriginalUrl);
    }

    private static string GenerateShortCode()
    {
        return Guid.NewGuid().ToString("N")[..7];
    }
}