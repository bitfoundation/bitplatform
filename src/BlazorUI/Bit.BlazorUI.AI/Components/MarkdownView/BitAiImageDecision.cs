namespace Bit.BlazorUI;

/// <summary>
/// What an <see cref="BitAiImagePolicy"/> decided about one image.
/// </summary>
public enum BitAiImageDecision
{
    /// <summary>Render the image.</summary>
    Show = 0,

    /// <summary>Render a placeholder that loads the image when activated.</summary>
    AskFirst = 1,

    /// <summary>Render the alternative text in place of the image.</summary>
    Hide = 2,
}
