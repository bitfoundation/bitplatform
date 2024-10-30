//+:cnd:noEmit
using System.Runtime.InteropServices;

namespace Boilerplate.Client.Core.Services;

public class AppTelemetryContext : ITelemetryContext
{
    public virtual Guid? UserId { get; set; }

    public virtual Guid? UserSessionId { get; set; }

    public Guid AppSessionId { get; set; } = Guid.NewGuid();

    public virtual string? OS { get; set; } = RuntimeInformation.OSDescription;

    public virtual string? AppVersion { get; set; } = typeof(AppTelemetryContext).Assembly.GetName().Version?.ToString();

    public virtual string? WebView { get; set; }

    public virtual string? UserAgent { get; set; }

    public string? TimeZone { get; set; }

    public string? Culture { get; set; } = CultureInfo.CurrentCulture.Name;

    //#if (signalr == true)
    public bool IsOnline { get; set; }
    //#endif
}
