using System.Reflection;

namespace Boilerplate.Client.Windows.Components.Pages;

public partial class AboutPage
{
    protected override string? Title => Localizer[nameof(AppStrings.AboutTitle)];
    protected override string? Subtitle => string.Empty;


    private string appName = default!;
    private string appVersion = default!;
    private string os = default!;
    private string processId = default!;


    protected override async Task OnInitAsync()
    {
        var asm = typeof(AboutPage).Assembly;
        appName = asm.GetCustomAttribute<AssemblyTitleAttribute>()!.Title;
        appVersion = asm.GetName().Version!.ToString();
        os = AppPlatform.OSDescription;
        processId = Environment.ProcessId.ToString();

        await base.OnInitAsync();
    }
}
