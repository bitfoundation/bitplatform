using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class StoreBadges
{
    [AutoInject] private Navigator navigator = default!;

    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [Parameter] public bool ShowComingSoon { get; set; } = true;

    [Parameter] public string? Campaign { get; set; }

    [Parameter] public string? Class { get; set; }

    private LandingPlatform? visitorPlatform;
    private IReadOnlyList<LandingPlatformLink> ordered = [];

    protected override void OnParametersSet()
    {
        ordered = Order();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var userAgent = await navigator.GetUserAgent();

            visitorPlatform = userAgent switch
            {
                _ when userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) => LandingPlatform.iOS,
                _ when userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) => LandingPlatform.Android,
                _ when userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) => LandingPlatform.Windows,
                _ when userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) => LandingPlatform.macOS,
                _ => null
            };

            if (visitorPlatform is not null)
            {
                ordered = Order();
                StateHasChanged();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private List<LandingPlatformLink> Order() => [.. Site.Platforms.OrderBy(l => l.Platform == visitorPlatform ? 0 : 1).ThenBy(l => l.Platform)];

    private string Track(LandingPlatformLink link)
    {
        if (Campaign is null)
            return link.Url!;

        var separator = link.Url!.Contains('?') ? '&' : '?';

        return link.Platform is LandingPlatform.Android
            ? $"{link.Url}{separator}referrer={Uri.EscapeDataString($"utm_source=landing&utm_campaign={Campaign}")}"
            : $"{link.Url}{separator}ct={Uri.EscapeDataString(Campaign)}";
    }

    private string PlatformName(LandingPlatform platform) => Localizer[$"Platform{platform}"];
}
