namespace Bit.Bheadless;

/// <summary>Starts editing a <see cref="BitBhEditable"/>; shown while it isn't edited, and gets the focus back after.</summary>
public class BitBhEditableEditTrigger : BitBhEditableTriggerBase
{
    protected override bool ShownWhileEditing => false;

    protected override Task ActivateAsync(BitBhEditable owner) => owner.EditAsync(ComponentId);
}
