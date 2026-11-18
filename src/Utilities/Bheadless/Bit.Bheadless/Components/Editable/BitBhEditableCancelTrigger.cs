namespace Bit.Bheadless;

/// <summary>Cancels the edit of a <see cref="BitBhEditable"/>; shown while editing.</summary>
public class BitBhEditableCancelTrigger : BitBhEditableTriggerBase
{
    protected override bool ShownWhileEditing => true;

    protected override Task ActivateAsync(BitBhEditable owner) => owner.CancelAsync();
}
