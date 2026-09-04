namespace Bit.Butil;

/// <summary>Where an <c>ariaNotify</c> announcement goes in the screen reader's queue.</summary>
public enum AriaNotifyPriority
{
    /// <summary>Announced after whatever the screen reader is already saying. The default.</summary>
    Normal,

    /// <summary>Interrupts the current announcement. For things the user must hear now.</summary>
    High
}
