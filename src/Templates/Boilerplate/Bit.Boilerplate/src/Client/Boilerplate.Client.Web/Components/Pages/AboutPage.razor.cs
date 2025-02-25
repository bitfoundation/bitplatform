//+:cnd:noEmit

using Bit.Butil;

namespace Boilerplate.Client.Web.Components.Pages;

public partial class AboutPage
{
    [AutoInject] private UserAgent userAgent = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;

    protected override string? Title => Localizer[nameof(AppStrings.AboutTitle)];
    protected override string? Subtitle => string.Empty;


    private string oem = default!;
    private string appName = default!;
    private string platform = default!;
    private string processId = default!;
    private string appVersion = default!;

    protected override async Task OnInitAsync()
    {
        // You can add `.razor`, `.razor.cs`, and `.razor.scss` files to the `Client.Maui` and `Client.Windows` projects,  
        // allowing direct access to native platform features without dependency injection.  
        // The `AboutPage.razor` file in `Client.Web` demonstrates that you can use the same route (e.g., `/about`) on the web,  
        // but it does not provide access to native platform features.

        appName = "Boilerplate";
        platform = telemetryContext.Platform!;
        appVersion = telemetryContext.AppVersion!;
        processId = Environment.ProcessId.ToString();

        if (InPrerenderSession is false)
        {
            oem = (await userAgent.Extract()).Manufacturer ?? "?";
        }

        await base.OnInitAsync();
    }
}
