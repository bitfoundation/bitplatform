using System.Reflection;

namespace Boilerplate.Client.Windows.Components.Pages;

public partial class AboutPage
{
    private string appName = default!;
    private string appVersion = default!;
    private string os = default!;
    private string processId = default!;

    protected async override Task OnInitAsync()
    {
        var asm = typeof(AboutPage).Assembly;
        appName = asm.GetCustomAttribute<AssemblyTitleAttribute>()!.Title;
        appVersion = asm.GetName().Version!.ToString();
        os = AppPlatform.OSDescription;
        processId = Environment.ProcessId.ToString();

        await base.OnInitAsync();
    }
}
