namespace Bit.BlazorUI;

/// <summary>Tells the palette what to do after a handler ran.</summary>
public abstract record BitCommandPaletteCommandResult
{
    /// <summary>Close the palette (default).</summary>
    public static BitCommandPaletteCommandResult Close { get; } = new CloseResult();

    /// <summary>Keep the palette open on the current page (e.g. toggles).</summary>
    public static BitCommandPaletteCommandResult KeepOpen { get; } = new KeepOpenResult();

    /// <summary>Clear the query and return to the root page, keeping the palette open.</summary>
    public static BitCommandPaletteCommandResult Reset { get; } = new ResetResult();

    /// <summary>Go back one page, keeping the palette open; on the root page - an option found from it - stay there.</summary>
    public static BitCommandPaletteCommandResult Back { get; } = new BackResult();

    /// <summary>Push a page listing the given commands.</summary>
    public static BitCommandPaletteCommandResult Page(string title, IReadOnlyList<BitCommandPaletteCommand> commands, string? placeholder = null, bool multiSelect = false, Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>>? onSubmit = null)
        => new PageResult(title, commands, placeholder, multiSelect, onSubmit);

    /// <summary>Ask the user for free text, then run <paramref name="onSubmit"/> with <see cref="BitCommandPaletteCommandContext.PromptValue"/> set.</summary>
    public static BitCommandPaletteCommandResult Prompt(string title, Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>> onSubmit, string? placeholder = null, string? initialValue = null)
        => new PromptResult(title, onSubmit, placeholder, initialValue);

    /// <summary>
    /// Ask the user for free text that <paramref name="validate"/> accepts - it returns null for a valid value and the
    /// message to show otherwise - then run <paramref name="onSubmit"/> with <see cref="BitCommandPaletteCommandContext.PromptValue"/>
    /// set. The message is shown and announced when <c>Enter</c> is pressed on an invalid value, and follows the text
    /// from then on. For checks that need a server, return <see cref="Error"/> from <paramref name="onSubmit"/> instead.
    /// </summary>
    public static BitCommandPaletteCommandResult Prompt(string title, Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>> onSubmit, Func<string, string?> validate, string? placeholder = null, string? initialValue = null)
        => new PromptResult(title, onSubmit, placeholder, initialValue) { Validate = validate };

    /// <summary>
    /// Show an error and stay where the palette is - on a prompt, so the value can be corrected. For failures a handler
    /// expects (a name already taken, a permission missing); exceptions are for the ones it does not.
    /// </summary>
    public static BitCommandPaletteCommandResult Error(string message) => new ErrorResult(message);

    /// <summary>Show a transient message in the palette footer.</summary>
    public static BitCommandPaletteCommandResult Toast(string message, bool keepOpen = false) => new ToastResult(message, keepOpen);

    /// <summary>See <see cref="Close"/>.</summary>
    public sealed record CloseResult : BitCommandPaletteCommandResult;

    /// <summary>See <see cref="KeepOpen"/>.</summary>
    public sealed record KeepOpenResult : BitCommandPaletteCommandResult;

    /// <summary>See <see cref="Reset"/>.</summary>
    public sealed record ResetResult : BitCommandPaletteCommandResult;

    /// <summary>See <see cref="Back"/>.</summary>
    public sealed record BackResult : BitCommandPaletteCommandResult;

    /// <summary>A page of commands. <c>OnSubmit</c> is invoked with <see cref="BitCommandPaletteCommandContext.SelectedCommands"/> on <c>Ctrl+Enter</c> for multi-select pages.</summary>
    public sealed record PageResult(string Title, IReadOnlyList<BitCommandPaletteCommand> Commands, string? Placeholder, bool MultiSelect, Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>>? OnSubmit = null) : BitCommandPaletteCommandResult;

    /// <summary>A free-text step; see <see cref="Prompt(string, Func{BitCommandPaletteCommandContext, Task{BitCommandPaletteCommandResult}}, string?, string?)"/>.</summary>
    public sealed record PromptResult(string Title, Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>> OnSubmit, string? Placeholder, string? InitialValue) : BitCommandPaletteCommandResult
    {
        /// <summary>Returns null for a value that may be submitted, and the message to show for one that may not.</summary>
        public Func<string, string?>? Validate { get; init; }
    }

    /// <summary>An error shown where the palette is; see <see cref="Error"/>.</summary>
    public sealed record ErrorResult(string Message) : BitCommandPaletteCommandResult;

    /// <summary>A footer message; see <see cref="Toast"/>.</summary>
    public sealed record ToastResult(string Message, bool StayOpen) : BitCommandPaletteCommandResult;
}
