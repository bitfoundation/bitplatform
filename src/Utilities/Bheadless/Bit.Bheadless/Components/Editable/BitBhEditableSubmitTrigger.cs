namespace Bit.Bheadless;

/// <summary>Commits the edit of a <see cref="BitBhEditable"/>; shown while editing.</summary>
public class BitBhEditableSubmitTrigger : BitBhEditableTriggerBase
{
    protected override bool ShownWhileEditing => true;

    protected override Task ActivateAsync(BitBhEditable owner) => owner.SubmitAsync();
}
