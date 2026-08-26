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

        // Read from the user agent rather than from ITelemetryContext.Platform, and never during prerendering. This
        // page runs in the web server's process whenever the render mode is server-side - during prerendering in every
        // mode, and for the whole circuit under Blazor Server - where Platform is the HOST's RuntimeInformation
        // .OSDescription until AppClientCoordinator overwrites it after the first render. Showing that to an anonymous
        // visitor discloses the server's kernel build on a page whose subject is the visitor's own device.
        // Environment.ProcessId has the same problem and no client-side answer at all, so it is simply not shown here.
        if (InPrerenderSession is false)
        {
            var userAgentData = await userAgent.Extract();
            oem = userAgentData.Manufacturer ?? "?";
            platform = string.Join(' ', [userAgentData.Manufacturer, userAgentData.OsName, userAgentData.Name, "browser"]);
        }
    }
}
