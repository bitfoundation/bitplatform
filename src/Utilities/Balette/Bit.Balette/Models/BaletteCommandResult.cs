namespace Bit.Balette;

/// <summary>Tells the palette what to do after a handler ran.</summary>
public abstract record BaletteCommandResult
{
    /// <summary>Close the palette (default).</summary>
    public static BaletteCommandResult Close { get; } = new CloseResult();

    /// <summary>Keep the palette open on the current page (e.g. toggles).</summary>
    public static BaletteCommandResult KeepOpen { get; } = new KeepOpenResult();

    /// <summary>Clear the query and return to the root page, keeping the palette open.</summary>
    public static BaletteCommandResult Reset { get; } = new ResetResult();

    /// <summary>Go back one page, keeping the palette open; on the root page - an option found from it - stay there.</summary>
    public static BaletteCommandResult Back { get; } = new BackResult();

    /// <summary>Push a page listing the given commands.</summary>
    public static BaletteCommandResult Page(string title, IReadOnlyList<BaletteCommand> commands, string? placeholder = null, bool multiSelect = false, Func<BaletteCommandContext, Task<BaletteCommandResult>>? onSubmit = null)
        => new PageResult(title, commands, placeholder, multiSelect, onSubmit);

    /// <summary>Ask the user for free text, then run <paramref name="onSubmit"/> with <see cref="BaletteCommandContext.PromptValue"/> set.</summary>
    public static BaletteCommandResult Prompt(string title, Func<BaletteCommandContext, Task<BaletteCommandResult>> onSubmit, string? placeholder = null, string? initialValue = null)
        => new PromptResult(title, onSubmit, placeholder, initialValue);

    /// <summary>
    /// Ask the user for free text that <paramref name="validate"/> accepts - it returns null for a valid value and the
    /// message to show otherwise - then run <paramref name="onSubmit"/> with <see cref="BaletteCommandContext.PromptValue"/>
    /// set. The message is shown and announced when <c>Enter</c> is pressed on an invalid value, and follows the text
    /// from then on. For checks that need a server, return <see cref="Error"/> from <paramref name="onSubmit"/> instead.
    /// </summary>
    public static BaletteCommandResult Prompt(string title, Func<BaletteCommandContext, Task<BaletteCommandResult>> onSubmit, Func<string, string?> validate, string? placeholder = null, string? initialValue = null)
        => new PromptResult(title, onSubmit, placeholder, initialValue) { Validate = validate };

    /// <summary>
    /// Show an error and stay where the palette is - on a prompt, so the value can be corrected. For failures a handler
    /// expects (a name already taken, a permission missing); exceptions are for the ones it does not.
    /// </summary>
    public static BaletteCommandResult Error(string message) => new ErrorResult(message);

    /// <summary>Show a transient message in the palette footer.</summary>
    public static BaletteCommandResult Toast(string message, bool keepOpen = false) => new ToastResult(message, keepOpen);

    /// <summary>See <see cref="Close"/>.</summary>
    public sealed record CloseResult : BaletteCommandResult;

    /// <summary>See <see cref="KeepOpen"/>.</summary>
    public sealed record KeepOpenResult : BaletteCommandResult;

    /// <summary>See <see cref="Reset"/>.</summary>
    public sealed record ResetResult : BaletteCommandResult;

    /// <summary>See <see cref="Back"/>.</summary>
    public sealed record BackResult : BaletteCommandResult;

    /// <summary>A page of commands. <c>OnSubmit</c> is invoked with <see cref="BaletteCommandContext.SelectedCommands"/> on <c>Ctrl+Enter</c> for multi-select pages.</summary>
    public sealed record PageResult(string Title, IReadOnlyList<BaletteCommand> Commands, string? Placeholder, bool MultiSelect, Func<BaletteCommandContext, Task<BaletteCommandResult>>? OnSubmit = null) : BaletteCommandResult;

    /// <summary>A free-text step; see <see cref="Prompt(string, Func{BaletteCommandContext, Task{BaletteCommandResult}}, string?, string?)"/>.</summary>
    public sealed record PromptResult(string Title, Func<BaletteCommandContext, Task<BaletteCommandResult>> OnSubmit, string? Placeholder, string? InitialValue) : BaletteCommandResult
    {
        /// <summary>Returns null for a value that may be submitted, and the message to show for one that may not.</summary>
        public Func<string, string?>? Validate { get; init; }
    }

    /// <summary>An error shown where the palette is; see <see cref="Error"/>.</summary>
    public sealed record ErrorResult(string Message) : BaletteCommandResult;

    /// <summary>A footer message; see <see cref="Toast"/>.</summary>
    public sealed record ToastResult(string Message, bool StayOpen) : BaletteCommandResult;
}
