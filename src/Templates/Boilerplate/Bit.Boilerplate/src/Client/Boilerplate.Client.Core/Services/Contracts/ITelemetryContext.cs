//+:cnd:noEmit
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

    public string? OS { get; set; }

    public string? AppVersion { get; set; }
    public string? WebView { get; set; }

    public string? UserAgent { get; set; }

    public string? TimeZone { get; set; }
    public string? Culture { get; set; }

    //#if (signalr == true)
    public bool IsOnline { get; set; }
    //#endif
}
