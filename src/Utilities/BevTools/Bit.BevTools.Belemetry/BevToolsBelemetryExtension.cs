namespace Bit.BevTools;

/// <summary>Contributes the Support panel: Belemetry's production diagnostics inside BevTools.</summary>
public sealed class BevToolsBelemetryExtension : IBevToolsExtension
{
    /// <summary>The panel's id, for <c>ShowPanel</c>.</summary>
    public const string PanelId = "belemetry";

    public string Name => "Belemetry production diagnostics";

    public void Configure(IBevToolsExtensionBuilder builder) =>
        builder.AddPanel(new BevToolsPanelDescriptor(PanelId, "Support", typeof(BevBelemetryPanel), Order: 60));
}
