namespace Bit.Bheadless;

/// <summary>What a <see cref="BitBhOptionGroup"/> shares with its label.</summary>
public sealed class BitBhOptionGroupContext
{
    private readonly Action<bool> _registerLabel;

    internal BitBhOptionGroupContext(string labelId, Action<bool> registerLabel)
    {
        LabelId = labelId;
        _registerLabel = registerLabel;
    }

    /// <summary>The id the label renders with.</summary>
    public string LabelId { get; }

    internal void RegisterLabel(bool present) => _registerLabel(present);
}
