namespace Bit.BlazorUI;

/// <summary>Field weights used when combining per-field scores into a command score.</summary>
public sealed class BitCommandPaletteMatchWeights
{
    /// <summary>Weight of the title. Default 1.0.</summary>
    public double Title { get; set; } = 1.0;
    /// <summary>Weight of a keyword. Default 0.9.</summary>
    public double Keyword { get; set; } = 0.9;
    /// <summary>Weight of the subtitle. Default 0.7.</summary>
    public double Subtitle { get; set; } = 0.7;
    /// <summary>Weight of the group heading. Default 0.5.</summary>
    public double Group { get; set; } = 0.5;
    /// <summary>
    /// Weight of the titles of a nested command's ancestors, when it is found from the root page
    /// (<see cref="BitCommandPaletteOptions.SearchNestedCommands"/>): <c>theme dark</c> finds "Dark" on the page of "Theme". Default 0.5.
    /// </summary>
    public double Path { get; set; } = 0.5;
    /// <summary>Weight of the description. Default 0.4.</summary>
    public double Description { get; set; } = 0.4;
    /// <summary>Weight of a tag. Default 0.3.</summary>
    public double Tag { get; set; } = 0.3;
}
