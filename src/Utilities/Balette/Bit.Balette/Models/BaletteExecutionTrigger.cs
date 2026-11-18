namespace Bit.Balette;

/// <summary>What started a command: the palette's own UI, a global shortcut, or code calling <see cref="IBaletteService.ExecuteAsync"/>.</summary>
public enum BaletteExecutionTrigger
{
    /// <summary>A row chosen in the palette, with the keyboard or the pointer, or a prompt or multi-select page submitted.</summary>
    Palette,

    /// <summary>A command's global shortcut, pressed while the palette was closed (or open).</summary>
    Shortcut,

    /// <summary>The application, calling <see cref="IBaletteService.ExecuteAsync"/>.</summary>
    Programmatic,
}
