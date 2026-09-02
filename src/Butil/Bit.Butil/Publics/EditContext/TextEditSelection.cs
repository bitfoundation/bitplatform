namespace Bit.Butil;

/// <summary>
/// Where the caret or selection sits in a <see cref="TextEditContext"/>'s text.
/// </summary>
public class TextEditSelection
{
    /// <summary>Start offset of the selection.</summary>
    public int Start { get; set; }

    /// <summary>End offset of the selection. Equal to <see cref="Start"/> for a plain caret.</summary>
    public int End { get; set; }
}
