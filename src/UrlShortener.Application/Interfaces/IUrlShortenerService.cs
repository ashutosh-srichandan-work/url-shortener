using UrlShortener.Application.DTOs;

namespace UrlShortener.Application.Interfaces;

public interface IUrlShortenerService
{
    Task<ShortUrlResponse> CreateShortUrlAsync(CreateShortUrlRequest request, string baseUrl, CancellationToken cancellationToken = default);
    Task<string?> GetOriginalUrlAndTrackClickAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<ShortUrlResponse?> GetUrlDetailsAsync(Guid id, string baseUrl, CancellationToken cancellationToken = default);
    Task<UrlAnalyticsResponse?> GetAnalyticsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteUrlAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<ShortUrlResponse>> GetAllPaginatedAsync(int page, int pageSize, string baseUrl, CancellationToken cancellationToken = default);
}
