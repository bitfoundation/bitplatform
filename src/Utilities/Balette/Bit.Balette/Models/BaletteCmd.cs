namespace Bit.Balette;

/// <summary>Entry points for the fluent API: <c>BaletteCmd.New("Title").Href("/").Build()</c>.</summary>
public static class BaletteCmd
{
    /// <summary>Starts a command with the given title.</summary>
    public static BaletteCommandBuilder New(string title) => new(title);
    /// <summary>Starts a command that navigates to <paramref name="href"/>.</summary>
    public static BaletteCommandBuilder Navigate(string title, string href) => new BaletteCommandBuilder(title).Href(href);
    /// <summary>Starts a command that runs <paramref name="action"/> and closes the palette.</summary>
    public static BaletteCommandBuilder Action(string title, Action action) => new BaletteCommandBuilder(title).Handler(action);
    /// <summary>Starts a command that runs <paramref name="action"/> and closes the palette.</summary>
    public static BaletteCommandBuilder Action(string title, Func<Task> action) => new BaletteCommandBuilder(title).Handler(_ => action());
    /// <summary>Starts a command whose handler decides what the palette does next.</summary>
    public static BaletteCommandBuilder Action(string title, Func<BaletteCommandContext, Task<BaletteCommandResult>> action) => new BaletteCommandBuilder(title).Handler(action);
    /// <summary>Starts a command that opens a page listing <paramref name="children"/>.</summary>
    public static BaletteCommandBuilder Page(string title, params BaletteCommand[] children) => new BaletteCommandBuilder(title).Kind(BaletteCommandKind.Page).Children(children);
}
