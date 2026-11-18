namespace Bit.Bheadless;

/// <summary>The state of a <see cref="BitBhTagsInput"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhTagsInputContext
{
    internal BitBhTagsInputContext(BitBhTagsInput owner) => Owner = owner;

    internal BitBhTagsInput Owner { get; }

    /// <summary>The current tags.</summary>
    public IReadOnlyList<string> Values => Owner.CurrentValues;

    /// <summary>The text being typed.</summary>
    public string Text => Owner.Text;

    /// <summary>Whether no more tags can be added.</summary>
    public bool IsMaxReached => Owner.IsMaxReached;

    /// <summary>Whether the input is disabled.</summary>
    public bool Disabled => Owner.IsDisabled;

    /// <summary>Adds a tag.</summary>
    public Task<bool> AddAsync(string tag) => Owner.AddAsync(tag);

    /// <summary>Removes the tag at an index.</summary>
    public Task RemoveAtAsync(int index) => Owner.RemoveAtAsync(index);

    /// <summary>Removes every tag.</summary>
    public Task ClearAsync() => Owner.ClearAsync();
}
