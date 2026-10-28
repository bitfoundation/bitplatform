using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Bit.Balette.Demo.Client.Shared;
using Bit.Balette.Demo.Server.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Balette.Tests.Mcp;

/// <summary>
/// The demo's code viewer: every snippet a page shows goes through the highlighter without its safety net, so a lexer
/// bug fails here rather than shipping as an uncoloured snippet; and the colours tell apart what a reader has to - a
/// member from a type, C# from text, a selector from a declaration.
/// </summary>
[TestClass]
public class DemoCodeHighlighterTests
{
    private static readonly Assembly Client = typeof(CodeBlock).Assembly;

    /// <summary>
    /// Every snippet a page shows in a CodeBlock of its own, in the language the block is given. The page's source - as
    /// the MCP server serves it - says which field holds the snippet, and the field is read from the compiled page, so
    /// what is lexed is the text the page renders. The MCP page is not served, so its one snippet has a test of its own.
    /// </summary>
    public static IEnumerable<object[]> Snippets =>
        from type in Client.GetTypes()
        where type.GetCustomAttributes<RouteAttribute>().Any()
        let source = BaletteSourceCatalog.GetSourceFile($"Demo/Client/Pages/{type.Name}.razor")
        where source is not null
        from block in Regex.Matches(source, "<CodeBlock\\b[^>]*>")
        let name = Regex.Match(block.Value, "\\bCode=\"@(\\w+)\"").Groups[1].Value
        let snippet = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        // A snippet built from the page's state - the theming page's markup, the ranking formula - is a property.
        where snippet is not null
        let language = Regex.Match(block.Value, "\\bLanguage=\"(\\w+)\"")
        select new object[]
        {
            $"/{type.Name}: {name}",
            (string)snippet.GetValue(null)!,
            language.Success ? language.Groups[1].Value : "razor",
        };

    [TestMethod]
    [DynamicData(nameof(Snippets))]
    public void Every_snippet_a_page_shows_is_highlighted_without_a_lexer_error_and_keeps_its_text(string name, string code, string language)
    {
        AssertHighlighted(name, code, language);
    }

    [TestMethod]
    public void The_snippets_of_the_pages_are_found_in_every_language()
    {
        var snippets = Snippets.ToArray();

        // A page that renders its samples some other way, or a block whose field went missing, would drop out silently.
        foreach (var page in new[] { "Home", "GettingStarted", "Commands", "PagesAndFlows", "SearchAndRanking", "Modes", "Keyboard", "Theming", "Localization", "Performance", "OptionsAndSecurity" })
        {
            Assert.IsTrue(snippets.Any(snippet => ((string)snippet[0]).StartsWith($"/{page}: ", StringComparison.Ordinal)), $"{page} shows no snippet");
        }
        CollectionAssert.IsSubsetOf(new[] { "razor", "csharp", "css" }, snippets.Select(snippet => (string)snippet[2]).Distinct().ToArray());
        Assert.AreEqual("css", snippets.Single(snippet => (string)snippet[0] == "/Theming: TokensExample")[2]);

        // A Razor comment written inside a snippet's string is the snippet's, not a comment of the page that holds it.
        StringAssert.StartsWith((string)snippets.Single(snippet => (string)snippet[0] == "/Localization: ParameterExample")[1], "@* One palette. *@");
    }

