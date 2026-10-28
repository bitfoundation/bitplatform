namespace Bit.Bmotion;

/// <summary>
/// What <see cref="IBmotionInterop.ReadStylesAsync(string, string[], string[], string[])"/> read off an element:
/// computed values, the values the stylesheet gives properties once their inline declarations are lifted, and natural
/// sizes. Keys are the engine's camelCase names.
/// </summary>
public sealed class BmotionStyleRead
{
    /// <summary>The computed value of each requested property.</summary>
    public Dictionary<string, string>? Current { get; set; }

    /// <summary>The value the stylesheet gives each requested property, with its inline declaration lifted.</summary>
    public Dictionary<string, string>? Stylesheet { get; set; }

    /// <summary>The natural (<c>auto</c>) size of each requested property, in px.</summary>
    public Dictionary<string, string>? Auto { get; set; }
}
