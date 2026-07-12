using FluentAssertions;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Validators;

namespace UrlShortener.Tests;

public class CreateShortUrlRequestValidatorTests
{
    private readonly CreateShortUrlRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        var request = new CreateShortUrlRequest { Url = "https://www.example.com" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUrl_ShouldFail()
    {
        var request = new CreateShortUrlRequest { Url = "" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Fact]
    public void Validate_WithInvalidUrl_ShouldFail()
    {
        var request = new CreateShortUrlRequest { Url = "not-a-url" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("ab")]
    public void Validate_WithShortAlias_ShouldFail(string alias)
    {
        var request = new CreateShortUrlRequest { Url = "https://example.com", CustomAlias = alias };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidAliasCharacters_ShouldFail()
    {
        var request = new CreateShortUrlRequest { Url = "https://example.com", CustomAlias = "my alias!" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithPastExpirationDate_ShouldFail()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://example.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidAlias_ShouldPass()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://example.com",
            CustomAlias = "my-valid-alias_1"
        };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithFutureExpiration_ShouldPass()
    {
        var request = new CreateShortUrlRequest
        {
            Url = "https://example.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
