namespace Bit.Butil.Demo.Client.Shared;

/// <summary>
/// One file of a code sample. A snippet that only makes sense as several files - a page plus the
/// worker script it spawns, a component plus the service-worker it talks to, C# plus the JS module
/// it imports - is handed to <see cref="CodeBlock"/> as an array of these, and the pane shows them
/// as tabs rather than making the reader guess at the half that is missing.
/// </summary>
/// <remarks>
/// A plain object rather than a child component on purpose. The tab strip has to name every file
/// in the same render that draws it, and children registering themselves with their parent only
/// have names by the render after - which never arrives during the standalone prerender the MCP
/// tools render pages through.
/// </remarks>
public sealed class CodeFile
{
    /// <summary>Infers the language from the file's extension.</summary>
    /// <param name="name">The file's name, as the reader would create it, e.g. "wwwroot/worker.js".</param>
    /// <param name="code">The file's contents.</param>
    public CodeFile(string name, string code) : this(name, LanguageOf(name), code) { }

    /// <param name="name">The file's name, as the reader would create it, e.g. "wwwroot/worker.js".</param>
    /// <param name="language">A fence slug - "csharp", "razor", "js", "json"...</param>
    /// <param name="code">The file's contents.</param>
    public CodeFile(string name, string language, string code)
    {
        Name = name;
        Language = language;
        Code = code;
    }

    /// <summary>The file's name, shown on its tab.</summary>
    public string Name { get; }

    /// <summary>The fence slug the sample is highlighted and labelled with.</summary>
    public string Language { get; }

    /// <summary>The file's contents.</summary>
    public string Code { get; }

    // Only the extensions this site's samples actually use. An unknown one falls through to C#,
    // which is what SyntaxHighlighter treats an empty slug as anyway.
    private static string LanguageOf(string name) => (Path.GetExtension(name) ?? "").ToLowerInvariant() switch
    {
        ".razor" or ".cshtml" => "razor",
        ".js" or ".mjs" => "js",
        ".ts" => "ts",
        ".html" or ".htm" => "html",
        ".json" => "json",
        ".css" => "css",
        ".xml" or ".csproj" or ".props" or ".targets" or ".xaml" => "xml",
        ".md" => "markdown",
        ".sh" or ".ps1" or ".cmd" => "shell",
        _ => "csharp",
    };
}
