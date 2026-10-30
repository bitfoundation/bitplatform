namespace Bit.Websites.Platform.Server.Services.Mcp;

/// <summary>
/// The bit libraries that answer for themselves through MCP tools of their own, rather than through the source code
/// index. Every list of them this site hands an agent - the server instructions, the descriptions of the tools that
/// point away from them, the chatbot's prompt - is written from here, so adding or renaming a library is one edit.
/// </summary>
public static class BitLibraries
{
    /// <summary>
    /// Their tools are served by this site itself, out of each release's own documentation site
    /// (<see cref="McpDocumentationSite.Projects"/>), and listed to every caller.
    /// </summary>
    public static readonly BitLibrary[] ServedHere =
    [
        new("BlazorUI", "a component, a theme"),
        new("Butil", "a browser API")
    ];

    /// <summary>
    /// Each has an MCP server of its own at https://{name in lowercase}.bitplatform.dev/mcp, which this site proxies
    /// for the callers that ask for every library. Their tools are listed in this order.
    /// </summary>
    public static readonly BitLibrary[] Deployed =
    [
        new("Bmotion", "an animation"),
        new("Bswup", "the service worker"),
        new("Brouter", "routing"),
        new("Bmage", "image processing"),
        new("Bink", "a drawing canvas"),
        new("Balette", "a command palette"),
        new("BbScanner", "a barcode scanner"),
        new("Bdnd", "drag and drop"),
        new("BlazeDb", "a browser database"),
        new("Blow", "a node-based editor"),
        new("Borm", "a schema-driven form"),
        new("Bcrdt", "real-time collaboration"),
        new("Bonboarding", "a product tour"),
        new("Bortable", "a sortable list"),
        new("Blex", "state management"),
        new("Bsqlite", "an EF Core database in the browser"),
        new("Belemetry", "telemetry"),
        new("Bheadless", "accessible unstyled UI primitives")
    ];

    public static readonly BitLibrary[] All = [.. ServedHere, .. Deployed];

    /// <summary>The names of <paramref name="libraries"/> the way a sentence lists them: "Bmotion, Bswup and Brouter".</summary>
    public static string Names(IEnumerable<BitLibrary> libraries) => List(libraries.Select(library => library.Name), "and");

    /// <summary>Joins <paramref name="items"/> the way a sentence lists them: "a, b and c", or "a, b or c".</summary>
    public static string List(IEnumerable<string> items, string conjunction)
    {
        var array = items.ToArray();

        return array.Length < 2 ? string.Concat(array) : $"{string.Join(", ", array[..^1])} {conjunction} {array[^1]}";
    }
}

/// <param name="Name">The name its package and its MCP tools carry, without the bit prefix.</param>
/// <param name="Topic">What a question has to be about for this library to answer it, worded to follow "a question about".</param>
public sealed record BitLibrary(string Name, string Topic);
