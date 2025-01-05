//+:cnd:noEmit
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface ITelemetryContext
{
    private static ITelemetryContext? _current;

    public static ITelemetryContext? Current
    {
        get
        {
            if (AppPlatform.IsBlazorHybridOrBrowser is false)
                throw new InvalidOperationException("ITelemetryContext.Current is only available in Blazor Hybrid or web assembly apps.");

            return _current;
        }
        set
        {
            if (AppPlatform.IsBlazorHybridOrBrowser is false)
                throw new InvalidOperationException("ITelemetryContext.Current is only available in Blazor Hybrid or web assembly apps.");

            _current = value;
        }
    }

    public Guid? UserId { get; set; }

    /// <summary>
    /// Stored in Users table's Sessions column and is identified after the user sign-in.
    /// </summary>
    public Guid? UserSessionId { get; set; }

    public Guid AppSessionId { get; set; }

    public string? Platform { get; set; }

    public string? AppVersion { get; set; }
    public string? WebView { get; set; }

    public string? PageUrl { get; set; }

    public string? TimeZone { get; set; }
    public string? Culture { get; set; }

    public string? Environment { get; set; }

    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    public bool? IsOnline { get; set; }

    public Dictionary<string, object?> ToDictionary(Dictionary<string, object?>? additionalParameters = null)
    {
        var data = new Dictionary<string, object?>(additionalParameters ?? [])
        {
            { nameof(UserId), UserId },
            { nameof(UserSessionId), UserSessionId },
            { nameof(AppSessionId), AppSessionId },
            { nameof(Platform), Platform },
            { nameof(AppVersion), AppVersion },
            { nameof(PageUrl), PageUrl },
            { nameof(TimeZone), TimeZone },
            { "ClientDateTime", DateTimeOffset.UtcNow.ToString("u") },
            { nameof(Culture), Culture },
            { nameof(Environment), Environment },
            { nameof(IsOnline), IsOnline }
        };

        if (AppPlatform.IsBlazorHybrid)
        {
            data[nameof(WebView)] = WebView;
        }

        return data;
    }
}
