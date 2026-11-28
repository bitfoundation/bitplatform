namespace Bit.BlazorUI;

/// <summary>Describes a handler that threw: <see cref="IBitCommandPaletteService.CommandFailed"/>.</summary>
public sealed class BitCommandPaletteCommandErrorEventArgs(BitCommandPaletteCommand command, Exception exception) : EventArgs
{
    /// <summary>The command.</summary>
    public BitCommandPaletteCommand Command { get; } = command;

    /// <summary>What the handler threw.</summary>
    public Exception Exception { get; } = exception;

    /// <summary>
    /// Set to true when the subscriber reported the error itself: the palette then does not log it, and
    /// <see cref="IBitCommandPaletteService.ExecuteAsync"/> does not rethrow it. A failure in the palette's own UI - a row chosen
    /// with the keyboard or the mouse, a shortcut - is never rethrown, so it cannot end a Blazor Server circuit.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// What the palette shows the user: a localized, generic <see cref="BitCommandPaletteStrings.CommandFailed"/> by default, so
    /// an exception's own text - which can name servers, tables or users - never reaches the screen by accident. Set it
    /// to a text of your own (for example <c>Exception.Message</c> for exceptions written for users), or to an empty
    /// string to show nothing.
    /// </summary>
    public string? Message { get; set; }
}
