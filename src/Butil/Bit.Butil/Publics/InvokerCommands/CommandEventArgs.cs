namespace Bit.Butil;

/// <summary>
/// A command dispatched at an element by an invoker - the payload of the <c>command</c> event.
/// </summary>
public class CommandEventArgs
{
    /// <summary>
    /// The command that was invoked. Built-in ones start with a dash (<c>"show-modal"</c>,
    /// <c>"close"</c>, <c>"toggle-popover"</c>) and are handled by the browser before the event
    /// reaches you; custom ones start with <c>--</c> and are yours alone to act on.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>The <c>id</c> of the invoking element, when it has one. Empty otherwise.</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>The invoking element's tag name, lower-cased - usually <c>"button"</c>.</summary>
    public string SourceTag { get; set; } = string.Empty;

    /// <summary>True for a custom command, i.e. one whose name starts with <c>--</c>.</summary>
    public bool IsCustom => Command.StartsWith("--", System.StringComparison.Ordinal);
}
