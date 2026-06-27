namespace Bit.Butil;

/// <summary>
/// Options for <see href="https://developer.mozilla.org/en-US/docs/Web/API/MutationObserver/observe">MutationObserver.observe()</see>.
/// </summary>
public class MutationObserverOptions
{
    /// <summary>Watch for added or removed children.</summary>
    public bool ChildList { get; set; }

    /// <summary>Watch for attribute changes on the target.</summary>
    public bool Attributes { get; set; }

    /// <summary>Watch for character-data changes within the target.</summary>
    public bool CharacterData { get; set; }

    /// <summary>Apply the chosen options to the entire subtree, not just the immediate target.</summary>
    public bool Subtree { get; set; }

    /// <summary>Include the previous attribute value in each <see cref="MutationRecord"/>.</summary>
    public bool AttributeOldValue { get; set; }

    /// <summary>Include the previous character-data value in each <see cref="MutationRecord"/>.</summary>
    public bool CharacterDataOldValue { get; set; }

    /// <summary>Optional whitelist of attribute names to watch. <c>null</c> means all.</summary>
    public string[]? AttributeFilter { get; set; }
}
