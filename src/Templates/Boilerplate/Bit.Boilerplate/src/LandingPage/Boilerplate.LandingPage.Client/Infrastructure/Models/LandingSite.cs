namespace Boilerplate.LandingPage.Client.Infrastructure.Models;

public enum LandingPlatform
{
    iOS,
    Android,
    Windows,
    macOS,
    Web
}

public enum LandingPlatformStatus
{
    Live,
    PreOrder,
    ComingSoon
}

public sealed record LandingPlatformLink(LandingPlatform Platform, LandingPlatformStatus Status, string? Url = null);

public sealed record LandingCompany
{
    public required string LegalName { get; init; }

    public required string Address { get; init; }

    public required string Email { get; init; }

    public string? Phone { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? VatId { get; init; }

    public string? Representative { get; init; }
}

public sealed record LandingSite
{
    public required string Key { get; init; }

    public required string Path { get; init; }

    public required string Name { get; init; }

    public required string AccentColor { get; init; }

    public required Type Layout { get; init; }

    public required Type NotFound { get; init; }

    public LandingPlatformLink[] Platforms { get; init; } = [];

    public string? AppleAppId { get; init; }

    public string? WebAppUrl { get; init; }

    public string? StatusUrl { get; init; }

    public required LandingCompany Company { get; init; }

    public bool IsPreLaunch => Platforms.All(p => p.Status is not LandingPlatformStatus.Live);

    public LandingPlatformLink? Find(LandingPlatform platform) => Platforms.FirstOrDefault(p => p.Platform == platform);
}
