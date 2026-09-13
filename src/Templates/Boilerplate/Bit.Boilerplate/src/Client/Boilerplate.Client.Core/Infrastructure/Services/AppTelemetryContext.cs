//+:cnd:noEmit
using System.Runtime.InteropServices;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public class AppTelemetryContext : ITelemetryContext
{
    public virtual Guid? UserId { get; set; }

    public virtual Guid? UserSessionId { get; set; }

    public Guid AppSessionId { get; set; } = Guid.CreateVersion7();

    public virtual string? Platform { get; set; } = RuntimeInformation.OSDescription;

    public virtual string? AppVersion { get; set; } = typeof(AppTelemetryContext).Assembly.GetName().Version?.ToString();

    public virtual string? WebView { get; set; }

    public virtual string? PageUrl { get; set; }

    public virtual string? TimeZone { get; set; }

    public bool? IsOnline { get; set; }
}
