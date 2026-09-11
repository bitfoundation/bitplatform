//+:cnd:noEmit

using Bit.Butil;

namespace Boilerplate.Client.Web.Components.Pages;

public partial class AboutPage
{
    [AutoInject] private UserAgent userAgent = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;


    private string oem = default!;
    private string appName = default!;
    private string platform = default!;
    private string appVersion = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        // You can add `.razor`, `.razor.cs`, and `.razor.scss` files to the `Client.Maui` and `Client.Windows` projects,
        // allowing direct access to native platform features without dependency injection.
        // The `AboutPage.razor` file in `Client.Web` demonstrates that you can use the same route (e.g., `/about`) on the web,
        // but it does not provide access to native platform features.

        appName = "Boilerplate";
        appVersion = telemetryContext.AppVersion!;

        // From the user agent rather than ITelemetryContext.Platform, and only once there is a device to ask: while
        // prerendering, the only os this anonymous page could name is the host's. Environment.ProcessId has the same
        // problem and no client-side answer, so it isn't shown at all.
        if (InPrerenderSession)
        {
            platform = "Generic Server";
        }
        else
        {
            var userAgentData = await userAgent.Extract();
            oem = userAgentData.Manufacturer ?? "?";
            platform = string.Join(' ', [userAgentData.Manufacturer, userAgentData.OsName, userAgentData.Name, "browser"]);
        }
    }
}