    [TestMethod]
    public void The_mcp_page_s_client_configuration_is_highlighted_as_json()
    {
        var page = Client.GetTypes().Single(type => type.Name == "McpServer" && type.GetCustomAttributes<RouteAttribute>().Any());
        var config = (string)page.GetField("ClientConfig", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        AssertHighlighted("/McpServer: ClientConfig", config, "json");
        var tokens = Tokens(config, "json");
        Assert.Contains("[attr:\"mcpServers\"]", tokens, "a key is not a key");
        Assert.Contains("[attr:\"url\"]: [str:\"https://balette.bitplatform.dev/mcp\"]", tokens, "a value is not a string");
        Assert.Contains("[com:// .mcp.json", tokens, "the comment is not a comment");
    }

    [TestMethod]
    public void A_language_other_than_razor_csharp_css_or_json_throws_rather_than_being_read_as_razor()
    {
        Assert.ThrowsExactly<ArgumentException>(() => SyntaxHighlighter.Highlight("var x = a < b;", "cs"));
        Assert.ThrowsExactly<ArgumentException>(() => SyntaxHighlighter.HighlightOrThrow("var x = a < b;", "c#"));
        Assert.Contains("[key:var]", Tokens("var x = 1;", "CSharp"), "the language's name is matched regardless of case");
        Assert.Contains("[attr:color]", Tokens("a { color: red; }", "CSS"), "the language's name is matched regardless of case");
    }

    [TestMethod]
    public void A_stylesheet_tells_its_selectors_from_its_declarations()
    {
        var tokens = Tokens("""
            /* after the library's stylesheet */
            :root[data-theme="dark"] {
                --bit-balette-accent: #2dd4bf;
                --bit-balette-width: 760px;
            }

            .my-palette [data-balette="item"][data-active] > kbd { background: var(--bit-balette-bg) !important; opacity: .5; }

            @media (max-width: 600px) {
                .my-palette { font-size: 0.75rem }
            }
            """, "css");

        Assert.Contains("[com:/* after the library's stylesheet */]", tokens, "the comment is not a comment");
        Assert.Contains("[fn::root][pun:[][attr:data-theme]=[str:\"dark\"][pun:]]", tokens, "the attribute selector is not one");
        Assert.Contains("[attr:--bit-balette-accent][pun::] [num:#2dd4bf][pun:;]", tokens, "a custom property's colour is not a value");
        Assert.Contains("[num:760px]", tokens, "a length is not a number");
        Assert.Contains("[type:.my-palette] [pun:[][attr:data-balette]=[str:\"item\"][pun:]][pun:[][attr:data-active][pun:]] > [tag:kbd]", tokens, "the selector is not a selector");
        Assert.Contains("[fn:var]([attr:--bit-balette-bg]) [key:!important]", tokens, "a function, a custom property or !important is not one");
        Assert.Contains("[attr:opacity][pun::] [num:.5]", tokens, "a fraction is not a number");
        Assert.Contains("[key:@media] ([attr:max-width]: [num:600px])", tokens, "the media query is not one");
        Assert.Contains("[attr:font-size][pun::] [num:0.75rem] [pun:}]", tokens, "a declaration without a ';' before the '}' is not one");
    }

    [TestMethod]
    public void A_json_key_is_told_from_a_string_value()
    {
        var tokens = Tokens("""
            {
              "name": "balette",
              "port": 5000,
              "secure": true,
              "tags": ["mcp", null]
            }
            """, "json");

        Assert.Contains("[attr:\"name\"]: [str:\"balette\"]", tokens);
        Assert.Contains("[attr:\"port\"]: [num:5000]", tokens);
        Assert.Contains("[attr:\"secure\"]: [key:true]", tokens);
        Assert.Contains("[[str:\"mcp\"], [key:null]]", tokens);
    }

    [TestMethod]
    public void A_member_the_source_declares_is_not_coloured_as_a_type()
    {
        var tokens = Tokens("""
            @inject IBaletteService Palette

            <div>@Command.Title</div>
            <span>@Now</span>

            @code {
                [Parameter] public BaletteCommand Command { get; set; } = default!;
                [Parameter] public EventCallback<int> OnStep { get; set; }

                private Task Step() => OnStep.InvokeAsync(Palette.Commands.Count);
                private BaletteCommandResult Next() => BaletteCommandResult.KeepOpen;
            }
            """);

        foreach (var member in new[] { "Command", "OnStep", "Palette" })
        {
            Assert.DoesNotContain($"[type:{member}].", tokens, $"{member} is coloured as a type");
        }
        Assert.Contains("[type:BaletteCommandResult].KeepOpen", tokens, "a type's static member lost the type's colour");
        Assert.Contains("[raz:@]Now[pun:</]", tokens, "the markup after an implicit expression is taken for type arguments");
    }

    [TestMethod]
    public void A_component_parameter_that_takes_csharp_is_coloured_as_csharp_and_one_that_takes_a_string_as_a_string()
    {
        var tokens = Tokens("""
            <Balette Inline="true" Commands="_commands" Placeholder="Search files…" Density="compact" class="palette" />

            @code {
                private List<BaletteCommand> _commands = [];
            }
            """);

        Assert.Contains("[attr:Inline]=[str:\"][key:true][str:\"]", tokens, "a literal is a string");
        Assert.Contains("[attr:Commands]=[str:\"]_commands[str:\"]", tokens, "a field is a string");
        Assert.Contains("[attr:Placeholder]=[str:\"Search files…\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:Density]=[str:\"compact\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:class]=[str:\"palette\"]", tokens, "an attribute passed on to an element is not a string");
    }

    // Highlights `code` without the safety net, and checks that every character of it is kept and some are coloured.
    private static void AssertHighlighted(string name, string code, string language)
    {
        var html = SyntaxHighlighter.HighlightOrThrow(code, language);

        Assert.AreEqual(code.Replace("\r\n", "\n"), WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", "")), $"{name}: the highlighted text differs from the source");
        Assert.Contains("class=\"tok-", html, $"{name} is not coloured");
    }

    /// <summary>
    /// The highlighted text with each token written as <c>[kind:text]</c>, adjacent tokens of one kind as one -
    /// the quotes and the text of a string are a single <c>[str:"item"]</c> - and the entities decoded.
    /// </summary>
    private static string Tokens(string code, string language = "razor")
    {
        var tokens = Regex.Replace(SyntaxHighlighter.HighlightOrThrow(code, language), "<span class=\"tok-(\\w+)\">([^<]*)</span>", "[$1:$2]");
        string merged;
        while ((merged = Regex.Replace(tokens, @"\[(\w+):([^\]]*)\]\[\1:", "[$1:$2")) != tokens)
        {
            tokens = merged;
        }
        return WebUtility.HtmlDecode(tokens);
    }
}
