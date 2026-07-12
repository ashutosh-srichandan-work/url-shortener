using FluentValidation;
using UrlShortener.Application.DTOs;

namespace UrlShortener.Application.Validators;

public class CreateShortUrlRequestValidator : AbstractValidator<CreateShortUrlRequest>
{
    public CreateShortUrlRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required.")
            .Must(BeAValidUrl).WithMessage("URL must be a valid absolute URL.");

        RuleFor(x => x.CustomAlias)
            .MinimumLength(3).When(x => !string.IsNullOrEmpty(x.CustomAlias))
            .WithMessage("Alias must be at least 3 characters.")
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.CustomAlias))
            .WithMessage("Alias must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9-_]+$").When(x => !string.IsNullOrEmpty(x.CustomAlias))
            .WithMessage("Alias can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.ExpiresAtUtc)
            .GreaterThan(DateTime.UtcNow).When(x => x.ExpiresAtUtc.HasValue)
            .WithMessage("Expiration date must be in the future.");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
