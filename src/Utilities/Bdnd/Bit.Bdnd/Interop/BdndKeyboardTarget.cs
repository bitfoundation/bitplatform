namespace Bit.Bdnd;

/// <summary>The drop target a keyboard move landed on, as the module reports it: its id, its rectangle and its name.</summary>
internal sealed class BdndKeyboardTarget
{
    /// <summary>The target's handler id.</summary>
    public string Id { get; set; } = "";

    /// <summary>The edge of the target the move landed against (<c>top</c>, <c>bottom</c>, <c>left</c>, <c>right</c>), or null.</summary>
    public string? Edge { get; set; }

    /// <summary>Whether that edge is before (true) or after (false) the target in its list's order - right to left too.</summary>
    public bool? Before { get; set; }

    /// <summary>The target's rectangle once it was scrolled into view, in client coordinates.</summary>
    public BdndDomRect Rect { get; set; } = new();

    /// <summary>The target's <c>aria-label</c> or <c>title</c>, used when it has no <c>Label</c>.</summary>
    public string? Label { get; set; }

    /// <summary>The 1-based place of the target among the candidates, in document order.</summary>
    public int Position { get; set; }

    /// <summary>How many candidates are in the document.</summary>
    public int Count { get; set; }

    /// <summary>False when there was no further target in the direction asked for, and the drag stays where it was.</summary>
    public bool Moved { get; set; }
}
