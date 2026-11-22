namespace Bit.BevTools;

/// <summary>
/// The extension <see cref="BevToolsOptions.AddPanel{TComponent}"/> adds: one panel, so an application that only wants a tab
/// of its own does not have to write an <see cref="IBevToolsExtension"/>.
/// </summary>
internal sealed class BevToolsPanelExtension(BevToolsPanelDescriptor panel) : IBevToolsExtension
{
    public BevToolsPanelDescriptor Panel { get; } = panel;

    public string Name => Panel.Title + " panel";

    public void Configure(IBevToolsExtensionBuilder builder) => builder.AddPanel(Panel);
}
