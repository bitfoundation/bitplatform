using Bit.BevTools;

namespace Bit.BevTools.Samples.Lazy.Client.DevTools;

/// <summary>A command palette entry of the app's own, which opens its Support panel.</summary>
internal sealed class SampleBevToolsCommand : IBevToolsCommand
{
    public string Id => "sample.support";

    public string Title => "Open the app's support panel";

    public string Category => "Sample";

    public Task ExecuteAsync(IBevToolsCommandContext context, string? argument)
    {
        context.ShowPanel(SampleDevTools.SupportPanelId);
        return Task.CompletedTask;
    }
}
