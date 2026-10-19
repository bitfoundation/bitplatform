namespace Bit.Blex.Demo.Client.Stores;

/// <summary>UI density options for the settings demo.</summary>
public enum Density
{
    /// <summary>Roomy spacing.</summary>
    Comfortable,

    /// <summary>Tight spacing.</summary>
    Compact,
}

/// <summary>
/// A persisted store: <c>Persist = true</c> plus a registered <see cref="IBlexStorage"/> means
/// the state is rehydrated on startup and written back after every action. <c>Token</c> exists to
/// demonstrate DevTools redaction -- it is replaced with <c>&lt;redacted&gt;</c> in the monitor.
/// </summary>
[BlexStoreAttribute(Name = "settings", Persist = true)]
public partial class SettingsStore
{
    [BlexStateAttribute] private string _accent = "violet";
    [BlexStateAttribute] private Density _density = Density.Comfortable;
    [BlexStateAttribute] private bool _showLineNumbers = true;
    [BlexStateAttribute] private string _token = "sk-live-51H8xQ2eZvKYlo";

    [BlexComputedAttribute] private string ComputeSummary() => $"{Accent} / {Density} / {(ShowLineNumbers ? "numbered" : "plain")}";

    [BlexActionAttribute] private void OnSetAccent(string accent) => Accent = accent;

    [BlexActionAttribute] private void OnSetDensity(Density density) => Density = density;

    [BlexActionAttribute] private void OnToggleLineNumbers() => ShowLineNumbers = !ShowLineNumbers;

    [BlexActionAttribute] private void OnRotateToken() => Token = "sk-live-" + Guid.NewGuid().ToString("N")[..14];
}
