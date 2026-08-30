//+:cnd:noEmit
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

public interface ITelemetryContext
{
    public static ITelemetryContext? Current
    {
        get
        {
            if (AppPlatform.IsBlazorHybridOrBrowser is false)
                throw new InvalidOperationException("ITelemetryContext.Current is only available in Blazor Hybrid or web assembly apps.");

            return field;
        }
        set
        {
            if (AppPlatform.IsBlazorHybridOrBrowser is false)
                throw new InvalidOperationException("ITelemetryContext.Current is only available in Blazor Hybrid or web assembly apps.");

            field = value;
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

    /// <summary>
    /// The zone the app actually renders date/times in - the user's preference, kept fresh by
    /// <c>TimeZoneService.ApplyPreferredTimeZone</c>; the device's zone until that first resolves.
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    public bool? IsOnline { get; set; }

    public Dictionary<string, object?> ToDictionary(Dictionary<string, object?>? additionalParameters = null)
    {
        var data = new Dictionary<string, object?>(additionalParameters ?? []);

        data[nameof(UserId)] = UserId;
        data[nameof(UserSessionId)] = UserSessionId;
        data["ClientAppSessionId"] = AppSessionId;
        data[nameof(Platform)] = Platform;
        data[nameof(AppVersion)] = AppVersion;
        data[nameof(PageUrl)] = PageUrl;
        data[nameof(TimeZone)] = TimeZone ?? TimeZoneInfo.Local.Id;
        data["ClientDateTime"] = TimeProvider.GetUtcNow().ToString("u");
        // Culture stays ambient - always current, where a stored one would go stale after an in-place language switch.
        data["Culture"] = CultureInfo.CurrentUICulture.Name;
        data["Environment"] = AppEnvironment.Current;
        data[nameof(IsOnline)] = IsOnline;

        if (AppPlatform.IsBlazorHybrid)
        {
            data[nameof(WebView)] = WebView;
        }

        return data;
    }
}
