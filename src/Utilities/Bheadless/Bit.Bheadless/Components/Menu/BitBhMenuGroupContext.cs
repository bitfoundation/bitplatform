namespace Bit.Bheadless;

/// <summary>Cascaded by <see cref="BitBhMenuGroup"/> so its label can name it.</summary>
public sealed class BitBhMenuGroupContext
{
    private readonly Action<bool> _registerLabel;

    internal BitBhMenuGroupContext(string labelId, Action<bool> registerLabel)
    {
        LabelId = labelId;
        _registerLabel = registerLabel;
    }

    /// <summary>The id the group's label renders with.</summary>
    public string LabelId { get; }

    internal void RegisterLabel(bool present) => _registerLabel(present);
}
