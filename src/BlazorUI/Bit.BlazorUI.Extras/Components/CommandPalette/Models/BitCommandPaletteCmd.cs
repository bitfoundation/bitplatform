namespace Bit.BlazorUI;

/// <summary>Entry points for the fluent API: <c>BitCommandPaletteCmd.New("Title").Href("/").Build()</c>.</summary>
public static class BitCommandPaletteCmd
{
    /// <summary>Starts a command with the given title.</summary>
    public static BitCommandPaletteCommandBuilder New(string title) => new(title);
    /// <summary>Starts a command that navigates to <paramref name="href"/>.</summary>
    public static BitCommandPaletteCommandBuilder Navigate(string title, string href) => new BitCommandPaletteCommandBuilder(title).Href(href);
    /// <summary>Starts a command that runs <paramref name="action"/> and closes the palette.</summary>
    public static BitCommandPaletteCommandBuilder Action(string title, Action action) => new BitCommandPaletteCommandBuilder(title).Handler(action);
    /// <summary>Starts a command that runs <paramref name="action"/> and closes the palette.</summary>
    public static BitCommandPaletteCommandBuilder Action(string title, Func<Task> action) => new BitCommandPaletteCommandBuilder(title).Handler(_ => action());
    /// <summary>Starts a command whose handler decides what the palette does next.</summary>
    public static BitCommandPaletteCommandBuilder Action(string title, Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>> action) => new BitCommandPaletteCommandBuilder(title).Handler(action);
    /// <summary>Starts a command that opens a page listing <paramref name="children"/>.</summary>
    public static BitCommandPaletteCommandBuilder Page(string title, params BitCommandPaletteCommand[] children) => new BitCommandPaletteCommandBuilder(title).Kind(BitCommandPaletteCommandKind.Page).Children(children);
}
