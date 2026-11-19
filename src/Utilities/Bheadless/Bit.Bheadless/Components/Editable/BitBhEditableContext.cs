namespace Bit.Bheadless;

/// <summary>The state of a <see cref="BitBhEditable"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhEditableContext
{
    internal BitBhEditableContext(BitBhEditable owner) => Owner = owner;

    internal BitBhEditable Owner { get; }

    /// <summary>The committed value.</summary>
    public string Value => Owner.CurrentValue;

    /// <summary>What is being typed.</summary>
    public string Draft => Owner.Draft;

    /// <summary>Whether it is being edited.</summary>
    public bool IsEditing => Owner.IsEditing;

    /// <summary>Starts editing.</summary>
    public Task EditAsync() => Owner.EditAsync();

    /// <summary>Commits the draft.</summary>
    public Task SubmitAsync() => Owner.SubmitAsync();

    /// <summary>Cancels the edit.</summary>
    public Task CancelAsync() => Owner.CancelAsync();
}
