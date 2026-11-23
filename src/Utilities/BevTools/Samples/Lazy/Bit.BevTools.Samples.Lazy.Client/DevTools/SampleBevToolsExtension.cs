using Bit.BevTools;

namespace Bit.BevTools.Samples.Lazy.Client.DevTools;

/// <summary>
/// An extension of the app's own. Internal: it implements a Bit.BevTools interface, and a public type of this assembly
/// that does would break the router's start (see <see cref="SampleDevTools"/>).
/// </summary>
internal sealed class SampleBevToolsExtension : IBevToolsExtension
{
    public const string SessionPanelId = "sample-session";

    public string Name => "Lazy loading sample";

    public void Configure(IBevToolsExtensionBuilder builder)
    {
        builder.AddPanel(new BevToolsPanelDescriptor(SessionPanelId, "Session", typeof(SampleSessionPanel), Order: 110));
        builder.AddCommand(new SampleBevToolsCommand());
    }
}
