namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Cascading context provided by <see cref="BitBhFieldset"/>: the legend id and the
/// disabled state that every field and control inside inherits.
/// </summary>
public sealed class BitBhFieldsetContext
{
    private readonly Action<bool>? _registerLegend;

    internal BitBhFieldsetContext(string legendId, bool disabled, Action<bool>? registerLegend = null)
    {
        LegendId = legendId;
        Disabled = disabled;
        _registerLegend = registerLegend;
    }

    /// <summary>The id of the fieldset's legend.</summary>
    public string LegendId { get; }

    /// <summary>Whether the fieldset is disabled.</summary>
    public bool Disabled { get; }

    internal void RegisterLegend(bool present) => _registerLegend?.Invoke(present);
}
