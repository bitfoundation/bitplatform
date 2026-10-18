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

    /// <summary>Go back one page, keeping the palette open.</summary>
    public static BaletteCommandResult Back { get; } = new BackResult();

    /// <summary>Push a page listing the given commands.</summary>
    public static BaletteCommandResult Page(string title, IReadOnlyList<BaletteCommand> commands, string? placeholder = null, bool multiSelect = false, Func<BaletteCommandContext, Task<BaletteCommandResult>>? onSubmit = null)
        => new PageResult(title, commands, placeholder, multiSelect, onSubmit);

    /// <summary>Ask the user for free text, then run <paramref name="onSubmit"/> with <see cref="BaletteCommandContext.PromptValue"/> set.</summary>
    public static BaletteCommandResult Prompt(string title, Func<BaletteCommandContext, Task<BaletteCommandResult>> onSubmit, string? placeholder = null, string? initialValue = null)
        => new PromptResult(title, onSubmit, placeholder, initialValue);

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

    /// <summary>A free-text step; see <see cref="Prompt"/>.</summary>
    public sealed record PromptResult(string Title, Func<BaletteCommandContext, Task<BaletteCommandResult>> OnSubmit, string? Placeholder, string? InitialValue) : BaletteCommandResult;

    /// <summary>A footer message; see <see cref="Toast"/>.</summary>
    public sealed record ToastResult(string Message, bool StayOpen) : BaletteCommandResult;
}
