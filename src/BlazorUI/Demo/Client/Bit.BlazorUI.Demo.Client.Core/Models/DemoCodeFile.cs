namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// One file of an example's source.
/// <para>
/// A <c>DemoExample</c> shows a single unnamed sample - the Razor markup, with the <c>@code</c>
/// block under it - and that stays the shape of nearly every example. Some features are not one
/// file though: a component styled from an isolated stylesheet, a page whose handlers are worth
/// showing beside its markup. Those pass a list of these instead, or beside the pair, and the code
/// panel grows one tab per file.
/// </para>
/// </summary>
public class DemoCodeFile
{
    public DemoCodeFile() { }

    /// <param name="name">The tab's label, written as the file would be named on disk.</param>
    /// <param name="code">The file's source, verbatim.</param>
    /// <param name="language">Only when <paramref name="name"/> does not end in a suffix that says so.</param>
    public DemoCodeFile(string name, string code, string? language = null)
    {
        Name = name;
        Code = code;
        Language = language;
    }

    /// <summary>
    /// The tab's label. Written as the file would be named on disk - <c>BitFooDemo.razor.scss</c> -
    /// because that is also what says which language it is in.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>The file's source, verbatim. Trimmed when it is shown, exactly like the Razor sample.</summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// What the file is written in, for the files whose <see cref="Name"/> does not say - a tab
    /// labelled "Program.cs additions", a snippet named after what it does rather than after a file.
    /// One of the canonical names <see cref="LanguageOf"/> returns.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// The language this file is in: the one it was given, or the one its name implies. Null when
    /// neither says - the file is then shown, and fenced for an MCP client, as plain text.
    /// </summary>
    public string? EffectiveLanguage => Language.HasValue() ? Language : LanguageOf(Name);

    /// <summary>
    /// The language Prism is asked to tokenize the file as.
    /// <para>
    /// The bundle the site ships (<c>wwwroot/prism-1.28.0</c>) carries markup, css, javascript,
    /// csharp and cshtml and nothing else, so a language that is a superset of one of those is
    /// highlighted as the subset it extends - scss and less as css, TypeScript as JavaScript. That
    /// leaves a nested rule or a type annotation untokenized rather than mistokenized. Anything the
    /// bundle does not know at all Prism leaves as plain text, silently, which is why an unmapped
    /// language is passed through rather than guessed at.
    /// </para>
    /// </summary>
    public string? HighlightLanguage => EffectiveLanguage switch
    {
        "razor" => "cshtml",
        "scss" or "sass" or "less" => "css",
        "typescript" => "javascript",
        "html" or "xml" or "svg" => "markup",
        var other => other
    };

    /// <summary>The class the <c>&lt;code&gt;</c> element carries, or null when Prism has nothing to do.</summary>
    public string? HighlightClass => HighlightLanguage.HasValue() ? $"language-{HighlightLanguage}" : null;

    /// <summary>
    /// The suffixes that say what a file is written in, longest first so that <c>.razor.cs</c> is
    /// read as C# rather than as the Razor file it sits beside.
    /// </summary>
    private static readonly (string Suffix, string Language)[] _languages =
    [
        (".razor.cs", "csharp"),
        (".razor.scss", "scss"),
        (".razor.css", "css"),
        (".razor", "razor"),
        (".cshtml", "razor"),
        (".cs", "csharp"),
        (".scss", "scss"),
        (".sass", "sass"),
        (".less", "less"),
        (".css", "css"),
        (".ts", "typescript"),
        (".js", "javascript"),
        (".json", "json"),
        (".html", "html"),
        (".htm", "html"),
        (".xml", "xml"),
        (".svg", "svg"),
        (".yml", "yaml"),
        (".yaml", "yaml"),
        (".sh", "bash"),
    ];

    /// <summary>
    /// The language a file name implies, or null when it implies nothing - a tab that is named
    /// after what the snippet does rather than after a file, and that has to say so with
    /// <see cref="Language"/>.
    /// </summary>
    public static string? LanguageOf(string? name)
    {
        if (name.HasValue() is false) return null;

        foreach (var (suffix, language) in _languages)
        {
            if (name!.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return language;
        }

        return null;
    }
}
