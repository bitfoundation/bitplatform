using System.Reflection;

namespace Boilerplate.Client.Windows.Components.Pages;

public partial class AboutPage
{
    [AutoInject] private ITelemetryContext telemetryContext = default!;


    private string appName = default!;
    private string appVersion = default!;
    private string platform = default!;
    private string webView = default!;
    private string processId = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        var asm = typeof(AboutPage).Assembly;
        appName = asm.GetCustomAttribute<AssemblyTitleAttribute>()!.Title;
        appVersion = telemetryContext.AppVersion!;
        platform = telemetryContext.Platform!;
        webView = telemetryContext.WebView!;
        processId = Environment.ProcessId.ToString();
    }
}
