namespace Bit.BevTools;

/// <summary>
/// The extension <see cref="BevToolsOptions.AddPanel{TComponent}"/> adds: one panel, so an application that only wants a tab
/// of its own does not have to write an <see cref="IBevToolsExtension"/>.
/// </summary>
internal sealed class BevToolsPanelExtension(BevToolsPanelDescriptor panel) : IBevToolsExtension
{
    public BevToolsPanelDescriptor Panel { get; } = panel;

    // The id tells two panels with the same title apart, in About's list and in the prefix of an error.
    public string Name => $"{Panel.Title} panel ({Panel.Id})";

    public void Configure(IBevToolsExtensionBuilder builder) => builder.AddPanel(Panel);
}
